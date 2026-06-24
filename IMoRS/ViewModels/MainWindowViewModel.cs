using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using IMoRS.DTOs;
using IMoRS.Models;
using IMoRS.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using Mapsui.Rendering.Skia;
using Mapsui.Styles;
using Mapsui.UI;
using Avalonia.Platform;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private Map? _map;

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
    public ObservableCollection<SignItem> Images { get; set; }
    
    private const int PageSize = 50;

    private readonly MarkerService _markerService = new();

    private readonly List<IFeature> _markers = [];

    private double? _pendingMarkerX;

    private double? _pendingMarkerY;

    public bool HasPendingMarker => _pendingMarkerX.HasValue && _pendingMarkerY.HasValue;


    public int TotalPages => Images.Count == 0 ? 1 : (int)Math.Ceiling(Images.Count / (double)PageSize);

    private readonly Dictionary<string, int> _bitmapIds = new();

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
            // Конвертируем путь если это avares
            var iconPath = marker.IconPath;
            if (iconPath.StartsWith("avares://"))
            {
                // Если путь из ресурсов, сохраняем во временную папку
                iconPath = SaveIconToTemp(iconPath);
                marker.IconPath = iconPath; // Обновляем в DTO
            }
        
            if (!File.Exists(iconPath))
            {
                Console.WriteLine($"Warning: Icon file not found: {iconPath}");
                continue;
            }

            var feature = new PointFeature(
                SphericalMercator.FromLonLat(marker.X, marker.Y));

            feature["Marker"] = marker;

            feature.Styles.Add(new ImageStyle
            {
                Image = $"file://{iconPath}",
                SymbolScale = 0.5
            });

            _markers.Add(feature);
            Console.WriteLine($"Added marker: {marker.X}, {marker.Y}");
        }

        _markerLayer.Features = _markers;
        map.Layers.Add(_markerLayer);
        
        var (centerX, centerY) = SphericalMercator.FromLonLat(82.9204, 55.0302);
        map.Navigator.CenterOn(centerX, centerY);
        map.Navigator.ZoomTo(12);

        Map = map;
        Map.Navigator.ViewportChanged += OnViewportChanged;
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        if (_markerLayer == null) return;

        var resolution = Map.Navigator.Viewport.Resolution;

        double scale;
        double baseSize = 0.5;

        if (resolution > 5000)
        {
            scale = baseSize;
        }
        else if (resolution > 1000)
        {
            double t = (5000 - resolution) / 4000;
            scale = baseSize * (1 - t * 0.7);
        }
        else
        {
            scale = baseSize * 0.3;
        }

        foreach (var feature in _markerLayer.Features)
        {
            var style = feature.Styles.OfType<SymbolStyle>().FirstOrDefault();

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
        
            Console.WriteLine($"Saved icon to: {tempPath}");
            return tempPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving icon: {ex.Message}");
            return avaresPath;
        }
    }

    private void AddMarker(double x, double y, string iconPath)
    {
        Console.WriteLine($"=== ADD MARKER START ===");
        Console.WriteLine($"Coordinates (Lon/Lat): {x}, {y}");
        Console.WriteLine($"Icon path: {iconPath}");
    
        // Сохраняем иконку в temp и получаем физический путь
        var physicalPath = SaveIconToTemp(iconPath);
        Console.WriteLine($"Physical path: {physicalPath}");
        Console.WriteLine($"Icon exists: {File.Exists(physicalPath)}");
    
        if (_markerLayer == null || Map == null)
            return;
    
        _markerService.Add(x, y, physicalPath);
        Console.WriteLine("Marker saved to database");
    
        var (mercatorX, mercatorY) = SphericalMercator.FromLonLat(x, y);
        Console.WriteLine($"Mercator coordinates: {mercatorX}, {mercatorY}");
    
        var feature = new PointFeature(mercatorX, mercatorY);
    
        var markerDto = new MarkerDto
        {
            X = x,
            Y = y,
            IconPath = physicalPath,
            ImagePath = physicalPath
        };
        feature["Marker"] = markerDto;
    
        var imageStyle = new ImageStyle
        {
            Image = $"file://{physicalPath}",
            SymbolScale = 0.5,
            Opacity = 1,
            Enabled = true,
        };
        
        feature.Styles.Add(imageStyle);
        Console.WriteLine("Style added to feature");
    
        _markers.Add(feature);
        Console.WriteLine($"Total markers in collection: {_markers.Count}");
    
        _markerLayer.Features = new List<IFeature>(_markers);
        Console.WriteLine($"Layer features in Map: {_markerLayer.Features.Count()}");
        Console.WriteLine($"First feature: {_markerLayer.Features.FirstOrDefault()?.GetType()}");
        Console.WriteLine($"Layer features count: {_markerLayer.Features.Count()}");
    
        _markerLayer.Enabled = true;
        
        _markerLayer.DataHasChanged();
        Console.WriteLine("DataHasChanged called");
    
        Map.Refresh();
        Console.WriteLine("Map refreshed");
        Console.WriteLine($"=== ADD MARKER END ===");
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
        StatusMessage = "Кликните по карте для выбора места";
        IsAddingMarker = true;
        Console.WriteLine($"SetPendingMarker called: {x}, {y}"); // Добавьте это
    }
    
    [RelayCommand]
    private void AddMarkerFromPending()
    {
        Console.WriteLine("=== AddMarkerFromPending START ===");
    
        CloseList();
    
        if (!_pendingMarkerX.HasValue || !_pendingMarkerY.HasValue)
        {
            Console.WriteLine("No pending marker coordinates");
            return;
        }
    
        if (SelectedSign == null)
        {
            Console.WriteLine("No sign selected");
            return;
        }
    
        Console.WriteLine($"Selected sign path: {SelectedSign.Path}");
        Console.WriteLine($"Pending coords: {_pendingMarkerX.Value}, {_pendingMarkerY.Value}");
    
        AddMarker(
            _pendingMarkerX.Value,
            _pendingMarkerY.Value,
            SelectedSign.Path);

        ClearPendingMarker();
        Console.WriteLine("=== AddMarkerFromPending END ===");
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
    public void OpenPanel1()
    {
        PanelWidth1 = App.MainWindow!.Bounds.Width * 0.25;
        if (SelectedMarker == null)
            return;

        UserImage = new Bitmap(SelectedMarker.ImagePath);
    }

    [RelayCommand]
    public void ClosePanel1()
    {
        PanelWidth1 = 30;
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
        ClosePanel1();
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
    }

    [RelayCommand]
    public async Task AddImage()
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
                            Patterns = ["*.jpg", "*.jpeg", "*.png"]
                        }
                    ]
                });
            if (files.Count == 0)
                return;


            var photoPath = files[0].Path.LocalPath;

            SelectedMarker.ImagePath = photoPath;
            _markerService.UpdateApp(SelectedMarker);

            UserImage = new Bitmap(photoPath);
        }
    }

    [RelayCommand]
    private void SelectIcon(SignItem item)
    {
        if (item == null) return;
        SelectedSign = item;
        // Дополнительно можно подсветить выбранный элемент
        foreach (var img in Images)
        {
            img.IsSelected = img == item;
        }
    }
}