using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

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
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ILecturerService, LecturerService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ITaskNotifier, SignalRTaskNotifier>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();

var app = builder.Build();

// ── Self-Healing Database Initialization ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("Running self-healing database check...");
        db.Database.ExecuteSqlRaw("ALTER TABLE tasks ADD COLUMN IF NOT EXISTS submission_link VARCHAR(2000);");
        
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('20260524112019_AddSubmissionLinkToTask', '8.0.0')
            ON CONFLICT DO NOTHING;
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('20260525095323_AddInvitations', '8.0.0')
            ON CONFLICT DO NOTHING;
        ");

        // Seed 3 more diverse projects for the lecturer
        db.Database.ExecuteSqlRaw(@"
            -- 1. Insert Projects
            INSERT INTO projects (id, org_id, name, description, deadline, created_at, updated_at)
            VALUES
            ('c1000000-0000-0000-0000-000000000003', 'e1000000-0000-0000-0000-000000000001', 'Mobile E-Commerce App', 'Develop UI prototypes and research features for school e-commerce platform', '2026-06-15', NOW(), NOW()),
            ('c1000000-0000-0000-0000-000000000004', 'e1000000-0000-0000-0000-000000000001', 'IoT Smart Greenhouse', 'Design sensor grid layouts and dashboard mockup for urban gardening', '2026-05-20', NOW(), NOW()),
            ('c1000000-0000-0000-0000-000000000005', 'e1000000-0000-0000-0000-000000000001', 'AI Chatbot Integration', 'Build support desk automation prototype using Gemini API', '2026-05-28', NOW(), NOW())
            ON CONFLICT (id) DO NOTHING;

            -- 2. Insert Project Members
            INSERT INTO project_members (project_id, user_id, role, joined_at)
            VALUES
            ('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000003', 'Leader', NOW()),
            ('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000007', 'Member', NOW()),
            ('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000005', 'Member', NOW()),

            ('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000007', 'Leader', NOW()),
            ('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000002', 'Member', NOW()),
            ('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000001', 'Member', NOW()),

            ('c1000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000001', 'Leader', NOW()),
            ('c1000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000003', 'Member', NOW())
            ON CONFLICT (project_id, user_id) DO NOTHING;

            -- 3. Insert Project Tasks
            INSERT INTO tasks (id, project_id, title, description, status, priority, assignee_id, start_date, end_date, position, created_at, updated_at)
            VALUES
            -- Mobile E-Commerce App (On track, deadline 2026-06-15)
            ('d1000000-0000-0000-0000-000000000012', 'c1000000-0000-0000-0000-000000000003', 'User research & interviews', 'Interview 10 potential users.', 'done', 'high', 'a1000000-0000-0000-0000-000000000003', '2026-05-10', '2026-05-15', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000013', 'c1000000-0000-0000-0000-000000000003', 'Figma wireframes draft', 'Create initial wireframe layout.', 'ready-for-review', 'high', 'a1000000-0000-0000-0000-000000000007', '2026-05-16', '2026-05-20', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000014', 'c1000000-0000-0000-0000-000000000003', 'Architecture diagram', 'Define data flows and entity models.', 'in-progress', 'medium', 'a1000000-0000-0000-0000-000000000003', '2026-05-21', '2026-05-25', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000015', 'c1000000-0000-0000-0000-000000000003', 'Backend API endpoints doc', 'Write API document for frontend integration.', 'todo', 'medium', 'a1000000-0000-0000-0000-000000000005', '2026-05-26', '2026-06-01', 0, NOW(), NOW()),

            -- IoT Smart Greenhouse (Overdue, deadline 2026-05-20)
            ('d1000000-0000-0000-0000-000000000016', 'c1000000-0000-0000-0000-000000000004', 'Literature review on hydroponic sensors', 'Compile datasheet summary.', 'done', 'low', 'a1000000-0000-0000-0000-000000000007', '2026-05-01', '2026-05-05', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000017', 'c1000000-0000-0000-0000-000000000004', 'Hardware bill of materials selection', 'Select best sensors and processors.', 'done', 'high', 'a1000000-0000-0000-0000-000000000001', '2026-05-06', '2026-05-10', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000018', 'c1000000-0000-0000-0000-000000000004', 'Layout diagram export', 'Map grid sensor placements in CAD.', 'ready-for-review', 'high', 'a1000000-0000-0000-0000-000000000002', '2026-05-11', '2026-05-14', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000019', 'c1000000-0000-0000-0000-000000000004', 'Power consumption simulation', 'Run standard gardening cycle load test.', 'todo', 'high', 'a1000000-0000-0000-0000-000000000007', '2026-05-15', '2026-05-19', 0, NOW(), NOW()),

            -- AI Chatbot Integration (At risk, deadline 2026-05-28, only 3 days left with low progress)
            ('d1000000-0000-0000-0000-000000000020', 'c1000000-0000-0000-0000-000000000005', 'Identify core intent list', 'Outline 20 FAQ questions and trigger intents.', 'done', 'medium', 'a1000000-0000-0000-0000-000000000001', '2026-05-15', '2026-05-18', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000021', 'c1000000-0000-0000-0000-000000000005', 'GEMINI SDK integration test', 'Run diagnostic prompts via SDK.', 'in-progress', 'high', 'a1000000-0000-0000-0000-000000000003', '2026-05-19', '2026-05-24', 0, NOW(), NOW()),
            ('d1000000-0000-0000-0000-000000000022', 'c1000000-0000-0000-0000-000000000005', 'UI chatbot window component', 'Code floating chat frame in React.', 'todo', 'high', 'a1000000-0000-0000-0000-000000000001', '2026-05-25', '2026-05-28', 0, NOW(), NOW())
            ON CONFLICT (id) DO NOTHING;
        ");

        Console.WriteLine("✔ Self-healing database check completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("⚠ Self-healing database check encountered an error: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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
