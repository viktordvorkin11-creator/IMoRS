using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using IMoRS.Data;
using IMoRS.Data.Entities;
using IMoRS.DTOs;

namespace IMoRS.Services;

public class MarkerService
{
    public void Add(double x, double y)
    {
        using var context = new AppDbContext();
    
        var marker = new MarkerEntity
        {
            X = x,
            Y = y,
            ImagePath = ImageService.GetImagePath("sign_6.png") // Добавьте путь по умолчанию
        };
    
        context.Markers.Add(marker);
        context.SaveChanges();
    }

    public List<MarkerDto> GetAll()
    {
        using var db = new AppDbContext();

        Console.WriteLine($"Найдено меток в БД: {db.Markers.Count()}");
        return db.Markers
            .Select(m => new MarkerDto
            {
                Id = m.Id,
                X = m.X,
                Y = m.Y,
                Description = m.Description,
                IconPath = m.IconPath
            })
            .ToList();
        
    }
    
    public void UpdateApp(MarkerDto dto)
    {
        using var db = new AppDbContext();

        var entity = db.Markers.Find(dto.Id);

        if (entity == null) return;

        entity.X = dto.X;
        entity.Y = dto.Y;
        entity.Description = dto.Description;
        entity.ImagePath = dto.ImagePath;
        entity.IconPath = dto.IconPath;

        db.SaveChanges();
    } 
}