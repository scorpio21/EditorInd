using IndLib;
using Xunit;

namespace IndLib.Tests;

public class DualFormatTests
{
    private static string A(string name) => Path.Combine(TestPaths.Aom2018Dir, name);
    private static string I(string name) => Path.Combine(TestPaths.InitDir, name);

    [Fact]
    public void Personajes_Aom2018_DetectaVariantInt16()
    {
        var data = IndFileReader.Read(A("Personajes.ind"));
        Assert.NotNull(data.Variant);
        Assert.Equal("Aom2018-Int16", data.Variant!.Name);
        Assert.Equal(660, data.Count);
        var rec = data.Records[0];
        Assert.Equal(new[] { 4582, 4584, 4581, 4583 }, (int[])rec.Values["Body"]);
        Assert.Equal((short)0, (short)rec.Values["HeadOffsetX"]);
        Assert.Equal((short)-38, (short)rec.Values["HeadOffsetY"]);
    }

    [Fact]
    public void Personajes_Aodrag9_UsaLayoutPorDefecto()
    {
        var data = IndFileReader.Read(I("personajes.ind"));
        Assert.Null(data.Variant);
        Assert.Equal(470, data.Count);
    }

    [Fact]
    public void Cabezas_Aom2018_DetectaVariant4Campos()
    {
        var data = IndFileReader.Read(A("Cabezas.ind"));
        Assert.NotNull(data.Variant);
        Assert.Equal("Aom2018-4Head", data.Variant!.Name);
        Assert.Equal(831, data.Count);
        Assert.Equal(4, data.Variant.Fields.Length);
    }
}
