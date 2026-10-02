using DrawingFinder.Models;

namespace DrawingFinder.Services;

public static class IndexService
{
    public static List<DrawingRecord> BuildIndex(string rootFolder)
    {
        var results = new List<DrawingRecord>();

        foreach (string file in Directory.GetFiles(
            rootFolder,
            "*.pdf",
            SearchOption.AllDirectories))
        {
            results.Add(new DrawingRecord
            {
                FileName = Path.GetFileName(file),
                FullPath = file,
                SearchName = Normalize(Path.GetFileNameWithoutExtension(file)),
                ModifiedDate = File.GetLastWriteTime(file)
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