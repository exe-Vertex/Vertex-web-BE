using System;
using System.Collections.Generic;

namespace Vertex.Services.Models
{
    public class GeminiRequest
    {
        public Content? systemInstruction { get; set; }
        public List<Content> contents { get; set; } = new();
    }

    public class Content
    {
        public List<Part> parts { get; set; } = new();
    }

    public class Part
    {
        public string text { get; set; } = string.Empty;
    }

    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; } = new();
        public UsageMetadata usageMetadata { get; set; } = new();
    }

    public class Candidate
    {
        public Content content { get; set; } = new();
        public string finishReason { get; set; } = string.Empty;
    }

    public class UsageMetadata
    {
        public int promptTokenCount { get; set; }
        public int candidatesTokenCount { get; set; }
        public int totalTokenCount { get; set; }
    }
    
    public class ChatRequestDto
    {
        public string Prompt { get; set; } = string.Empty;
    }
}
