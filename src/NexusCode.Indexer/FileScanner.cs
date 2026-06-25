using System.Security.Cryptography;
using NexusCode.Domain;

namespace NexusCode.Indexer;

public sealed class FileScanner : IFileScanner
{
    public async Task<ScanResult> ScanAsync(string repositoryPath, ScanOptions options, CancellationToken ct = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var result = new ScanResult();

        await Task.Run(() =>
        {
            ScanDirectory(repositoryPath, repositoryPath, options, result, ct);
        }, ct);

        result.Duration = DateTimeOffset.UtcNow - startTime;
        return result;
    }

    public async Task<ChangeSet> DetectChangesAsync(string repositoryPath, CancellationToken ct = default)
    {
        var changeSet = new ChangeSet();
        var trackedFilesPath = Path.Combine(repositoryPath, ".nexus", "tracked_files.json");

        Dictionary<string, string> previousHashes = new();
        if (File.Exists(trackedFilesPath))
        {
            var json = await File.ReadAllTextAsync(trackedFilesPath, ct);
            previousHashes = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }

        var currentFiles = new Dictionary<string, string>();
        var options = new ScanOptions();

        ScanDirectory(repositoryPath, repositoryPath, options, new ScanResult(), ct);

        foreach (var csFile in Directory.GetFiles(repositoryPath, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(csFile, repositoryPath, options)) continue;

            var hash = await ComputeHashAsync(csFile, ct);
            currentFiles[csFile] = hash;

            if (!previousHashes.ContainsKey(csFile))
            {
                changeSet.NewFiles.Add(csFile);
            }
            else if (previousHashes[csFile] != hash)
            {
                changeSet.ModifiedFiles.Add(csFile);
            }
        }

        foreach (var trackedFile in previousHashes.Keys)
        {
            if (!currentFiles.ContainsKey(trackedFile))
            {
                changeSet.DeletedFiles.Add(trackedFile);
            }
        }

        var nexusDir = Path.Combine(repositoryPath, ".nexus");
        Directory.CreateDirectory(nexusDir);

        var updatedJson = System.Text.Json.JsonSerializer.Serialize(currentFiles);
        await File.WriteAllTextAsync(trackedFilesPath, updatedJson, ct);

        return changeSet;
    }

    private void ScanDirectory(string rootPath, string currentPath, ScanOptions options, ScanResult result, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            foreach (var file in Directory.GetFiles(currentPath))
            {
                ct.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(file).ToLowerInvariant();
                if (!options.IncludeExtensions.Contains(extension)) continue;

                if (IsExcluded(file, rootPath, options)) continue;

                var fileInfo = new FileInfo(file);
                if (fileInfo.Length > options.MaxFileSize) continue;

                if (extension == ".cs")
                    result.SourceFiles.Add(file);
                else if (extension == ".csproj")
                    result.ProjectFiles.Add(file);
                else if (extension == ".sln")
                    result.SolutionFiles.Add(file);
            }

            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                ct.ThrowIfCancellationRequested();

                if (IsDirectoryExcluded(dir, rootPath, options)) continue;

                ScanDirectory(rootPath, dir, options, result, ct);
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static bool IsExcluded(string filePath, string rootPath, ScanOptions options)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (var part in parts)
        {
            if (options.ExcludePatterns.Contains(part, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsDirectoryExcluded(string dirPath, string rootPath, ScanOptions options)
    {
        var dirName = Path.GetFileName(dirPath);
        return options.ExcludePatterns.Contains(dirName, StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<string> ComputeHashAsync(string filePath, CancellationToken ct = default)
    {
        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToBase64String(hash);
    }
}
