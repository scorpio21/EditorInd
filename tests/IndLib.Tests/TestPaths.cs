namespace IndLib.Tests;

public static class TestPaths
{
    public static string InitDir =>
        Environment.GetEnvironmentVariable("AO_INIT_DIR") ?? @"K:\Descargas\aaoo\init";

    public static string Aom2018Dir =>
        Environment.GetEnvironmentVariable("AO_AOM2018_DIR") ?? @"K:\Argentum\Aomania\Aom2018\Caom\AomUtilidad2012\configurador\DESINDDAT";
}
