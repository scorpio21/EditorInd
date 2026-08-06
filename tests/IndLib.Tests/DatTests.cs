using IndLib;
using Xunit;

namespace IndLib.Tests;

public class DatTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Theory]
    [InlineData("texdefault1.dat", 256, 256, 17, 17, 32)]
    [InlineData("texdefault2.dat", 2048, 1024, 70, 70, 32)]
    public void TexDefault_Valores(string file, int bw, int bh, int cw, int ch, int bco)
    {
        var data = IndFileReader.Read(P(file));
        Assert.Equal(1, data.Count);
        var rec = data.Records[0];
        Assert.Equal(bw, (int)rec.Values["BitmapWidth"]);
        Assert.Equal(bh, (int)rec.Values["BitmapHeight"]);
        Assert.Equal(cw, (int)rec.Values["CellWidth"]);
        Assert.Equal(ch, (int)rec.Values["CellHeight"]);
        Assert.Equal(bco, (byte)rec.Values["BaseCharOffset"]);
        Assert.Equal(256, ((byte[])rec.Values["CharWidth"]).Length);
    }

    [Fact]
    public void Minimap_CuentaColores()
    {
        var data = IndFileReader.Read(P("minimap.dat"), P("graficos.ind"));
        Assert.Equal(24546, data.Count);
        Assert.Equal(24546, data.MinimapEntries.Count);
        Assert.NotEqual("", data.Warning); // 24546 colores != 24548 grhs activos
    }

    [Fact]
    public void Minimap_PrimerColor()
    {
        var data = IndFileReader.Read(P("minimap.dat"), P("graficos.ind"));
        Assert.Equal(1, data.MinimapEntries[0].Grh);
        Assert.Equal(0x00000000u, data.MinimapEntries[0].Color);
        Assert.Equal(0x000000FFu, data.MinimapEntries[1].Color);
    }
}
