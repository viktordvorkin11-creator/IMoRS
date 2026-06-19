using System.IO;
using System.Text.Json;
using IMoRS.Models;

namespace IMoRS.Services;

public static class MapStateService
{
    private const string FILE_NAME = "mapState.json";

    public static void Save(MapState state)
    {
        var json = JsonSerializer.Serialize(state);
        File.WriteAllText(FILE_NAME, json);
    }

    public static MapState? Load()
    {
        if (!File.Exists(FILE_NAME))
        {
            return null;
        }

        var json = File.ReadAllText(FILE_NAME);
        return JsonSerializer.Deserialize<MapState>(json);
    }
}