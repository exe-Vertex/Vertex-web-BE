#pragma warning disable SKEXP0001, SKEXP0050
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Memory;

namespace Vertex.Services.Services
{
    /// <summary>
    /// A persistent memory store that delegates to VolatileMemoryStore for in-memory
    /// operations and automatically saves/loads all data to a JSON file on disk.
    /// This ensures embeddings survive server restarts without requiring an external DB.
    /// </summary>
    public class JsonMemoryStore : IMemoryStore
    {
        private readonly VolatileMemoryStore _inner = new();
        private readonly string _filePath;
        private readonly SemaphoreSlim _saveLock = new(1, 1);

        // In-memory mirror of all records, keyed by collection → id → record
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SerializableMemoryRecord>> _data = new();

        public JsonMemoryStore(string filePath)
        {
            _filePath = filePath;
            LoadFromDisk();
        }

        // ── Collection operations ──────────────────────────

        public async Task CreateCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            await _inner.CreateCollectionAsync(collectionName, cancellationToken);
            _data.TryAdd(collectionName, new ConcurrentDictionary<string, SerializableMemoryRecord>());
        }

        public async Task<bool> DoesCollectionExistAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            return await _inner.DoesCollectionExistAsync(collectionName, cancellationToken);
        }

        public IAsyncEnumerable<string> GetCollectionsAsync(CancellationToken cancellationToken = default)
        {
            return _inner.GetCollectionsAsync(cancellationToken);
        }

        public async Task DeleteCollectionAsync(string collectionName, CancellationToken cancellationToken = default)
        {
            await _inner.DeleteCollectionAsync(collectionName, cancellationToken);
            _data.TryRemove(collectionName, out _);
            await SaveToDiskAsync();
        }

        // ── Record operations ──────────────────────────────

        public async Task<string> UpsertAsync(string collectionName, MemoryRecord record, CancellationToken cancellationToken = default)
        {
            var result = await _inner.UpsertAsync(collectionName, record, cancellationToken);

            var collection = _data.GetOrAdd(collectionName, _ => new ConcurrentDictionary<string, SerializableMemoryRecord>());
            collection[record.Metadata.Id] = SerializableMemoryRecord.FromMemoryRecord(record);
            await SaveToDiskAsync();

            return result;
        }

        public async IAsyncEnumerable<string> UpsertBatchAsync(string collectionName, IEnumerable<MemoryRecord> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var collection = _data.GetOrAdd(collectionName, _ => new ConcurrentDictionary<string, SerializableMemoryRecord>());

            await foreach (var id in _inner.UpsertBatchAsync(collectionName, records, cancellationToken))
            {
                yield return id;
            }

            // Re-read from inner store to ensure consistency
            foreach (var record in records)
            {
                collection[record.Metadata.Id] = SerializableMemoryRecord.FromMemoryRecord(record);
            }
            await SaveToDiskAsync();
        }

        public async Task<MemoryRecord?> GetAsync(string collectionName, string key, bool withEmbedding = false, CancellationToken cancellationToken = default)
        {
            return await _inner.GetAsync(collectionName, key, withEmbedding, cancellationToken);
        }

        public IAsyncEnumerable<MemoryRecord> GetBatchAsync(string collectionName, IEnumerable<string> keys, bool withEmbeddings = false, CancellationToken cancellationToken = default)
        {
            return _inner.GetBatchAsync(collectionName, keys, withEmbeddings, cancellationToken);
        }

        public async Task RemoveAsync(string collectionName, string key, CancellationToken cancellationToken = default)
        {
            await _inner.RemoveAsync(collectionName, key, cancellationToken);
            if (_data.TryGetValue(collectionName, out var collection))
            {
                collection.TryRemove(key, out _);
            }
            await SaveToDiskAsync();
        }

        public async Task RemoveBatchAsync(string collectionName, IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            await _inner.RemoveBatchAsync(collectionName, keys, cancellationToken);
            if (_data.TryGetValue(collectionName, out var collection))
            {
                foreach (var key in keys) collection.TryRemove(key, out _);
            }
            await SaveToDiskAsync();
        }

        public async Task<(MemoryRecord, double)?> GetNearestMatchAsync(string collectionName, ReadOnlyMemory<float> embedding, double minRelevanceScore = 0, bool withEmbedding = false, CancellationToken cancellationToken = default)
        {
            return await _inner.GetNearestMatchAsync(collectionName, embedding, minRelevanceScore, withEmbedding, cancellationToken);
        }

        public IAsyncEnumerable<(MemoryRecord, double)> GetNearestMatchesAsync(string collectionName, ReadOnlyMemory<float> embedding, int limit, double minRelevanceScore = 0, bool withEmbeddings = false, CancellationToken cancellationToken = default)
        {
            return _inner.GetNearestMatchesAsync(collectionName, embedding, limit, minRelevanceScore, withEmbeddings, cancellationToken);
        }

        // ── Persistence ────────────────────────────────────

        private void LoadFromDisk()
        {
            if (!File.Exists(_filePath)) return;

            try
            {
                var json = File.ReadAllText(_filePath);
                var stored = JsonSerializer.Deserialize<Dictionary<string, List<SerializableMemoryRecord>>>(json);
                if (stored == null) return;

                foreach (var (collectionName, records) in stored)
                {
                    _inner.CreateCollectionAsync(collectionName).GetAwaiter().GetResult();
                    var collection = _data.GetOrAdd(collectionName, _ => new ConcurrentDictionary<string, SerializableMemoryRecord>());

                    foreach (var record in records)
                    {
                        var memoryRecord = record.ToMemoryRecord();
                        _inner.UpsertAsync(collectionName, memoryRecord).GetAwaiter().GetResult();
                        collection[record.Id] = record;
                    }
                }
            }
            catch (Exception)
            {
                // If the file is corrupted, start fresh — the FE auto-sync will repopulate
            }
        }

        private async Task SaveToDiskAsync()
        {
            await _saveLock.WaitAsync();
            try
            {
                var toSerialize = _data.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Values.ToList()
                );

                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(toSerialize, new JsonSerializerOptions { WriteIndented = false });
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception)
            {
                // Silently fail — next save will pick up the data
            }
            finally
            {
                _saveLock.Release();
            }
        }

        // ── Serializable record DTO ────────────────────────

        private class SerializableMemoryRecord
        {
            public string Id { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string AdditionalMetadata { get; set; } = string.Empty;
            public string ExternalSourceName { get; set; } = string.Empty;
            public bool IsReference { get; set; }
            public float[] Embedding { get; set; } = Array.Empty<float>();

            public static SerializableMemoryRecord FromMemoryRecord(MemoryRecord record)
            {
                return new SerializableMemoryRecord
                {
                    Id = record.Metadata.Id,
                    Text = record.Metadata.Text,
                    Description = record.Metadata.Description,
                    AdditionalMetadata = record.Metadata.AdditionalMetadata,
                    ExternalSourceName = record.Metadata.ExternalSourceName,
                    IsReference = record.Metadata.IsReference,
                    Embedding = record.Embedding.ToArray()
                };
            }

            public MemoryRecord ToMemoryRecord()
            {
                return MemoryRecord.LocalRecord(
                    id: Id,
                    text: Text,
                    description: Description,
                    embedding: new ReadOnlyMemory<float>(Embedding),
                    additionalMetadata: AdditionalMetadata
                );
            }
        }
    }
}
