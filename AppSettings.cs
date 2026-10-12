using System.Text.Json;

namespace ClipboardMerge;

internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public bool ShowCopyNotification { get; set; } = true;
    public int NotificationMilliseconds { get; set; } = 1000;
    public bool PlaySound { get; set; } = false;
    public bool AutoStart { get; set; } = true;
    public string Separator { get; set; } = "\r\n";
    public int PreviewLength { get; set; } = 4000;

    public static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipboardMerge");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
