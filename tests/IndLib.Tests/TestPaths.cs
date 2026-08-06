namespace IndLib.Tests;

public static class TestPaths
{
    public static string InitDir =>
        Environment.GetEnvironmentVariable("AO_INIT_DIR") ?? @"K:\Descargas\aaoo\init";
}
