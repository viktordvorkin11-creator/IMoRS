using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using IMoRS.DTOs;
using IMoRS.Services;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Загружает все изображения из ресурсов и пользовательской папки.</summary>
    private void LoadImages()
    {
        Images.Clear();

        foreach (var item in IconStorageService.LoadAllImagesFromAssets("Assets/SignIconPng"))
            Images.Add(item);

        var iconsDir = IconStorageService.UserIconsDir;
        Directory.CreateDirectory(iconsDir);

        foreach (var file in Directory.GetFiles(iconsDir))
        {
            try
            {
                Images.Add(new SignItem
                {
                    Image = new Bitmap(file),
                    Path = file
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки {file}: {ex.Message}");
            }
        }

        FilteredImages = new ObservableCollection<SignItem>(Images);
    }

    private async Task EditIcon()
    {
        if (App.MainWindow == null) return;

        var files = await App.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif"]
                    }
                ],
            });

        if (files.Count == 0) return;

        var photoPath = files[0].Path.LocalPath;

        var sign = new SignItem
        {
            Image = new Bitmap(photoPath),
            Path = photoPath
        };

        Images.Add(sign);
        FilteredImages.Add(sign);

        foreach (var img in Images) img.IsSelected = false;

        SelectedMarker!.IconPath = IconStorageService.ResizeAndSaveImage(photoPath);
        _markerService.UpdateApp(SelectedMarker);

        UpdateMarkerOnMap(SelectedMarker);

        UserImage = new Bitmap(photoPath);

        var savedPath = Path.Combine(
            IconStorageService.UserIconsDir,
            Guid.NewGuid() + Path.GetExtension(photoPath));

        File.Copy(photoPath, savedPath, true);
    }

    private async Task AddIcon()
    {
        if (App.MainWindow == null) return;

        var files = await App.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Выберите изображение для метки",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Изображения")
                    {
                        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif"]
                    }
                ]
            });

        if (files.Count == 0) return;

        CustomMarkerImagePath = IconStorageService.ResizeAndSaveImage(files[0].Path.LocalPath);

        var sign = new SignItem
        {
            Image = new Bitmap(CustomMarkerImagePath),
            Path = CustomMarkerImagePath
        };

        Images.Add(sign);
        FilteredImages.Add(sign);

        SelectIcon(sign);
    }
}