using System.Text.Json;
using FocusBlocker.Shared;

namespace FocusBlocker.Service.Storage;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _basePath;
    private readonly string _configPath;
    private readonly string _domainPath;

    public ConfigStore()
    {
        _basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FocusBlocker");
        _configPath = Path.Combine(_basePath, "appsettings.json");
        _domainPath = Path.Combine(_basePath, "domains.json");
    }

    public string BasePath => _basePath;
    public string ConfigPath => _configPath;
    public string DomainPath => _domainPath;

    public AppSettings LoadAppSettings()
    {
        EnsureDefaults();
        var json = File.ReadAllText(_configPath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? AppSettings.Default;
    }

    public DomainList LoadDomains()
    {
        EnsureDefaults();
        var json = File.ReadAllText(_domainPath);
        return JsonSerializer.Deserialize<DomainList>(json, JsonOptions) ?? new DomainList(1, Array.Empty<DomainGroup>());
    }

    private void EnsureDefaults()
    {
        Directory.CreateDirectory(_basePath);

        if (!File.Exists(_configPath))
        {
            var json = JsonSerializer.Serialize(AppSettings.Default, JsonOptions);
            File.WriteAllText(_configPath, json);
        }

        if (!File.Exists(_domainPath))
        {
            var json = JsonSerializer.Serialize(new DomainList(1, Array.Empty<DomainGroup>()), JsonOptions);
            File.WriteAllText(_domainPath, json);
        }
    }
}
