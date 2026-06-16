using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vertex.Services.Interfaces
{
    /// <summary>
    /// Service responsible for syncing project data into the in-memory Vector Store
    /// and performing semantic search (RAG retrieval) against it.
    /// </summary>
    public interface IAiSyncService
    {
        /// <summary>
        /// Reads all projects and tasks belonging to the given organization from the database,
        /// chunks them into text descriptions, generates embeddings, and stores them in
        /// the in-memory Vector Store for RAG retrieval.
        /// </summary>
        Task<int> SyncProjectDataAsync(Guid orgId);

        /// <summary>
        /// Performs a semantic search against the Vector Store to find the most relevant
        /// project/task context chunks for a given user query, scoped to a specific organization.
        /// </summary>
        Task<List<string>> SearchRelevantContextAsync(Guid orgId, string query, int limit = 3);
    }
}
