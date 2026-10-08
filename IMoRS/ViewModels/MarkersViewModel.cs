using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using IMoRS.DTOs;
using IMoRS.Services;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel
{
    partial void OnMarkerScaleChanged(double value)
    {
        if (!IsEditing || SelectedMarker == null)
            return;

        UpdateMarkerScale(value);
    }

    /// <summary>Обновляет внешний вид метки на карте.</summary>
    private void UpdateMarkerOnMap(MarkerDto marker)
    {
        var feature = _markers.FirstOrDefault(f =>
            f["Marker"] is MarkerDto dto && dto.Id == marker.Id);

        if (feature == null)
            return;

        feature.Styles.Clear();

        feature.Styles.Add(new ImageStyle
        {
            Image = $"file:///{marker.IconPath!.Replace("\\", "/")}",
            SymbolScale = marker.Scale,
            Opacity = 1,
            Enabled = true
        });

        feature["Marker"] = marker;

        _markerLayer.DataHasChanged();
        Map?.Refresh();
    }

    /// <summary>Обновляет масштаб выбранной метки.</summary>
    private void UpdateMarkerScale(double newScale)
    {
        if (SelectedMarker == null) return;

        SelectedMarker.Scale = newScale;

        var feature = _markers.FirstOrDefault(f =>
            f["Marker"] is MarkerDto dto && dto.Id == SelectedMarker.Id);

        if (feature == null) return;

        feature.Styles.Clear();
        feature.Styles.Add(new ImageStyle
        {
            Image = $"file:///{SelectedMarker.IconPath!.Replace("\\", "/")}",
            SymbolScale = newScale,
            Opacity = 1,
            Enabled = true
        });

        feature["Marker"] = SelectedMarker;

        _markerLayer.DataHasChanged();
        Map?.Refresh();
    }

    /// <summary>Добавляет новую метку на карту.</summary>
    private void AddMarker(double x, double y, string iconPath)
    {
        try
        {
            var physicalPath = IconStorageService.SaveIconToTemp(iconPath);

            if (_markerLayer == null || Map == null)
                return;

            if (!File.Exists(physicalPath))
                throw new FileNotFoundException(
                    $"Файл иконки не найден: {physicalPath}");

            var (mercatorX, mercatorY) =
                SphericalMercator.FromLonLat(x, y);

            var markerDto = _markerService.Add(
                x,
                y,
                physicalPath,
                Settings.DefaultMarkerScale,
                "");

            var feature = new PointFeature(
                mercatorX,
                mercatorY);

            feature["Marker"] = markerDto;

            feature.Styles.Add(new ImageStyle
            {
                Image = $"file:///{physicalPath.Replace("\\", "/")}",
                SymbolScale = markerDto.Scale,
                Opacity = 1,
                Enabled = true
            });

            _markers.Add(feature);
            _markerLayer.Features = new List<IFeature>(_markers);

            _markerLayer.DataHasChanged();
            Map.Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка AddMarker: {ex}");
            throw;
        }
    }

    public void ClearPendingMarker()
    {
        _pendingMarkerX = null;
        _pendingMarkerY = null;
    }

    public void SetPendingMarker(double x, double y)
    {
        _pendingMarkerX = x;
        _pendingMarkerY = y;
    }

    private void ReloadMarkers()
    {
        if (Map == null) return;
        LoadMarkersFromDb();
        Map.Refresh();
        SelectedMarker = null;
    }

    private void LoadMarkersFromDb()
    {
        _markers.Clear();
        var allMarkers = _markerService.GetAll();

        if (allMarkers.Any())
            MarkerScale = allMarkers.First().Scale;

        foreach (var marker in allMarkers)
        {
            var iconPath = marker.IconPath;

            if (string.IsNullOrEmpty(iconPath))
                continue;

            if (iconPath.StartsWith("avares://"))
            {
                iconPath = IconStorageService.SaveIconToTemp(iconPath);
                marker.IconPath = iconPath;
            }

            if (!File.Exists(iconPath))
                continue;

            var feature = new PointFeature(
                SphericalMercator.FromLonLat(marker.X, marker.Y));

            feature["Marker"] = marker;

            feature.Styles.Add(new ImageStyle
            {
                Image = $"file:///{iconPath.Replace("\\", "/")}",
                SymbolScale = marker.Scale
            });

            _markers.Add(feature);
        }

        _markerLayer.Features = _markers;
    }

    private int _n;

    [RelayCommand]
    private void ApplyClearDatabase()
    {
        _n += 1;
        ClearDatabase();
    }

    private CancellationTokenSource _cts;
    [ObservableProperty] private string clearDbButtonText = "Очистить маркеры";

    private void ClearDatabase()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        Task.Run(() => ClearDbAsync(_cts.Token));
    }

    private async Task ClearDbAsync(CancellationToken token)
    {
        if (_n == 2)
        {
            _markerService.DeleteAll();
            ReloadMarkers();
            ClearDbButtonText = "Очистить маркеры";
        }
        else
        {
            if (_n == 1)
            {
                ClearDbButtonText = "Подтвердить 3";
                await Task.Delay(1000, token);
                ClearDbButtonText = "Подтвердить 2";
                await Task.Delay(1000, token);
                ClearDbButtonText = "Подтвердить 1";
                await Task.Delay(1000, token);
                ClearDbButtonText = "Подтвердить 0";
                await Task.Delay(1000, token);
                ClearDbButtonText = "Очистить маркеры";
                n = 0;
            }
        }
    }
}