using System;
using System.IO;
using System.Text.Json;
using IMoRS.Models;

namespace IMoRS.Services;

/// <summary>
/// Сохраняет и загружает состояние карты в JSON-файл.
/// Файл лежит в %AppData%\IMoRS\mapState.json.
/// </summary>
public static class MapStateService
{
    public static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IMoRS",
        "mapState.json");

    public static void Save(MapState state)
    {
        if (!IsFinite(state.X) || !IsFinite(state.Y) || !IsFinite(state.Resolution))
            return;

        var dir = Path.GetDirectoryName(StatePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(state);
        var tempPath = StatePath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(StatePath))
            File.Replace(tempPath, StatePath, null);
        else
            File.Move(tempPath, StatePath);
    }

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    public static MapState? Load()
    {
        if (!File.Exists(StatePath))
            return null;

        try
        {
            var json = File.ReadAllText(StatePath);
            return JsonSerializer.Deserialize<MapState>(json);
        }
        catch
        {
            return null;
        }
    }
}