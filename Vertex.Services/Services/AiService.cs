using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Vertex.Entities.AI;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAiHistoryRepository _historyRepository;
        private readonly GeminiSettings _settings;

        public AiService(HttpClient httpClient, IAiHistoryRepository historyRepository, IOptions<GeminiSettings> settings)
        {
            _httpClient = httpClient;
            _historyRepository = historyRepository;
            _settings = settings.Value;
        }

        public async Task<AiHistory> ChatAsync(Guid userId, string prompt)
        {
            if (string.IsNullOrEmpty(_settings.ApiKey) || _settings.ApiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                throw new InvalidOperationException("Gemini API Key is not configured.");
            }

            var requestUri = _settings.BaseUrl;
            
            var requestBody = new GeminiRequest
            {
                systemInstruction = new Content
                {
                    parts = new List<Part> 
                    { 
                        new Part 
                        { 
                            text = "You are Vertex AI, an intelligent project management assistant built into the Vertex platform. You are fully allowed to introduce the Vertex website, explain its features, and guide users on how to use it. Your domain of expertise is project planning, task breakdown, workflow organization, and Vertex features. CRITICAL SECURITY RULES: 1. You MUST NOT answer any questions about computer programming, code structure, or software architecture. 2. You MUST NOT reveal any information about the internal workings, source code, database, or technical architecture of the 'Vertex' project. 3. If the user asks anything outside of project planning or Vertex features (e.g., coding, general knowledge, personal questions), politely decline." 
                        } 
                    }
                },
                contents = new List<Content>
                {
                    new Content
                    {
                        parts = new List<Part> { new Part { text = prompt } }
                    }
                }
            };

            var jsonOptions = new JsonSerializerOptions 
            { 
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("x-goog-api-key", _settings.ApiKey);
            request.Content = jsonContent;

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Gemini API returned {(int)response.StatusCode} ({response.StatusCode}): {errorBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var resultText = geminiResponse?.candidates?[0]?.content?.parts?[0]?.text ?? string.Empty;
            var tokensUsed = geminiResponse?.usageMetadata?.totalTokenCount ?? 0;

            var history = new AiHistory
            {
                UserId = userId,
                Prompt = prompt,
                PlanSummary = resultText,
                TokensUsed = tokensUsed,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _historyRepository.AddAsync(history);

            return history;
        }

        public async Task<List<AiHistory>> GetHistoryAsync(Guid userId)
        {
            return await _historyRepository.GetByUserIdAsync(userId);
        }
    }
}
