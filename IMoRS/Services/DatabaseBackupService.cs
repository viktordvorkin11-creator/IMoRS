using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using IMoRS.DTOs;
using Microsoft.Data.Sqlite;

namespace IMoRS.Services;

public static class DatabaseBackupService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };
    
    public static void Export(string filePath, IEnumerable<MarkerDto> markers)
    {
        var json = JsonSerializer.Serialize(markers, Options);

        var tempPath = filePath + ".tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(filePath))
            File.Replace(tempPath, filePath, null);
        else
            File.Move(tempPath, filePath);
    }

    public static List<MarkerDto>? Import(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл не найден: {filePath}");

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<MarkerDto>>(json);
    }

    public static Task ExportAsync(string filePath, IEnumerable<MarkerDto> markers)
        => Task.Run(() => Export(filePath, markers));

    public static Task<List<MarkerDto>?> ImportAsync(string filePath)
        => Task.Run(() => Import(filePath));
}