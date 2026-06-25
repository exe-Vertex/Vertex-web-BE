#pragma warning disable SKEXP0070
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Vertex.Entities.AI;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class AiService : IAiService
    {
        private readonly Kernel _kernel;
        private readonly IAiHistoryRepository _historyRepository;
        private readonly IAiSyncService _syncService;
        private readonly GeminiSettings _settings;
        private readonly ILogger<AiService> _logger;

        // System prompt that defines the AI's personality, capabilities, and security rules
        private const string BaseSystemPrompt = @"You are Vertex AI, an intelligent project management assistant built into the Vertex platform.

YOUR CAPABILITIES:
- You can introduce the Vertex website, explain its features, and guide users on how to use it.
- You are an expert in project planning, task breakdown, workflow organization, and team management.
- You can analyze project data provided to you and give specific advice about the user's actual projects and tasks.

CRITICAL SECURITY RULES:
1. You MUST NOT answer any questions about computer programming, code structure, or software architecture.
2. You MUST NOT reveal any information about the internal workings, source code, database, or technical architecture of the Vertex project.
3. If the user asks anything outside of project planning or Vertex features (e.g., coding, general knowledge, personal questions), politely decline.

RESPONSE STYLE:
- Be concise, professional, and friendly.
- When discussing project data, reference specific project names, task titles, and member names when available.
- Use bullet points and clear formatting for task breakdowns.";

        public AiService(
            Kernel kernel,
            IAiHistoryRepository historyRepository,
            IAiSyncService syncService,
            IOptions<GeminiSettings> settings,
            ILogger<AiService> logger)
        {
            _kernel = kernel;
            _historyRepository = historyRepository;
            _syncService = syncService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<AiHistory> ChatAsync(Guid userId, Guid orgId, string prompt)
        {
            // === Step 1: RAG — Search Vector Store for relevant project context ===
            var relevantContext = await _syncService.SearchRelevantContextAsync(orgId, prompt, limit: 3);
            
            // === Step 2: Build system prompt with injected context ===
            var systemPrompt = BuildSystemPromptWithContext(relevantContext);

            // === Step 3: Build ChatHistory with memory (previous conversations) ===
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);

            // Load previous messages from database to give AI "memory"
            var previousMessages = await _historyRepository.GetByUserIdAsync(userId);
            var recentMessages = previousMessages
                .OrderBy(m => m.CreatedAt)
                .TakeLast(_settings.MaxHistoryMessages)
                .ToList();

            foreach (var msg in recentMessages)
            {
                chatHistory.AddUserMessage(msg.Prompt);
                if (!string.IsNullOrEmpty(msg.PlanSummary))
                {
                    chatHistory.AddAssistantMessage(msg.PlanSummary);
                }
            }

            // Add the current user message
            chatHistory.AddUserMessage(prompt);

            // === Step 4: Get AI response via Semantic Kernel ===
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            
            _logger.LogInformation("Sending chat request to AI with {HistoryCount} previous messages and {ContextCount} RAG context chunks.",
                recentMessages.Count, relevantContext.Count);
            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                new GeminiPromptExecutionSettings
                {
                    MaxTokens = 2048,
                    Temperature = 0.7
                });
            var resultText = response.Content ?? string.Empty;

            // === Step 5: Save to database ===
            var history = new AiHistory
            {
                UserId = userId,
                Prompt = prompt,
                PlanSummary = resultText,
                TokensUsed = 0, // Semantic Kernel doesn't expose token count directly for Google
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _historyRepository.AddAsync(history);

            return history;
        }

        public async Task<List<AiHistory>> GetHistoryAsync(Guid userId)
        {
            return await _historyRepository.GetByUserIdAsync(userId);
        }

        public async Task<string> GeneratePlanAsync(Guid userId, Models.GeneratePlanRequestDto request)
        {
            var memberDetails = new StringBuilder();
            if (request.TeamMembers != null)
            {
                foreach (var m in request.TeamMembers)
                {
                    var skillsList = new List<string>();
                    if (!string.IsNullOrEmpty(m.TargetSkills))
                        skillsList.Add($"Target: {m.TargetSkills}");
                    if (m.CoreSkills != null && m.CoreSkills.Count > 0)
                        skillsList.Add($"Core: {string.Join(", ", m.CoreSkills)}");

                    var skillsText = skillsList.Count > 0 ? $" (Skills - {string.Join("; ", skillsList)})" : "";
                    memberDetails.AppendLine($"- {m.Name}{skillsText}");
                }
            }

            var weeks = Math.Max(2, Math.Min(24, request.DurationWeeks));
            var categoryText = string.Equals(request.Category, "Auto detect", StringComparison.OrdinalIgnoreCase)
                ? "Auto detect from the project goal and description"
                : request.Category;
            var systemPrompt = "You are an AI Project Planner for Vertex. Your task is to generate a structured project plan based on the goal, description, team size, duration, difficulty, and available team members with their skills. You MUST respond with ONLY a valid JSON object. Do not include markdown formatting, backticks, or introduction. CRITICAL RULES: 1. Do NOT use raw double quotes inside any JSON string value (use single quotes like 'hero' instead). 2. Do NOT include raw newline characters or carriage returns inside any JSON string values (keep each string value on a single line).";

            var promptText = $@"Goal: {request.ProjectGoal}
Description: {request.Description}
Project type/category: {categoryText}
Difficulty: {request.Difficulty}
Duration: {weeks} weeks
Team size: {request.TeamSize}
Available team members:
{memberDetails}

Generate a project plan and analyze project risks. If project type/category is auto-detected, infer the most suitable type from the goal and description, then tailor milestones, subtasks, terminology, and risks to that type. You MUST respond with ONLY a valid JSON object. No markdown formatting, no backticks, no introduction.
The JSON must have these exact keys at the top level:
1. ""plan"": An array of objects (one object per week, max {weeks} objects) where each object represents a week and contains:
   - week: string (e.g. ""Week 1"")
   - milestone: string (a concise summary of the week's milestone, e.g. ""Database Schema and Backend Setup"")
   - subtasks: An array of objects, where each object represents an individual actionable task (2 to 5 tasks per week) containing:
     - title: string (short, clear, actionable task title, e.g. ""Create user registration API"")
     - description: string (detailed explanation of the task, what needs to be done, requirements, etc. Keep on a single line, no newlines)
     - assignee: string (must be one of the available team members' exact name, or ""Unassigned"". Assign tasks logically based on their skills)
     - estHours: number (estimated effort in hours, e.g. 6. Estimate realistically based on difficulty and task scope, NOT a flat 40h)
     - priority: string (must be ""High"", ""Medium"", or ""Low"")
2. ""risks"": An array of strings containing 2 to 4 project-specific risks analyzed from the description, category, difficulty, duration, and team constraints (e.g. ""Potential delay in Figma design approval due to short timeline"").

CRITICAL JSON RULES:
1. Do NOT output any markdown tags (like ```json or ```).
2. Do NOT output any text before or after the JSON object.
3. If you write quotes inside string values (such as in description or title), use single quotes (e.g. 'database') instead of double quotes to prevent breaking the JSON parser.
4. Do NOT include raw newline characters or carriage returns inside any JSON string values. Every string value must be strictly on a single line.";



            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(promptText);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            
            _logger.LogInformation("Sending plan generation request to AI.");
            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                new GeminiPromptExecutionSettings
                {
                    MaxTokens = 8192,
                    Temperature = 0.5
                });

            var resultText = response.Content ?? "{\"plan\":[],\"risks\":[]}";
            _logger.LogInformation("Raw AI Response content: {Content}", resultText);
            resultText = resultText.Replace("```json", "").Replace("```", "").Trim();

            var history = new AiHistory
            {
                UserId = userId,
                Prompt = $"Plan generation for: {request.ProjectGoal}",
                PlanSummary = resultText,
                TokensUsed = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _historyRepository.AddAsync(history);

            return resultText;
        }

        /// <summary>
        /// Builds the full system prompt by injecting RAG context (relevant project data)
        /// into the base system prompt.
        /// </summary>
        private string BuildSystemPromptWithContext(List<string> relevantContext)
        {
            if (relevantContext == null || relevantContext.Count == 0)
            {
                return BaseSystemPrompt;
            }

            var contextSection = new StringBuilder();
            contextSection.AppendLine();
            contextSection.AppendLine("RELEVANT PROJECT DATA (from the user's workspace):");
            contextSection.AppendLine("Use this data to answer the user's question accurately. Only reference this data if the user's question is related to it.");
            contextSection.AppendLine("---");
            foreach (var chunk in relevantContext)
            {
                contextSection.AppendLine(chunk);
                contextSection.AppendLine("---");
            }

            return BaseSystemPrompt + contextSection;
        }

        public async Task<string> GenerateSubtasksAsync(Models.GenerateSubtasksRequestDto request)
        {
            var systemPrompt = @"You are an expert technical project manager and agile coach.
Your job is to break down a complex task into smaller, actionable subtasks (a checklist) to help the assignee execute it effectively.
You MUST output ONLY a valid JSON array of strings. No markdown formatting, no backticks, no introduction, and no extra text.
Each string should be a concise, actionable subtask. Aim for 3 to 6 subtasks depending on the complexity of the task.
Example output:
[\""Design database schema\"", \""Setup Entity Framework migrations\"", \""Create REST API endpoints\""]";

            var promptText = $"Task Title: {request.TaskTitle}\nTask Description: {request.TaskDescription}";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage(promptText);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            
            _logger.LogInformation("Sending subtask generation request to AI for task: {TaskTitle}", request.TaskTitle);
            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                new GeminiPromptExecutionSettings
                {
                    MaxTokens = 1024,
                    Temperature = 0.7
                });

            var resultText = response.Content ?? "[]";
            resultText = resultText.Replace("```json", "").Replace("```", "").Trim();
            return resultText;
        }
    }
}
