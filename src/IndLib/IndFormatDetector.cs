namespace IndLib;

public static class IndFormatDetector
{
    public static IndFormat? Detect(string fileName)
    {
        var name = Path.GetFileName(fileName).ToLowerInvariant();
        foreach (var f in IndFormatCatalog.All)
        {
            foreach (var p in f.FilePatterns)
            {
                if (name == p.ToLowerInvariant()) return f;
            }
        }
        return null;
    }
}
