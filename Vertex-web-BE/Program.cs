using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Vertex.Repositories;
using Vertex.Repositories.Interfaces;
using Vertex.Repositories.Repositories;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;
using Vertex.Services.Services;
using Vertex_web_BE.Hubs;
using Vertex_web_BE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Vertex API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var configuredFrontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
var localFrontendOrigins = new[]
{
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:3000",
    "http://127.0.0.1:3000"
};
var frontendOrigins = (builder.Environment.IsDevelopment()
        ? configuredFrontendOrigins.Concat(localFrontendOrigins)
        : configuredFrontendOrigins)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct()
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<PasswordResetOptions>(builder.Configuration.GetSection("PasswordReset"));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));
builder.Services.Configure<PayOSSettings>(builder.Configuration.GetSection("PayOS"));
builder.Services.Configure<ExternalAuthSettings>(builder.Configuration.GetSection("ExternalAuth"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtKey,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/taskhub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAiHistoryRepository, AiHistoryRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IExternalAuthProvider, ExternalAuthProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ILecturerService, LecturerService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ITaskNotifier, SignalRTaskNotifier>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IAiQuotaService, AiQuotaService>();

builder.Services.AddHttpClient();

// Semantic Kernel + AI Services Registration
var geminiSettings = builder.Configuration.GetSection("GeminiSettings").Get<GeminiSettings>() ?? new GeminiSettings();

// Build the Semantic Kernel with Google Gemini Chat Completion
#pragma warning disable SKEXP0001, SKEXP0050, SKEXP0070
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddGoogleAIGeminiChatCompletion(
    modelId: geminiSettings.ChatModel,
    apiKey: geminiSettings.ApiKey
);
var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);

// Build the Semantic Text Memory (Persistent JsonMemoryStore + Google Embedding)
var embeddingService = new GoogleAITextEmbeddingGenerationService(
    modelId: geminiSettings.EmbeddingModel,
    apiKey: geminiSettings.ApiKey
);
var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "vector_store.json");
var memoryStore = new Vertex.Services.Services.JsonMemoryStore(vectorStorePath);
var semanticMemory = new SemanticTextMemory(memoryStore, embeddingService);
builder.Services.AddSingleton<ISemanticTextMemory>(semanticMemory);
#pragma warning restore SKEXP0001, SKEXP0050, SKEXP0070

// Register AI services
builder.Services.AddScoped<IAiSyncService, AiSyncService>();
builder.Services.AddScoped<IAiService, AiService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<Vertex_web_BE.Middlewares.ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.UseStaticFiles();

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TaskHub>("/taskhub");

app.Run();
