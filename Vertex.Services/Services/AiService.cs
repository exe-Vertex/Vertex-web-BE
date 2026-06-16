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
    }
}
