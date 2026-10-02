namespace DrawingFinder.Models;

public class DrawingRecord
{
    public string FileName { get; set; } = "";
    public string SearchName { get; set; } = "";
    public string FullPath { get; set; } = "";
    public DateTime ModifiedDate { get; set; }
    public bool IsFolder { get; set; }
}