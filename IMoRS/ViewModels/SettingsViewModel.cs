using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IMoRS.Data;
using IMoRS.DTOs;
using IMoRS.Models;
using IMoRS.Services;
using Mapsui.Projections;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty] private AppSettings settings;

    private void InitializeSettings()
    {
        Settings = SettingsService.Load();
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        if (!IsEditing)
        {
            _ = ClosePanel1();
        }
        else
        {
            if (!IsPanel1Open) return;
            PanelWidth1 = 30;
            DescOpacity = 0;
            await Task.Delay(175);
            IsPanel1Open = false;
        }

        ClosePanel2();
        OverlayOpacity = 0.67;
        BorderSettingsHeight = App.MainWindow!.Bounds.Height * 0.7;
        await Task.Delay(200);
        BorderSettingsWidth = App.MainWindow!.Bounds.Width * 0.7;
        await Task.Delay(200);
        IsListOpen = true;
        SettingsOpacity = 1;
    }

    [RelayCommand]
    private async Task CloseSettings()
    {
        IsListOpen = false;
        SettingsOpacity = 0;
        OverlayOpacity = 0;
        BorderSettingsWidth = 25;
        await Task.Delay(200);
        BorderSettingsHeight = 25;
        await Task.Delay(200);
        BorderSettingsHeight = 0;
        await Task.Delay(200);
        BorderSettingsWidth = 25;
        IconsOpacity = 0;
        RemovePendingMarkerLayer();
        ResetSettings();
    }

    [RelayCommand]
    private void ApplySettings()
    {
        SettingsService.Save(Settings);
    }

    [RelayCommand]
    private void ApplySettingsAndClose()
    {
        SettingsService.Save(Settings);
        CloseSettings();
    }

    [RelayCommand]
    private void ResetSettings()
    {
        Settings = SettingsService.LoadNew();
    }

    [RelayCommand]
    private void SaveCurrentMapAsDefault()
    {
        if (Map == null) return;

        var vp = Map.Navigator.Viewport;

        var (lon, lat) = SphericalMercator.ToLonLat(vp.CenterX, vp.CenterY);

        Settings.DefaultMapX = lon;
        Settings.DefaultMapY = lat;
        Settings.DefaultMapZoom = Math.Log2(156543.03392804097 / vp.Resolution);

        SettingsService.Save(Settings);
    }

    public static readonly string IconsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IMoRS", "Icons");

    [RelayCommand]
    private async Task OpenFolder()
    {
        if (!Directory.Exists(IconsPath))
        {
            Directory.CreateDirectory(IconsPath);
        }
        Process.Start("explorer.exe", IconsPath);
    }

    [RelayCommand]
    private async Task ExportDatabase()
    {
        if (App.MainWindow == null) return;

        var file = await App.MainWindow.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Экспорт меток",
                SuggestedFileName = $"markers_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            });

        if (file == null) return;

        try
        {
            var markers = _markerService.GetAll();
            await DatabaseBackupService.ExportAsync(file.Path.LocalPath, markers);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Экспорт не удался: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportDatabase()
    {
        if (App.MainWindow == null) return;

        var files = await App.MainWindow.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Импорт меток",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
                }
            });

        if (files.Count == 0) return;

        try
        {
            var imported = await DatabaseBackupService.ImportAsync(files[0].Path.LocalPath);
            if (imported == null || imported.Count == 0)
            {
                Console.WriteLine("Файл пуст или не содержит меток");
                return;
            }

            ReplaceAllMarkers(imported);

            ReloadMarkers();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Импорт не удался: {ex.Message}");
        }
    }
    private void ReplaceAllMarkers(List<MarkerDto> imported)
    {
        _markerService.DeleteAll();

        foreach (var m in imported)
        {
            _markerService.Add(m.X, m.Y, m.IconPath ?? "", m.Scale, m.Description);
        }
    }
}