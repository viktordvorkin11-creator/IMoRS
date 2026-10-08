using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using IMoRS.DTOs;
using SkiaSharp;

namespace IMoRS.Services;

public static class IconStorageService
{
    public static string UserIconsDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IMoRS",
        "Icons");

    public static List<SignItem> LoadAllImagesFromAssets(string assetsFolderPath)
    {
        var items = new List<SignItem>();
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var folderUri = new Uri($"avares://{assemblyName}/{assetsFolderPath}");

        foreach (var assetUri in AssetLoader.GetAssets(folderUri, null))
        {
            using var stream = AssetLoader.Open(assetUri);
            items.Add(new SignItem
            {
                Image = new Bitmap(stream),
                Path = assetUri.ToString()
            });
        }
        return items;
    }

    public static string SaveIconToTemp(string avaresPath)
    {
        try
        {
            var fileName = Path.GetFileName(avaresPath);
            var tempPath = Path.Combine(Path.GetTempPath(), "IMoRS", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            if (File.Exists(tempPath))
                return tempPath;

            using var stream = AssetLoader.Open(new Uri(avaresPath));
            using var fileStream = File.Create(tempPath);
            stream.CopyTo(fileStream);

            return tempPath;
        }
        catch
        {
            return avaresPath;
        }
    }

    public static string ResizeAndSaveImage(string sourcePath)
    {
        var iconsDir = UserIconsDir;
        Directory.CreateDirectory(iconsDir);

        var destinationPath = Path.Combine(iconsDir, $"{Guid.NewGuid()}.png");

        int maxSize = SettingsService.Load().MaxIconDimension;

        using var input = File.OpenRead(sourcePath);
        using var codec = SKCodec.Create(input)
            ?? throw new Exception("Не удалось открыть изображение.");

        var info = codec.Info;
        float scale = Math.Min(
            (float)maxSize / info.Width,
            (float)maxSize / info.Height);
        scale = Math.Min(scale, 1f);

        int width = (int)(info.Width * scale);
        int height = (int)(info.Height * scale);

        using var bitmap = SKBitmap.Decode(sourcePath);
        using var resizedBitmap = bitmap.Resize(
            new SKImageInfo(width, height),
            SKSamplingOptions.Default)
            ?? throw new Exception("Ошибка изменения размера.");

        using var image = SKImage.FromBitmap(resizedBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var output = File.OpenWrite(destinationPath);

        data.SaveTo(output);
        return destinationPath;
    }
}