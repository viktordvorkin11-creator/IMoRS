namespace IMoRS.Models;

public class MarkerModel
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string? Description { get; set; } = string.Empty;
}