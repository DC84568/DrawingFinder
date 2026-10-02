using DrawingFinder.Models;

namespace DrawingFinder.Services;

public static class IndexService
{
    public static List<DrawingRecord> BuildIndex(string rootFolder)
    {
        var results = new List<DrawingRecord>();

        foreach (string folder in Directory.GetDirectories(
            rootFolder,
            "*",
            SearchOption.TopDirectoryOnly))
        {
            results.Add(new DrawingRecord
            {
                FileName = "📁  " + Path.GetFileName(folder),
                FullPath = folder,
                SearchName = Normalize(Path.GetFileName(folder)),
                ModifiedDate = Directory.GetLastWriteTime(folder),
                IsFolder = true
            });
        }

        foreach (string file in Directory.GetFiles(
            rootFolder,
            "*",
            SearchOption.TopDirectoryOnly))
        {
            results.Add(new DrawingRecord
            {
                FileName = Path.GetFileName(file),
                FullPath = file,
                SearchName = Normalize(Path.GetFileNameWithoutExtension(file)),
                ModifiedDate = File.GetLastWriteTime(file),
                IsFolder = false
            });
        }

        return results;
    }

    public static string Normalize(string text)
    {
        return text
            .Replace("-", "")
            .Replace("_", "")
            .Replace(" ", "")
            .ToUpper();
    }
}