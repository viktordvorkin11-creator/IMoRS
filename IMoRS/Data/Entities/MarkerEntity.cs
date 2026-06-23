namespace IMoRS.Data.Entities;

public class MarkerEntity
{
    public int Id { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public string? Description { get; set; }
    
    public string? ImagePath { get; set; }
    
    public string? IconPath { get; set; }
}