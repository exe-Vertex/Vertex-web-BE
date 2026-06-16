#pragma warning disable SKEXP0001
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Memory;
using Vertex.Repositories.Interfaces;
using Vertex.Services.Interfaces;

namespace Vertex.Services.Services
{
    /// <summary>
    /// Handles data ingestion (syncing Projects/Tasks from DB into Vector Store)
    /// and semantic search (RAG retrieval) for the AI chat feature.
    /// </summary>
    public class AiSyncService : IAiSyncService
    {
        private static string GetCollectionName(Guid orgId) => $"vertex-{orgId}";

        private readonly IProjectRepository _projectRepository;
        private readonly ISemanticTextMemory _memory;
        private readonly ILogger<AiSyncService> _logger;

        public AiSyncService(
            IProjectRepository projectRepository,
            ISemanticTextMemory memory,
            ILogger<AiSyncService> logger)
        {
            _projectRepository = projectRepository;
            _memory = memory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> SyncProjectDataAsync(Guid orgId)
        {
            _logger.LogInformation("Starting data sync for organization {OrgId}...", orgId);
            var collectionName = GetCollectionName(orgId);

            var projects = await _projectRepository.GetByOrgIdAsync(orgId);
            int chunkCount = 0;

            foreach (var project in projects)
            {
                // --- Chunk 1: Project overview ---
                var projectText = new StringBuilder();
                projectText.AppendLine($"Project: {project.Name}");
                projectText.AppendLine($"Description: {project.Description ?? "No description"}");
                projectText.AppendLine($"Deadline: {project.Deadline:yyyy-MM-dd}");
                projectText.AppendLine($"Created: {project.CreatedAt:yyyy-MM-dd}");

                await _memory.SaveInformationAsync(
                    collection: collectionName,
                    id: $"project-{project.Id}",
                    text: projectText.ToString(),
                    description: $"Overview of project '{project.Name}'");
                chunkCount++;

                // --- Chunk 2: Project members ---
                var members = await _projectRepository.GetMembersByProjectIdAsync(project.Id);
                if (members.Any())
                {
                    var membersText = new StringBuilder();
                    membersText.AppendLine($"Members of project '{project.Name}':");
                    foreach (var member in members)
                    {
                        var memberName = member.User?.Name ?? "Unknown";
                        membersText.AppendLine($"  - {memberName} (Role: {member.Role}, Joined: {member.JoinedAt:yyyy-MM-dd})");
                    }

                    await _memory.SaveInformationAsync(
                        collection: collectionName,
                        id: $"project-members-{project.Id}",
                        text: membersText.ToString(),
                        description: $"Members of project '{project.Name}'");
                    chunkCount++;
                }

                // --- Chunk 3+: Tasks (grouped by status) ---
                var tasks = await _projectRepository.GetTasksByProjectIdAsync(project.Id);
                if (tasks.Any())
                {
                    var statusGroups = tasks.GroupBy(t => t.Status);
                    foreach (var group in statusGroups)
                    {
                        var tasksText = new StringBuilder();
                        tasksText.AppendLine($"Tasks in project '{project.Name}' with status '{group.Key}':");
                        foreach (var task in group)
                        {
                            var assigneeName = task.Assignee?.Name ?? "Unassigned";
                            tasksText.AppendLine($"  - [{task.Priority}] {task.Title}: {task.Description ?? "No description"} (Assigned to: {assigneeName}, Due: {task.EndDate:yyyy-MM-dd})");
                        }

                        await _memory.SaveInformationAsync(
                            collection: collectionName,
                            id: $"project-tasks-{project.Id}-{group.Key}",
                            text: tasksText.ToString(),
                            description: $"Tasks with status '{group.Key}' in project '{project.Name}'");
                        chunkCount++;
                    }
                }
            }

            _logger.LogInformation("Data sync completed. {ChunkCount} chunks saved to vector store for org {OrgId}.", chunkCount, orgId);
            return chunkCount;
        }

        /// <inheritdoc />
        public async Task<List<string>> SearchRelevantContextAsync(Guid orgId, string query, int limit = 3)
        {
            var results = new List<string>();
            var collectionName = GetCollectionName(orgId);

            try
            {
                await foreach (var result in _memory.SearchAsync(
                    collection: collectionName,
                    query: query,
                    limit: limit,
                    minRelevanceScore: 0.5))
                {
                    results.Add(result.Metadata.Text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search failed (store may be empty). Falling back to no context.");
            }

            return results;
        }
    }
}
