using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vertex.Repositories;
using Vertex.Services.Interfaces;

namespace Vertex.Services.Services
{
    public class StorageUsageService : IStorageUsageService
    {
        private readonly AppDbContext _dbContext;

        public StorageUsageService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<long> GetUsedBytesAsync(Guid orgId)
        {
            var projectFiles = await _dbContext.ProjectFiles
                .AsNoTracking()
                .Where(file => file.Project.OrgId == orgId && file.FileSize > 0)
                .Select(file => new StorageItem
                {
                    Id = file.Id,
                    Path = file.StoragePath,
                    Size = file.FileSize
                })
                .ToListAsync();

            var taskFiles = await _dbContext.TaskAttachments
                .AsNoTracking()
                .Where(attachment =>
                    attachment.Type == "file" &&
                    attachment.Size > 0 &&
                    attachment.Task != null &&
                    attachment.Task.Project != null &&
                    attachment.Task.Project.OrgId == orgId)
                .Select(attachment => new StorageItem
                {
                    Id = attachment.Id,
                    Path = attachment.Url,
                    Size = attachment.Size ?? 0
                })
                .ToListAsync();

            return projectFiles
                .Concat(taskFiles)
                .GroupBy(item => GetStorageKey(item), StringComparer.OrdinalIgnoreCase)
                .Sum(group => group.Max(item => item.Size));
        }

        public async Task EnsureCanStoreAsync(Guid orgId, long additionalBytes)
        {
            if (additionalBytes <= 0)
                throw new InvalidOperationException("The uploaded file is empty.");

            var storageLimit = await _dbContext.Organizations
                .AsNoTracking()
                .Where(org => org.Id == orgId)
                .Select(org => (long?)org.StorageLimit)
                .FirstOrDefaultAsync();

            if (!storageLimit.HasValue)
                throw new InvalidOperationException("Organization not found.");

            var storageUsed = await GetUsedBytesAsync(orgId);
            if (additionalBytes > storageLimit.Value || storageUsed > storageLimit.Value - additionalBytes)
            {
                throw new InvalidOperationException(
                    $"Storage quota exceeded. This organization has used {FormatSize(storageUsed)} of {FormatSize(storageLimit.Value)}.");
            }
        }

        private static string GetStorageKey(StorageItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Path))
                return item.Id.ToString();

            return item.Path.Replace('\\', '/').TrimStart('/');
        }

        private static string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            var value = (double)bytes;
            var suffixIndex = 0;

            while (value >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                value /= 1024;
                suffixIndex++;
            }

            return $"{value:0.##} {suffixes[suffixIndex]}";
        }

        private sealed class StorageItem
        {
            public Guid Id { get; init; }
            public string? Path { get; init; }
            public long Size { get; init; }
        }
    }
}
