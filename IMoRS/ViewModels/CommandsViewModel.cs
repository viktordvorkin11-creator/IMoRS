using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IMoRS.DTOs;
using IMoRS.Models;
using IMoRS.Services;
using Mapsui;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel
{
    [RelayCommand]
    private void ApplySelectedIcon()
    {
        try
        {
            if (SelectedMarker == null || SelectedSign == null) return;

            var path = SelectedSign.Path!;

            if (path.StartsWith("avares://"))
                path = IconStorageService.SaveIconToTemp(path);

            SelectedMarker.IconPath = path;
            SelectedMarker.Scale = MarkerScale;

            _markerService.UpdateApp(SelectedMarker);
            UpdateMarkerOnMap(SelectedMarker);
            _ = CloseList();
        }
        catch
        {
        }
    }

    [RelayCommand]
    private void SelectIcon(SignItem item)
    {
        if (item == null) return;
        CustomMarkerImagePath = null;
        SelectedSign = item;
        foreach (var img in Images) img.IsSelected = img == item;
    }

    [RelayCommand]
    private void AddMarkerFromPending()
    {
        try
        {
            if (_pendingMarkerX == null || _pendingMarkerY == null)
                return;

            if (SelectedSign == null && string.IsNullOrEmpty(CustomMarkerImagePath))
                return;

            _ = CloseList();

            string iconPath = !string.IsNullOrEmpty(CustomMarkerImagePath)
                ? CustomMarkerImagePath!
                : SelectedSign!.Path!;

            AddMarker(
                _pendingMarkerX.Value,
                _pendingMarkerY.Value,
                iconPath);

            CustomMarkerImagePath = null;
            ClearPendingMarker();
            RemovePendingMarkerLayer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка добавления метки: {ex}");
        }
    }

    [RelayCommand]
    public void Close()
    {
        if (Map != null)
        {
            var vp = Map.Navigator.Viewport;
            MapStateService.Save(new MapState
            {
                X = vp.CenterX,
                Y = vp.CenterY,
                Resolution = vp.Resolution
            });
        }

        App.MainWindow?.Close();
    }

    [RelayCommand]
    public void Minimize() =>
        App.MainWindow!.WindowState = WindowState.Minimized;

    [RelayCommand]
    private async Task OpenPanel1()
    {
        PanelWidth1 = 500;
        if (SelectedMarker == null) return;

        MarkerScale = SelectedMarker.Scale;

        UserImage = !string.IsNullOrEmpty(SelectedMarker.IconPath)
                    && File.Exists(SelectedMarker.IconPath)
            ? new Bitmap(SelectedMarker.IconPath)
            : null;

        await Task.Delay(300);
        DescOpacity = 1;
        IsPanel1Open = true;
        Description = SelectedMarker.Description;
    }

    [RelayCommand]
    private async Task ClosePanel1()
    {
        if (!IsPanel1Open) return;
        cts?.Cancel();
        DeleteMarkerButtonText = "Удалить";
        n = 0;
        PanelWidth1 = 30;
        DescOpacity = 0;
        await Task.Delay(175);
        IsPanel1Open = false;
        IsEditing = false;
        Description = string.Empty;
        ButtonHeight2 = 0;
        await Task.Delay(175);
        ButtonHeight1 = 43;
    }

    [RelayCommand]
    private void OpenPanel2()
    {
        if (_isAddingMarker)
        {
            IsPanel2Open = true;
            PanelWidth2 = 160;
        }
    }

    [RelayCommand]
    private void ClosePanel2()
    {
        PanelWidth2 = 15;
        IsPanel2Open = false;
    }

    [RelayCommand]
    private async Task OpenList()
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
        BorderListHeight = App.MainWindow!.Bounds.Height * 0.7;
        await Task.Delay(200);
        BorderListWidth = App.MainWindow!.Bounds.Width * 0.7;
        await Task.Delay(200);
        IsListOpen = true;
        IconsOpacity = 1;
        IsAddingMarker = false;
    }

    [RelayCommand]
    private async Task CloseList()
    {
        IsListOpen = false;
        OverlayOpacity = 0;
        BorderListWidth = 25;
        await Task.Delay(200);
        BorderListHeight = 25;
        await Task.Delay(200);
        BorderListHeight = 0;
        BorderListWidth = App.MainWindow!.Bounds.Width * 0.7;
        await Task.Delay(200);
        BorderListWidth = 25;
        IconsOpacity = 0;
        if (IsEditing) _ = OpenPanel1();
        RemovePendingMarkerLayer();
    }


    int n;
    [ObservableProperty] private string deleteMarkerButtonText = "Удалить";

    [RelayCommand]
    private void AddOrEditIcon()
    {
        _ = IsEditing ? EditIcon() : AddIcon();
    }

    [RelayCommand]
    private void ApplyDeleteMarker()
    {
        n += 1;
        DeleteMarker();
    }

    private CancellationTokenSource cts;


    [RelayCommand]
    private void DeleteMarker()
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        Task.Run(() => DeleteMarkerAsync(cts.Token));
    }
    
    private async Task DeleteMarkerAsync(CancellationToken token)
    {
        if (!settings.ConfirmDelete || n == 2)
        {
            n = 0;
            if (SelectedMarker == null) return;

            _markerService.Delete(SelectedMarker.Id);

            var feature = _markers.FirstOrDefault(f =>
                f["Marker"] is MarkerDto dto && dto.Id == SelectedMarker.Id);

            if (feature != null) _markers.Remove(feature);

            _markerLayer.Features = _markers;
            _markerLayer.DataHasChanged();
            Map?.Refresh();

            SelectedMarker = null;
            _ = ClosePanel1();
        }
        else
        {
            if (n == 1)
            {
                DeleteMarkerButtonText = "Подтвердить 3";
                await Task.Delay(1000, token);
                DeleteMarkerButtonText = "Подтвердить 2";
                await Task.Delay(1000, token);
                DeleteMarkerButtonText = "Подтвердить 1";
                await Task.Delay(1000, token);
                DeleteMarkerButtonText = "Подтвердить 0";
                await Task.Delay(1000, token);
                DeleteMarkerButtonText = "Удалить";
                n = 0;
            }
        }
    }

    [RelayCommand]
    private async Task EditMarker()
    {
        IsEditing = true;
        OldMarkerScale = SelectedMarker!.Scale;
        MarkerScale = SelectedMarker.Scale;
        EditPartsOpacity = 1;
        SliderHeight = 70;
        ButtonHeight1 = 0;
        await Task.Delay(175);
        ButtonHeight2 = 43;
    }

    [RelayCommand]
    private async Task ApplyChanges()
    {
        SelectedMarker!.Description = Description;
        SelectedMarker.Scale = MarkerScale;
        OldMarkerScale = MarkerScale;
        _markerService.UpdateApp(SelectedMarker);
        IsEditing = false;
        EditPartsOpacity = 0;
        SliderHeight = 0;
        ButtonHeight2 = 0;
        await Task.Delay(175);
        ButtonHeight1 = 43;
    }

    [RelayCommand]
    private async Task CancelChanges()
    {
        _ = ClosePanel1();
        if (OldMarkerScale != 0)
        {
            MarkerScale = OldMarkerScale;
            UpdateMarkerScale(OldMarkerScale);
        }

        IsEditing = false;
        EditPartsOpacity = 0;
        SliderHeight = 0;
        ButtonHeight2 = 0;
        await Task.Delay(175);
        ButtonHeight1 = 43;
    }

    [RelayCommand]
    private void ChangeMode()
    {
        if (IsAddingMarker)
        {
            ClearPendingMarker();
            ClosePanel2();
            RemovePendingMarkerLayer();
        }

        IsAddingMarker = !IsAddingMarker;
    }
}