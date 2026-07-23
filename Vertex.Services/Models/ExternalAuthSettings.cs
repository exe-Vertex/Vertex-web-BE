namespace Vertex.Services.Models
{
    public class ExternalAuthSettings
    {
        public GoogleAuthSettings Google { get; set; } = new();
        public GitHubAuthSettings GitHub { get; set; } = new();
    }

    public class GoogleAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string[] ClientIds { get; set; } = Array.Empty<string>();
    }

    public class GitHubAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}
