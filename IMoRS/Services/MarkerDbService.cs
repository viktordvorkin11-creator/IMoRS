using System.Collections.Generic;
using IMoRS.Models;
using Microsoft.Data.Sqlite;

namespace IMoRS.Services;

public class MarkerDbService
{
    private const string ConnectionString = "Data Source=Markers.db";

    public MarkerDbService()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText = @"
                       CREATE TABLE IF NOT EXISTS Markers(
                           Id INTEGER PRIMARY KEY AUTOINCREMENT,
                           X REAL NOT NULL,
                           Y REAL NOT NULL,
                           Description TEXT
                        );";

        command.ExecuteNonQuery();
    }

    public void AddMarker(double x, double y)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText = @"
                        INSERT INTO Markers (X, Y)
                        VALUES($x, $y)";

        command.Parameters.AddWithValue("$x", x);
        command.Parameters.AddWithValue("$y", y);

        command.ExecuteNonQuery();
    }

    public List<MarkerModel> LoadMarkers()
    {
        List<MarkerModel> markers = [];

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Markers";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            markers.Add(new MarkerModel()
            {
                Id = reader.GetInt32(0),
                X = reader.GetDouble(1),
                Y = reader.GetDouble(2),
                // Description = reader.GetString(3)
            });
        }

        return markers;
    }
}