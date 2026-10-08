using System;
using System.IO;
using IMoRS.Models;
using Newtonsoft.Json;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace IMoRS.Services;

public static class SettingsService
{
    public static readonly string SettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IMoRS", "settings.json");

    private static AppSettings? _cached;

    public static AppSettings Load()
    {
        if (_cached != null) return _cached;

        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cached = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
                _cached = new AppSettings();
        }
        catch
        {
            _cached = new AppSettings();
        }

        return _cached;
    }

    public static AppSettings LoadNew()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _cached = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
                _cached = new AppSettings();
        }
        catch
        {
            _cached = new AppSettings();
        }

        return _cached;
    }

    public static void Save(AppSettings settings)
    {
        _cached = settings;

        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);

        var json = System.Text.Json.JsonSerializer.Serialize(
            settings,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var tempPath = SettingsPath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(SettingsPath))
            File.Replace(tempPath, SettingsPath, null);
        else
            File.Move(tempPath, SettingsPath);
    }
}