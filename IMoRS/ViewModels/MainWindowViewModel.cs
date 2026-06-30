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
using FFmpegVideoPlayer.Core;
using IMoRS.DTOs;
using IMoRS.Services;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using SkiaSharp;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Переменные

    [ObservableProperty] private MarkerDto? selectedMarker;
    [ObservableProperty] private Bitmap? userImage;
    [ObservableProperty] private Bitmap? selectedIcon;
    [ObservableProperty] private SignItem? selectedSign;
    [ObservableProperty] private ObservableCollection<SignItem> _filteredImages = new();

    [ObservableProperty] private double iconsOpacity = 0;
    [ObservableProperty] private double appOpacity = 0;
    [ObservableProperty] private double descOpacity = 0;
    [ObservableProperty] private double editPartsOpacity = 0;
    [ObservableProperty] private double overlayOpacity;
    [ObservableProperty] private double panelWidth1 = 30;
    [ObservableProperty] private double panelWidth2 = 15;
    [ObservableProperty] private double borderListWidth = 25;
    [ObservableProperty] private double buttonHeight2 = 0;
    [ObservableProperty] private double borderListHeight = 0;
    [ObservableProperty] private double buttonHeight1 = 43;
    [ObservableProperty] private double sliderHeight = 0;
    [ObservableProperty] private double markerScale = 0.2;
    [ObservableProperty] private double oldMarkerScale;

    [ObservableProperty] private bool _isMaximized = false;
    [ObservableProperty] private bool _isAddingMarker = false;
    [ObservableProperty] private bool _isPanel2Open;
    [ObservableProperty] private bool _isListOpen;
    [ObservableProperty] private bool _isMapVisible = false;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private bool _isPanel1Open = false;

    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string? selectedIconPath;
    [ObservableProperty] private string? customMarkerImagePath;
    [ObservableProperty] private string description = string.Empty;

    public ObservableCollection<SignItem> Images { get; set; }
    private readonly MarkerService _markerService = new();
    private readonly List<IFeature> _markers = [];
    private double? _pendingMarkerX;
    private double? _pendingMarkerY;

    #endregion

    // Конструктор
    public MainWindowViewModel()
    {
        Images = new ObservableCollection<SignItem>();

        AppOpacity = 0;
        IconsOpacity = 0;
        CreateMap();
        LoadImages();
        LoadApp();
    }

    /// <summary>
    /// Выполняет плавное появление приложения с задержками
    /// </summary>
    private async Task LoadApp()
    {
        await Task.Delay(1000);
        AppOpacity = 1;
        await Task.Delay(1000);
        IsMapVisible = true;
    }

    #region Картинки, иконки

    /// <summary>
    /// Загружает все изображения из папки ресурсов и пользовательской директории
    /// </summary>
    private void LoadImages()
    {
        Images.Clear();

        // Загружаем встроенные иконки из ресурсов приложения
        foreach (var item in LoadAllImagesFromAssets("Assets/SignIconPng"))
        {
            Images.Add(item);
        }

        // Определяем путь к пользовательской папке с иконками
        var iconsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IMoRS",
            "Icons");

        Directory.CreateDirectory(iconsDir); // Создаем папку, если её нет

        // Загружаем все пользовательские иконки из папки
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

        // Обновляем отфильтрованный список
        FilteredImages = new ObservableCollection<SignItem>(Images);
    }

    /// <summary>
    /// Загружает все изображения из указанной папки в ресурсах приложения
    /// </summary>
    /// <param name="assetsFolderPath">Путь к папке в ресурсах</param>
    /// <returns>Список элементов изображений</returns>
    private List<SignItem> LoadAllImagesFromAssets(string assetsFolderPath)
    {
        var items = new List<SignItem>();

        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var folderUri = new Uri($"avares://{assemblyName}/{assetsFolderPath}");

        // Перебираем все ресурсы в указанной папке
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

    /// <summary>
    /// Сохраняет иконку из ресурсов во временную папку
    /// </summary>
    /// <param name="avaresPath">Путь к ресурсу</param>
    /// <returns>Физический путь к сохраненному файлу</returns>
    private string SaveIconToTemp(string avaresPath)
    {
        try
        {
            var fileName = Path.GetFileName(avaresPath);
            var tempPath = Path.Combine(Path.GetTempPath(), "IMoRS", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            if (File.Exists(tempPath))
                return tempPath;

            // Копируем из ресурсов во временный файл
            using var stream = AssetLoader.Open(new Uri(avaresPath));
            using var fileStream = File.Create(tempPath);
            stream.CopyTo(fileStream);

            return tempPath;
        }
        catch (Exception ex)
        {
            return avaresPath; // В случае ошибки возвращаем исходный путь
        }
    }

    /// <summary>
    /// Редактирует иконку существующей метки
    /// </summary>
    private async Task EditIcon()
    {
        if (App.MainWindow != null)
        {
            // Открываем диалог выбора файла
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

            // Добавляем новую иконку в коллекцию
            var sign = new SignItem
            {
                Image = new Bitmap(photoPath),
                Path = photoPath
            };

            Images.Add(sign);
            FilteredImages.Add(sign);

            // Сбрасываем выделение
            foreach (var img in Images)
                img.IsSelected = false;

            // Устанавливаем новую иконку для метки
            SelectedMarker.IconPath = ResizeAndSaveImage(photoPath);
            _markerService.UpdateApp(SelectedMarker);

            UpdateMarkerOnMap(SelectedMarker);

            UserImage = new Bitmap(photoPath);

            // Сохраняем иконку в пользовательскую папку
            var iconsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "IMoRS",
                "Icons");

            Directory.CreateDirectory(iconsDir);

            var fileName = Guid.NewGuid() + Path.GetExtension(photoPath);
            var savedPath = Path.Combine(iconsDir, fileName);

            File.Copy(photoPath, savedPath, true);
        }
    }

    /// <summary>
    /// Изменяет размер изображения и сохраняет его в формате PNG
    /// </summary>
    /// <param name="sourcePath">Путь к исходному изображению</param>
    /// <returns>Путь к сохраненному изображению</returns>
    private string ResizeAndSaveImage(string sourcePath)
    {
        var iconsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IMoRS",
            "Icons");

        Directory.CreateDirectory(iconsDir);

        var destinationPath = Path.Combine(
            iconsDir,
            $"{Guid.NewGuid()}.png");

        const int maxSize = 512; // Максимальный размер изображения

        // Используем SkiaSharp для работы с изображениями
        using var input = File.OpenRead(sourcePath);
        using var codec = SKCodec.Create(input);

        if (codec == null)
            throw new Exception("Не удалось открыть изображение.");

        var info = codec.Info;

        // Вычисляем масштаб для сохранения пропорций
        float scale = Math.Min(
            (float)maxSize / info.Width,
            (float)maxSize / info.Height);

        scale = Math.Min(scale, 1f); // Не увеличиваем изображение

        int width = (int)(info.Width * scale);
        int height = (int)(info.Height * scale);

        var resizedInfo = new SKImageInfo(width, height);

        using var bitmap = SKBitmap.Decode(sourcePath);

        // Изменяем размер
        using var resizedBitmap = bitmap.Resize(
            resizedInfo,
            SKSamplingOptions.Default);

        if (resizedBitmap == null)
            throw new Exception("Ошибка изменения размера.");

        using var image = SKImage.FromBitmap(resizedBitmap);

        // Сохраняем в PNG
        using var data = image.Encode(
            SKEncodedImageFormat.Png,
            100);

        using var output = File.OpenWrite(destinationPath);

        data.SaveTo(output);

        return destinationPath;
    }

    /// <summary>
    /// Добавляет новую иконку в коллекцию из файла
    /// </summary>
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

        CustomMarkerImagePath = ResizeAndSaveImage(files[0].Path.LocalPath);

        // Добавляем в коллекцию
        var sign = new SignItem
        {
            Image = new Bitmap(CustomMarkerImagePath),
            Path = CustomMarkerImagePath
        };

        Images.Add(sign);
        FilteredImages.Add(sign);

        SelectIcon(sign); // Выбираем добавленную иконку
    }


    /// <summary>
    /// Применяет выбранную иконку к метке
    /// </summary>
    [RelayCommand]
    private void ApplySelectedIcon()
    {
        try
        {
            if (SelectedMarker == null || SelectedSign == null)
                return;

            var path = SelectedSign.Path;

            // Преобразуем путь из ресурсов
            if (path.StartsWith("avares://"))
            {
                path = SaveIconToTemp(path);
            }

            SelectedMarker.IconPath = path;
            SelectedMarker.Scale = MarkerScale;

            _markerService.UpdateApp(SelectedMarker); // Обновляем в базе

            UpdateMarkerOnMap(SelectedMarker); // Обновляем на карте

            CloseList(); // Закрываем список
        }
        catch
        {
        }
    }

    /// <summary>
    /// Выбирает иконку из списка
    /// </summary>
    [RelayCommand]
    private void SelectIcon(SignItem item)
    {
        if (item == null) return;

        CustomMarkerImagePath = null; // Сбрасываем пользовательскую иконку

        SelectedSign = item;

        // Снимаем выделение со всех иконок, кроме выбранной
        foreach (var img in Images)
        {
            img.IsSelected = img == item;
        }
    }

    #endregion

    #region Работа с картой

    [ObservableProperty] private Map? _map;

    private readonly MemoryLayer _markerLayer = new()
    {
        Style = null,
        Name = "Merkers",
        Features = new List<IFeature>()
    };

    /// <summary>
    /// Создает и настраивает карту с загруженными метками
    /// </summary>
    private void CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer()); // Добавляем слои карты

        _markerLayer.Name = "Markers";
        _markerLayer.Features = new List<IFeature>();

        // Получаем все сохраненные метки из базы
        var allMarkers = _markerService.GetAll();

        // Устанавливаем масштаб первой метки по умолчанию
        if (allMarkers.Any())
        {
            MarkerScale = allMarkers.First().Scale;
        }

        // Добавляем все метки на карту
        foreach (var marker in allMarkers)
        {
            var iconPath = marker.IconPath;

            if (string.IsNullOrEmpty(iconPath))
                continue;

            // Преобразуем путь из ресурсов во временный файл
            if (iconPath.StartsWith("avares://"))
            {
                iconPath = SaveIconToTemp(iconPath);
                marker.IconPath = iconPath;
            }

            if (!File.Exists(iconPath))
                continue;

            // Создаем точку на карте в координатах меркатора
            var feature = new PointFeature(
                SphericalMercator.FromLonLat(marker.X, marker.Y));

            feature["Marker"] = marker; // Сохраняем данные метки

            // Добавляем стиль с изображением
            feature.Styles.Add(new ImageStyle
            {
                Image = $"file:///{iconPath.Replace("\\", "/")}",
                SymbolScale = marker.Scale
            });

            _markers.Add(feature);
        }

        _markerLayer.Features = _markers;
        map.Layers.Add(_markerLayer);

        // Восстанавливаем состояние карты или устанавливаем позицию по умолчанию
        var state = MapStateService.Load();

        if (state != null && state.X > 0 && state.Y > 0)
        {
            map.Navigator.CenterOn(state.X, state.Y);
            map.Navigator.ZoomTo(state.Resolution > 0 ? state.Resolution : 12);
        }
        else
        {
            // Центрируем на Новосибирске
            var (centerX, centerY) = SphericalMercator.FromLonLat(82.9204, 55.0302);
            map.Navigator.CenterOn(centerX, centerY);
            map.Navigator.ZoomTo(12);
        }

        Map = map;
    }

    #endregion

    #region Метки

    /// <summary>
    /// Обновляет значение слайдера при движении 
    /// </summary>
    partial void OnMarkerScaleChanged(double value)
    {
        if (!IsEditing || SelectedMarker == null)
            return;

        UpdateMarkerScale(value);
    }

    /// <summary>
    /// Обновляет внешний вид метки на карте
    /// </summary>
    /// <param name="marker">Обновленные данные метки</param>
    private void UpdateMarkerOnMap(MarkerDto marker)
    {
        // Находим существующую метку по ID
        var feature = _markers.FirstOrDefault(f =>
        {
            if (f["Marker"] is MarkerDto dto)
                return dto.Id == marker.Id;

            return false;
        });

        if (feature == null)
            return;

        // Очищаем старые стили
        feature.Styles.Clear();

        // Добавляем новый стиль с обновленными данными
        feature.Styles.Add(new ImageStyle
        {
            Image = $"file:///{marker.IconPath!.Replace("\\", "/")}",
            SymbolScale = marker.Scale,
            Opacity = 1,
            Enabled = true
        });

        feature["Marker"] = marker; // Обновляем данные

        _markerLayer.DataHasChanged(); // Уведомляем о изменении
        Map.Refresh(); // Обновляем карту
    }

    /// <summary>
    /// Обновляет масштаб выбранной метки
    /// </summary>
    /// <param name="newScale">Новый масштаб</param>
    private void UpdateMarkerScale(double newScale)
    {
        if (SelectedMarker == null) return;

        SelectedMarker.Scale = newScale; // Обновляем данные

        // Находим фичу метки
        var feature = _markers.FirstOrDefault(f =>
        {
            if (f["Marker"] is MarkerDto dto)
                return dto.Id == SelectedMarker.Id;
            return false;
        });

        if (feature == null) return;

        // Обновляем стиль с новым масштабом
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

    /// <summary>
    /// Добавляет новую метку на карту
    /// </summary>
    /// <param name="x">Координата X (долгота)</param>
    /// <param name="y">Координата Y (широта)</param>
    /// <param name="iconPath">Путь к иконке</param>
    private void AddMarker(double x, double y, string iconPath)
    {
        var physicalPath = SaveIconToTemp(iconPath);

        if (_markerLayer == null || Map == null)
            return;

        // Конвертируем координаты в проекцию меркатора
        var (mercatorX, mercatorY) = SphericalMercator.FromLonLat(x, y);

        var feature = new PointFeature(mercatorX, mercatorY);

        // Сохраняем метку в базу данных
        var markerDto = _markerService.Add(x, y, physicalPath, markerScale);

        feature["Marker"] = markerDto;

        // Добавляем стиль
        feature.Styles.Add(new ImageStyle
        {
            Image = $"file:///{physicalPath.Replace("\\", "/")}",
            SymbolScale = markerScale,
            Opacity = 1,
            Enabled = true
        });

        _markers.Add(feature);
        _markerLayer.Features = new List<IFeature>(_markers);

        _markerLayer.DataHasChanged();
        Map.Refresh();
    }

    /// <summary>
    /// Очищает данные ожидающей метки
    /// </summary>
    public void ClearPendingMarker()
    {
        _pendingMarkerX = null;
        _pendingMarkerY = null;
    }

    /// <summary>
    /// Устанавливает координаты для новой метки
    /// </summary>
    public void SetPendingMarker(double x, double y)
    {
        _pendingMarkerX = x;
        _pendingMarkerY = y;
        IsAddingMarker = true;
    }

    #endregion

    #region Комманды

    /// <summary>
    /// Добавляет метку в сохраненных координатах
    /// </summary>
    [RelayCommand]
    private void AddMarkerFromPending()
    {
        try
        {
            CloseList(); // Закрываем список иконок

            string iconPath;

            // Определяем путь к иконке
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

            CustomMarkerImagePath = null; // Сбрасываем пользовательскую иконку

            ClearPendingMarker();
        }
        catch
        {
            return;
        }
    }

    /// <summary>
    /// Закрывает окно
    /// </summary>
    [RelayCommand]
    public void Close()
    {
        App.MainWindow?.Close();
    }

    /// <summary>
    /// Сварачивает окно
    /// </summary>
    [RelayCommand]
    public void Minimize()
    {
        App.MainWindow?.WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Переключает состояние окна между обычным и полноэкранным
    /// </summary>
    [RelayCommand]
    public void MaxRestore()
    {
        App.MainWindow?.WindowState = App.MainWindow?.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        if (App.MainWindow != null)
        {
            if (App.MainWindow.WindowState == WindowState.Maximized)
            {
                App.MainWindow.WindowState = WindowState.Maximized;
                IsMaximized = true;
            }
            else
            {
                App.MainWindow.WindowState = WindowState.Normal;
                App.MainWindow.Width = 1600;
                App.MainWindow.Height = 900;
                IsMaximized = false;
            }
        }
    }

    /// <summary>
    /// Открывает панель информации о метке
    /// </summary>
    [RelayCommand]
    public async Task OpenPanel1()
    {
        PanelWidth1 = 400; // Расширяем панель
        if (SelectedMarker == null)
            return;

        MarkerScale = SelectedMarker.Scale;

        // Загружаем изображение метки
        if (!string.IsNullOrEmpty(SelectedMarker.IconPath) && File.Exists(SelectedMarker.IconPath))
        {
            UserImage = new Bitmap(SelectedMarker.IconPath);
        }
        else
        {
            UserImage = null;
        }

        await Task.Delay(300); // Ждем анимации
        DescOpacity = 1; // Показываем описание
        IsPanel1Open = true;
        Description = SelectedMarker.Description;
    }

    /// <summary>
    /// Закрывает панель информации о метке
    /// </summary>
    [RelayCommand]
    public async Task ClosePanel1()
    {
        if (!IsPanel1Open)
            return;
        PanelWidth1 = 30; // Сворачиваем панель
        DescOpacity = 0; // Скрываем описание
        await Task.Delay(175);
        IsPanel1Open = false;
        IsEditing = false;
        Description = string.Empty;
        ButtonHeight2 = 0;
        await Task.Delay(175);
        ButtonHeight1 = 43; // Восстанавливаем кнопки
    }

    /// <summary>
    /// Открывает панель создания метки
    /// </summary>
    [RelayCommand]
    public void OpenPanel2()
    {
        IsPanel2Open = true;
        PanelWidth2 = 160;
    }

    /// <summary>
    /// Закрывает панель создания метки
    /// </summary>
    [RelayCommand]
    public void ClosePanel2()
    {
        PanelWidth2 = 15;
        IsPanel2Open = false;
    }

    /// <summary>
    /// Открывает список иконок для выбора
    /// </summary>
    [RelayCommand]
    public async Task OpenList()
    {
        // Если не в режиме редактирования, закрываем панель
        if (!IsEditing)
        {
            ClosePanel1();
        }
        else
        {
            if (!IsPanel1Open)
                return;
            PanelWidth1 = 30;
            DescOpacity = 0;
            await Task.Delay(175);
            IsPanel1Open = false;
        }

        ClosePanel2();
        OverlayOpacity = 0.67; // Затемняем фон
        BorderListHeight = App.MainWindow!.Bounds.Height * 0.7; // Устанавливаем размеры списка
        await Task.Delay(200);
        BorderListWidth = App.MainWindow!.Bounds.Width * 0.7;
        await Task.Delay(200);
        IsListOpen = true;
        IconsOpacity = 1;

        IsAddingMarker = false;
        StatusMessage = string.Empty;
    }

    /// <summary>
    /// Закрывает список иконок
    /// </summary>
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
            OpenPanel1(); // Возвращаем панель редактирования
    }

    /// <summary>
    /// Добавляет или редактирует иконку в зависимости от режима
    /// </summary>
    [RelayCommand]
    private void AddOrEditIcon()
    {
        if (IsEditing)
        {
            EditIcon(); // Редактируем существующую
        }
        else
        {
            AddIcon(); // Добавляем новую
        }
    }

    /// <summary>
    /// Удаляет выбранную метку
    /// </summary>
    [RelayCommand]
    private void DeleteMarker()
    {
        if (SelectedMarker == null)
            return;

        _markerService.Delete(SelectedMarker.Id); // Удаляем из базы

        // Удаляем с карты
        var feature = _markers.FirstOrDefault(f =>
            f["Marker"] is MarkerDto dto &&
            dto.Id == SelectedMarker.Id);

        if (feature != null)
            _markers.Remove(feature);

        _markerLayer.Features = _markers;
        _markerLayer.DataHasChanged();
        Map?.Refresh();

        SelectedMarker = null;

        ClosePanel1(); // Закрываем панель
    }

    /// <summary>
    /// Переходит в режим редактирования метки
    /// </summary>
    [RelayCommand]
    private async Task EditMarker()
    {
        IsEditing = true;
        OldMarkerScale = SelectedMarker.Scale;

        MarkerScale = SelectedMarker.Scale;
        EditPartsOpacity = 1; // Показываем элементы редактирования

        SliderHeight = 70; // Показываем слайдер масштаба

        ButtonHeight1 = 0; // Скрываем основные кнопки
        await Task.Delay(175);
        ButtonHeight2 = 43; // Показываем кнопки редактирования
    }

    /// <summary>
    /// Применяет все изменения метки
    /// </summary>
    [RelayCommand]
    private async Task ApplyChanges()
    {
        SelectedMarker.Description = Description;
        SelectedMarker.Scale = MarkerScale;
        OldMarkerScale = MarkerScale;
        _markerService.UpdateApp(SelectedMarker);
        IsEditing = false;

        // Скрываем элементы редактирования
        EditPartsOpacity = 0;
        SliderHeight = 0;

        ButtonHeight2 = 0;
        await Task.Delay(175);
        ButtonHeight1 = 43; // Восстанавливаем основные кнопки
    }

    /// <summary>
    /// Отменяет изменения метки
    /// </summary>
    [RelayCommand]
    private async Task CancelChanges()
    {
        ClosePanel1();
        if (OldMarkerScale != 0)
        {
            // Восстанавливаем масштаб
            MarkerScale = OldMarkerScale;
            UpdateMarkerScale(OldMarkerScale);
        }


        IsEditing = false;

        // Скрываем элементы редактирования
        EditPartsOpacity = 0;
        SliderHeight = 0;

        ButtonHeight2 = 0;
        await Task.Delay(175);
        ButtonHeight1 = 43;
    }

    #endregion
}