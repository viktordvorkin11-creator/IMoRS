using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IMoRS.DTOs;
using IMoRS.Services;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private Map? _map;

    [ObservableProperty] private double imDescOpacity = 0;

    [ObservableProperty] private bool isMaximized = false;

    [ObservableProperty] private double panelWidth1 = 30;

    [ObservableProperty] private double panelWidth2 = 15;

    [ObservableProperty] private bool isAddingMarker = false;

    [ObservableProperty] private double arrowAngle;

    [ObservableProperty] private bool _isPanel2Open;

    [ObservableProperty] private double borderListWidth = 25;

    [ObservableProperty] private double borderListHeight = 0;

    [ObservableProperty] private bool isListOpen;

    [ObservableProperty] private double overlayOpacity;

    [ObservableProperty] private MarkerDto? selectedMarker;

    [ObservableProperty] private Bitmap? userImage;

    [ObservableProperty] private double iconsOpacity = 0;

    [ObservableProperty] private double appOpacity = 0;

    [ObservableProperty] private bool _isMapVisible = false;

    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty] private Bitmap? selectedIcon;

    [ObservableProperty] private string? selectedIconPath;

    [ObservableProperty] private SignItem? selectedSign;

    [ObservableProperty] private ObservableCollection<SignItem> _filteredImages = new();

    [ObservableProperty] private bool _isEditing = false;

    [ObservableProperty] private bool _isPanel1Open = false;

    [ObservableProperty] private double editPartsOpacity = 0;

    [ObservableProperty] private string? customMarkerImagePath;

    [ObservableProperty] private string description = string.Empty;
    
    [ObservableProperty] private double buttonHeight1 = 43;
    
    [ObservableProperty] private double buttonHeight2 = 0;

    public ObservableCollection<SignItem> Images { get; set; }

    private const int PageSize = 50;

    private readonly MarkerService _markerService = new();

    private readonly List<IFeature> _markers = [];

    private double? _pendingMarkerX;

    private double? _pendingMarkerY;

    public MainWindowViewModel()
    {
        Images = new ObservableCollection<SignItem>();

        AppOpacity = 0;
        IconsOpacity = 0;
        CreateMap();
        LoadImages();
        LoadApp();
    }

    private async Task LoadApp()
    {
        await Task.Delay(1000);
        AppOpacity = 1;
        await Task.Delay(1000);
        IsMapVisible = true;
    }

    private void LoadImages()
    {
        Images.Clear();

        foreach (var item in LoadAllImagesFromAssets("Assets/SignIconPng"))
        {
            Images.Add(item);
        }

        var iconsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IMoRS",
            "Icons");

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

    private List<SignItem> LoadAllImagesFromAssets(string assetsFolderPath)
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

    private readonly MemoryLayer _markerLayer = new()
    {
        Style = null,
        Name = "Merkers",
        Features = new List<IFeature>()
    };

    private void CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _markerLayer.Name = "Markers";
        _markerLayer.Features = new List<IFeature>();

        foreach (var marker in _markerService.GetAll())
        {
            var iconPath = marker.IconPath;

            if (string.IsNullOrEmpty(iconPath))
                continue;

            if (iconPath.StartsWith("avares://"))
            {
                iconPath = SaveIconToTemp(iconPath);
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
                SymbolScale = 0.2
            });

            _markers.Add(feature);
        }

        _markerLayer.Features = _markers;
        map.Layers.Add(_markerLayer);

        var state = MapStateService.Load();

        if (state != null && state.X > 0 && state.Y > 0)
        {
            map.Navigator.CenterOn(state.X, state.Y);
            map.Navigator.ZoomTo(state.Resolution > 0 ? state.Resolution : 12);
        }
        else
        {
            var (centerX, centerY) = SphericalMercator.FromLonLat(82.9204, 55.0302);
            map.Navigator.CenterOn(centerX, centerY);
            map.Navigator.ZoomTo(12);
        }
        
        Map = map;
    }

    private void UpdateMarkerOnMap(MarkerDto marker)
    {
        var feature = _markers.FirstOrDefault(f =>
        {
            if (f["Marker"] is MarkerDto dto)
                return dto.Id == marker.Id;

            return false;
        });

        if (feature == null)
            return;

        feature.Styles.Clear();

        feature.Styles.Add(new ImageStyle
        {
            Image = $"file:///{marker.IconPath!.Replace("\\", "/")}",
            SymbolScale = 0.2,
            Opacity = 1,
            Enabled = true
        });

        feature["Marker"] = marker;

        _markerLayer.DataHasChanged();
        Map.Refresh();
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        if (_markerLayer == null) return;

        var resolution = Map.Navigator.Viewport.Resolution;

        double scale = 0.15;

        if (resolution > 1000)
        {
            scale = 0;
        }

        foreach (var feature in _markerLayer.Features)
        {
            var style = feature.Styles.OfType<ImageStyle>().FirstOrDefault();

            if (style != null)
            {
                style.SymbolScale = scale;
            }
        }

        OnPropertyChanged(nameof(Map));
    }

    private string SaveIconToTemp(string avaresPath)
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
        catch (Exception ex)
        {
            return avaresPath;
        }
    }

    private void AddMarker(double x, double y, string iconPath)
    {
        var physicalPath = SaveIconToTemp(iconPath);

        if (_markerLayer == null || Map == null)
            return;

        _markerService.Add(x, y, physicalPath);

        var (mercatorX, mercatorY) = SphericalMercator.FromLonLat(x, y);

        var feature = new PointFeature(mercatorX, mercatorY);

        var markerDto = new MarkerDto
        {
            X = x,
            Y = y,
            IconPath = physicalPath
        };

        feature["Marker"] = markerDto;

        feature.Styles.Add(new ImageStyle
        {
            Image = $"file:///{physicalPath.Replace("\\", "/")}",
            SymbolScale = 0.2,
            Opacity = 1,
            Enabled = true
        });

        _markers.Add(feature);

        _markerLayer.Features = new List<IFeature>(_markers);

        _markerLayer.DataHasChanged();
        Map.Refresh();
    }

    public async Task EditIcon()
    {
        if (App.MainWindow != null)
        {
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

            if (files.Count == 0)
                return;

            var photoPath = files[0].Path.LocalPath;
            
            var sign = new SignItem
            {
                Image = new Bitmap(photoPath),
                Path = photoPath
            };

            Images.Add(sign);
            FilteredImages.Add(sign);

            SelectedMarker.IconPath = photoPath;
            _markerService.UpdateApp(SelectedMarker);

            UpdateMarkerOnMap(SelectedMarker);

            UserImage = new Bitmap(photoPath);
            
            var iconsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IMoRS",
                "Icons");

            Directory.CreateDirectory(iconsDir);

            var fileName = Guid.NewGuid() + Path.GetExtension(photoPath);
            var savedPath = Path.Combine(iconsDir, fileName);

            File.Copy(photoPath, savedPath, true);

            CloseList();
        }
    }

    private async Task AddIcon()
    {
        if (App.MainWindow == null)
            return;

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

        if (files.Count == 0)
            return;

        CustomMarkerImagePath = files[0].Path.LocalPath;
        
        var sign = new SignItem
        {
            Image = new Bitmap(CustomMarkerImagePath),
            Path = CustomMarkerImagePath
        };

        Images.Add(sign);
        FilteredImages.Add(sign);

        var iconsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IMoRS",
            "Icons");

        Directory.CreateDirectory(iconsDir);

        var fileName = Guid.NewGuid() + Path.GetExtension(CustomMarkerImagePath);
        var savedPath = Path.Combine(iconsDir, fileName);

        File.Copy(CustomMarkerImagePath, savedPath, true);
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
        IsAddingMarker = true;
    }
    
    private Bitmap LoadBitmap(string path)
    {
        if (path.StartsWith("avares://"))
        {
            using var stream = AssetLoader.Open(new Uri(path));
            return new Bitmap(stream);
        }

        return new Bitmap(path);
    }

    [RelayCommand]
    private void AddMarkerFromPending()
    {
        CloseList();

        string iconPath;

        if (!string.IsNullOrEmpty(CustomMarkerImagePath))
        {
            iconPath = CustomMarkerImagePath;
        }
        else
        {
            iconPath = SelectedSign.Path;
        }

        AddMarker(
            _pendingMarkerX.Value,
            _pendingMarkerY.Value,
            iconPath);

        CustomMarkerImagePath = null;

        ClearPendingMarker();
    }

    [RelayCommand]
    public void Close()
    {
        App.MainWindow?.Close();
    }

    [RelayCommand]
    public void Minimize()
    {
        App.MainWindow?.WindowState = WindowState.Minimized;
    }


    [RelayCommand]
    public void MaxRestore()
    {
        App.MainWindow?.WindowState = App.MainWindow?.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        if (App.MainWindow?.WindowState == WindowState.Maximized)
        {
            IsMaximized = true;
        }
        else
        {
            IsMaximized = false;
        }
    }

    [RelayCommand]
    public async Task OpenPanel1()
    {
        PanelWidth1 = App.MainWindow!.Bounds.Width * 0.25;
        if (SelectedMarker == null)
            return;

        if (!string.IsNullOrEmpty(SelectedMarker.IconPath) && File.Exists(SelectedMarker.IconPath))
        {
            UserImage = new Bitmap(SelectedMarker.IconPath);
        }
        else
        {
            UserImage = null;
        }

        // IsImageAdded = UserImage != null;

        await Task.Delay(300);
        ImDescOpacity = 1;
        IsPanel1Open = true;
        Description = SelectedMarker.Description;
    }

    [RelayCommand]
    public async Task ClosePanel1()
    {
        if (!IsPanel1Open)
            return;
        PanelWidth1 = 30;
        ImDescOpacity = 0;
        await Task.Delay(175);
        IsPanel1Open = false;
        IsEditing = false;
        Description = string.Empty;
        ButtonHeight1 = 43;
        await Task.Delay(175);
        ButtonHeight2 = 0;
    }

    [RelayCommand]
    public void ClosePanel2()
    {
        PanelWidth2 = 15;
        ArrowAngle = 180;
        IsPanel2Open = false;
    }

    [RelayCommand]
    public void OpenPanel2()
    {
        IsPanel2Open = true;

        PanelWidth2 = App.MainWindow!.Bounds.Width * 0.1;
        ArrowAngle = 0;
    }

    [RelayCommand]
    public async Task OpenList()
    {
        if (!IsEditing)
        {
            ClosePanel1();
        }
        else
        {
            if (!IsPanel1Open)
                return;
            PanelWidth1 = 30;
            ImDescOpacity = 0;
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
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    public async Task CloseList()
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
        if (IsEditing)
            OpenPanel1();
    }

    [RelayCommand]
    private void AddOrEditIcon()
    {
        if (IsEditing)
        {
            EditIcon();
        }
        else
        {
            AddIcon();
        }
    }

    [RelayCommand]
    private void SelectIcon(SignItem item)
    {
        if (item == null) return;

        CustomMarkerImagePath = null;

        SelectedSign = item;

        foreach (var img in Images)
        {
            img.IsSelected = img == item;
        }
    }

    [RelayCommand]
    private void DeleteMarker()
    {
        if (SelectedMarker == null)
            return;

        _markerService.Delete(SelectedMarker.Id);

        var feature = _markers.FirstOrDefault(f =>
            f["Marker"] is MarkerDto dto &&
            dto.Id == SelectedMarker.Id);

        if (feature != null)
            _markers.Remove(feature);

        _markerLayer.Features = _markers;
        _markerLayer.DataHasChanged();
        Map?.Refresh();

        SelectedMarker = null;

        ClosePanel1();
    }

    [RelayCommand]
    private async Task EditMarker()
    {
        IsEditing = true;
        EditPartsOpacity = 1;
        
        
        ButtonHeight1 = 0;
        await Task.Delay(175);
        ButtonHeight2 = 43;
    }
    
    [RelayCommand]
    private void ApplySelectedIcon()
    {
        if (SelectedMarker == null || SelectedSign == null)
            return;

        var path = SelectedSign.Path;

        if (path.StartsWith("avares://"))
        {
            path = SaveIconToTemp(path);
        }

        SelectedMarker.IconPath = path;

        _markerService.UpdateApp(SelectedMarker);

        UpdateMarkerOnMap(SelectedMarker);

        CloseList();
    }

    [RelayCommand]
    private async Task SaveDescription()
    {
        SelectedMarker.Description = Description;  
        _markerService.UpdateApp(SelectedMarker);

        ButtonHeight1 = 43;
        await Task.Delay(175);
        ButtonHeight2 = 0;
    }
}