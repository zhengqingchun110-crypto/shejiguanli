using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DecorationProjectScheduler.App.Services;

public sealed class SecuritySettingsService
{
    private const string DefaultPassword = "081122";
    private readonly string settingsPath;

    public SecuritySettingsService()
    {
        var appRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DecorationProjectScheduler");
        Directory.CreateDirectory(appRoot);
        settingsPath = Path.Combine(appRoot, "security-settings.json");
    }

    public bool VerifySensitivePassword(string password)
    {
        var settings = Load();
        return string.Equals(settings.SensitivePasswordHash, Hash(password), StringComparison.OrdinalIgnoreCase);
    }

    public void ChangeSensitivePassword(string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("新密码不能为空。");
        }

        if (!VerifySensitivePassword(currentPassword))
        {
            throw new InvalidOperationException("当前密码不正确。");
        }

        Save(new SecuritySettings { SensitivePasswordHash = Hash(newPassword.Trim()) });
    }

    private SecuritySettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            var defaults = new SecuritySettings { SensitivePasswordHash = Hash(DefaultPassword) };
            Save(defaults);
            return defaults;
        }

        var json = File.ReadAllText(settingsPath, Encoding.UTF8);
        var settings = JsonSerializer.Deserialize<SecuritySettings>(json) ?? new SecuritySettings();
        if (string.IsNullOrWhiteSpace(settings.SensitivePasswordHash))
        {
            settings.SensitivePasswordHash = Hash(DefaultPassword);
            Save(settings);
        }

        return settings;
    }

    private void Save(SecuritySettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(settingsPath, json, Encoding.UTF8);
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private sealed class SecuritySettings
    {
        public string SensitivePasswordHash { get; set; } = string.Empty;
    }
}
