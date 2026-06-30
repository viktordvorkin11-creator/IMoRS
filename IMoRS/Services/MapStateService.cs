using System.IO;
using System.Text.Json;
using IMoRS.Models;

namespace IMoRS.Services;

/// <summary>
/// Сохраняет и загружает состояние карты в JSON-файл
/// </summary>
public static class MapStateService
{
    private const string FILE_NAME = "mapState.json";

    /// <summary>
    /// Сохраняет состояние карты в файл
    /// </summary>
    public static void Save(MapState state)
    {
        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(FILE_NAME, json);
    }

    /// <summary>
    /// Загружает состояние карты из файла
    /// </summary>
    /// <returns>Состояние или null, если файл не найден</returns>
    public static MapState? Load()
    {
        if (!File.Exists(FILE_NAME))
            return null;

        var json = File.ReadAllText(FILE_NAME);
        return JsonSerializer.Deserialize<MapState>(json);
    }
}