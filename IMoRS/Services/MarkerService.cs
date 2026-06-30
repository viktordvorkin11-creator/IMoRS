using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using IMoRS.Data;
using IMoRS.Data.Entities;
using IMoRS.DTOs;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;

namespace IMoRS.Services;

/// <summary>
/// Сервис для управления метками на карте.
/// Обеспечивает CRUD-операции (создание, чтение, обновление, удаление) с метками,
/// используя Entity Framework Core для работы с базой данных.
/// </summary>
public class MarkerService
{
    /// <summary>
    /// Добавляет новый маркер в базу данных.
    /// </summary>
    /// <param name="x">Координата X (долгота) в системе координат карты</param>
    /// <param name="y">Координата Y (широта) в системе координат карты</param>
    /// <param name="iconPath">Путь к файлу иконки маркера</param>
    /// <param name="scale">Масштаб отображения маркера (1.0 - оригинальный размер)</param>
    /// <returns>DTO созданного маркера с присвоенным ID из базы данных</returns>
    public MarkerDto Add(double x, double y, string iconPath, double scale)
    {
        using var db = new AppDbContext();

        var marker = new MarkerEntity
        {
            X = x,
            Y = y,
            IconPath = iconPath,
            ImagePath = iconPath,
            Scale = scale
        };

        db.Markers.Add(marker);
        db.SaveChanges();

        return new MarkerDto
        {
            Id = marker.Id,
            X = marker.X,
            Y = marker.Y,
            IconPath = marker.IconPath,
            Scale = marker.Scale
        };
    }

    /// <summary>
    /// Получает список всех маркеров из базы данных.
    /// </summary>
    /// <returns>Список DTO всех маркеров, отсортированный по умолчанию</returns>
    public List<MarkerDto> GetAll()
    {
        using var db = new AppDbContext();

        return db.Markers
            .Select(m => new MarkerDto
            {
                Id = m.Id,
                X = m.X,
                Y = m.Y,
                Description = m.Description,
                IconPath = m.IconPath,
                Scale = m.Scale 
            })
            .ToList(); 
    }

    /// <summary>
    /// Обновляет существующий маркер в базе данных.
    /// </summary>
    /// <param name="dto">DTO с обновленными данными маркера</param>
    /// <remarks>
    /// Если маркер с указанным ID не найден, метод завершается без изменений.
    /// Если IconPath равен null, заменяется на пустую строку.
    /// </remarks>
    public void UpdateApp(MarkerDto dto)
    {
        using var db = new AppDbContext();

        var entity = db.Markers.Find(dto.Id);

        if (entity == null) return;

        entity.X = dto.X;
        entity.Y = dto.Y;
        entity.Description = dto.Description;
        entity.IconPath = dto.IconPath ?? string.Empty;
        entity.Scale = dto.Scale;

        db.SaveChanges();
    }

    /// <summary>
    /// Удаляет маркер из базы данных по его идентификатору.
    /// </summary>
    /// <param name="markerId">ID маркера, который нужно удалить</param>
    /// <remarks>
    /// Выводит информацию об удалении в консоль для отладки.
    /// Если маркер не найден, метод завершается без ошибок.
    /// </remarks>
    public void Delete(int markerId)
    {
        using var db = new AppDbContext();

        var marker = db.Markers.Find(markerId);

        if (marker == null)
            return;

        db.Markers.Remove(marker);
        db.SaveChanges();
    }
}