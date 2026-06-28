namespace Marketplace.API.OpenApi;

public sealed class EndpointDocRegistry
{
    private readonly Dictionary<(string Method, string Path), EndpointDocEntry> _entries = new();

    public EndpointDocRegistry(IHostEnvironment hostEnvironment)
    {
        foreach (var docsDirectory in ResolveDocsDirectories(hostEnvironment))
            LoadFromDocsDirectory(docsDirectory);
    }

    internal EndpointDocRegistry(IEnumerable<EndpointDocEntry> entries)
    {
        foreach (var entry in entries)
            AddEntry(entry);
    }

    public bool TryGet(string httpMethod, string path, out EndpointDocEntry entry)
    {
        var key = (EndpointPathNormalizer.NormalizeMethod(httpMethod), EndpointPathNormalizer.NormalizePath(path));
        return _entries.TryGetValue(key, out entry!);
    }

    public IReadOnlyCollection<EndpointDocEntry> GetAll() => _entries.Values.ToArray();

    public IReadOnlyCollection<(string Method, string Path)> GetMissingKeys(
        IEnumerable<(string Method, string Path)> expectedKeys) =>
        expectedKeys
            .Where(key => !TryGet(key.Method, key.Path, out _))
            .Distinct()
            .ToArray();

    private static IEnumerable<string> ResolveDocsDirectories(IHostEnvironment hostEnvironment)
    {
        var candidates = new[]
        {
            Path.Combine(hostEnvironment.ContentRootPath, "Docs", "Endpoints"),
            Path.Combine(AppContext.BaseDirectory, "Docs", "Endpoints")
        };

        return candidates.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private void LoadFromDocsDirectory(string docsDirectory)
    {
        if (!Directory.Exists(docsDirectory))
            return;

        foreach (var filePath in Directory.EnumerateFiles(docsDirectory, "*.md"))
        {
            if (Path.GetFileName(filePath).Equals("README.md", StringComparison.OrdinalIgnoreCase))
                continue;

            var markdown = File.ReadAllText(filePath);
            var fileName = Path.GetFileName(filePath);
            foreach (var entry in EndpointMarkdownParser.ParseFile(markdown, fileName))
                AddEntry(entry);
        }
    }

    private void AddEntry(EndpointDocEntry entry)
    {
        var key = (entry.HttpMethod, entry.Path);
        _entries[key] = entry;
    }
}
