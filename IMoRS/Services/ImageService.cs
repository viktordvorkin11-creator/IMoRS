using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IMoRS.Services;

public static class ImageService
{
    private static readonly string AppDataPath;
    
    static ImageService()
    {
        AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IMoRS",
            "SignIcons"
        );
        
        InitializeImages();
    }
    
    private static void InitializeImages()
    {
        try
        {
            Directory.CreateDirectory(AppDataPath);
            
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            var copiedCount = 0;
            var skippedCount = 0;
            
            foreach (var resourceName in resourceNames)
            {
                if (resourceName.StartsWith("IMoRS.Assets.SignIconPng."))
                {
                    var fileName = resourceName.Substring("IMoRS.Assets.SignIconPng.".Length);
                    var savePath = Path.Combine(AppDataPath, fileName);
                    
                    // Если файл уже есть - пропускаем
                    if (File.Exists(savePath))
                    {
                        skippedCount++;
                        continue;
                    }
                    
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        Debug.WriteLine($"❌ Не удалось загрузить: {fileName}");
                        continue;
                    }
                    
                    using var fileStream = File.Create(savePath);
                    stream.CopyTo(fileStream);
                    copiedCount++;
                    
                    Debug.WriteLine($"✅ {fileName}");
                }
            }
            
            Debug.WriteLine($"✅ Скопировано: {copiedCount}, пропущено: {skippedCount}");
            Debug.WriteLine($"📁 Папка: {AppDataPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ Ошибка: {ex.Message}");
        }
    }
    
    public static string? GetImagePath(string fileName)
    {
        var path = Path.Combine(AppDataPath, fileName);
        return File.Exists(path) ? path : null;
    }
    
    public static string[] GetAllImagePaths()
    {
        return Directory.GetFiles(AppDataPath);
    }
    
    public static string[] GetAllImageNames()
    {
        return Directory.GetFiles(AppDataPath)
            .Select(Path.GetFileName)
            .ToArray();
    }
}