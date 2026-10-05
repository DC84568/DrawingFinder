namespace DrawingFinder.Models;

public class UserSettings
{
    public string Notes { get; set; } =
        "*** Close Drawing Finder before editing this file. ***";

    public string LastFolder { get; set; } = "";

    public List<FavoriteFolder> FavoriteFolders { get; set; }
        = new();
}