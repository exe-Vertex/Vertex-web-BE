namespace Vertex.Services.Models
{
    public class PasswordResetOptions
    {
        public string FrontendUrl { get; set; } = "https://vertex.io.vn";
        public int TokenLifetimeMinutes { get; set; } = 30;
    }
}