namespace Vertex.Services.Models
{
    public class GeminiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ChatModel { get; set; } = "gemini-2.5-flash";
        public string EmbeddingModel { get; set; } = "text-embedding-004";
        public int MaxHistoryMessages { get; set; } = 20;
    }
}
