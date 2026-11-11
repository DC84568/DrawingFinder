namespace DrawingFinder.Models;

public class UserSettings
{
    public string Notes { get; set; } =
        "*** Close Drawing Finder before editing this file. ***";

    public string LastFolder { get; set; } = "";

    public List<FavoriteFolder> FavoriteFolders { get; set; }
        = new();

    public string ArenaCsvPath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive - Novanta",
            "LQ Mechanical Development - Documents",
            "Scripts",
            "Arena Export",
            "Arena Drawing Export.csv");
}