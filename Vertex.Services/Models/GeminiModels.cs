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
        public Guid? OrgId { get; set; }
    }

    public class GeneratePlanRequestDto
    {
        public Guid OrgId { get; set; }
        public string ProjectGoal { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int DurationWeeks { get; set; }
        public int TeamSize { get; set; }
        public List<MemberSkillDto> TeamMembers { get; set; } = new();
    }

    public class MemberSkillDto
    {
        public string Name { get; set; } = string.Empty;
        public string? TargetSkills { get; set; }
        public List<string>? CoreSkills { get; set; }
    }

    public class GenerateSubtasksRequestDto
    {
        public Guid OrgId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;
    }
}

