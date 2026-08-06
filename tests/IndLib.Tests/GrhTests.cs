using IndLib;
using Xunit;

namespace IndLib.Tests;

public class GrhTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Fact]
    public void Grafics_Conteos()
    {
        var data = IndFileReader.Read(P("graficos.ind"));
        Assert.Equal(24548, data.Count);
        Assert.Equal(24548, data.GrhEntries.Count);
        Assert.Equal(25517, data.GrhCount);
        Assert.True(data.GrhEntries.Count(e => e.NumFrames > 1) > 2000, "debe haber animaciones");
    }

    [Fact]
    public void Grafics_PrimeraEntrada_EsEstatica()
    {
        var e = IndFileReader.Read(P("graficos.ind")).GrhEntries[0];
        Assert.Equal(1, e.Grh);
        Assert.True(e.HasData);
        Assert.Equal(1, e.NumFrames);
        Assert.Equal(1, e.FileNum);
        Assert.Equal(64, e.SX);
        Assert.Equal(0, e.SY);
        Assert.Equal(32, e.PixelWidth);
        Assert.Equal(32, e.PixelHeight);
    }

    [Fact]
    public void Grafics_IndicesActivos_OrdenadosUnicos()
    {
        var active = IndFileReader.GetActiveGrhIndices(File.ReadAllBytes(P("graficos.ind")));
        Assert.Equal(24548, active.Count);
        Assert.Equal(1, active[0]);
        for (int i = 1; i < active.Count; i++) Assert.True(active[i] > active[i - 1]);
    }
}
