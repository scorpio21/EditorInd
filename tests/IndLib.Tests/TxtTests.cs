using IndLib;
using Xunit;

namespace IndLib.Tests;

public class TxtTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Theory]
    [InlineData("ataques.ind", null)]
    [InlineData("graficos.ind", null)]
    [InlineData("texdefault1.dat", null)]
    [InlineData("minimap.dat", "graficos.ind")]
    public void Export_Import_RoundTrip(string file, string? grafics)
    {
        var data = IndFileReader.Read(P(file), grafics == null ? null : P(grafics));
        var txt = TxtExporter.Export(data);
        var imported = TxtImporter.Import(txt, data.Format, data.HeaderBytes, grafics == null ? null : P(grafics));
        Assert.Equal(IndFileWriter.ToBytes(data), IndFileWriter.ToBytes(imported));
    }

    [Fact]
    public void Ataques_ExportContieneBloque()
    {
        var txt = TxtExporter.Export(IndFileReader.Read(P("ataques.ind")));
        Assert.Contains("# Formato: tIndiceAtaque", txt);
        Assert.Contains("Body.1 = 20466", txt);
    }

    [Fact]
    public void Grafics_ExportFormatoCompacto()
    {
        var data = IndFileReader.Read(P("graficos.ind"));
        var txt = TxtExporter.Export(data);
        Assert.Contains("[Graphics]", txt);
        Assert.Contains("\r\nGrh1=1-1-64-0-32-32-\r\n", txt);
        Assert.DoesNotContain("# Formato: GrhData", txt);
        Assert.DoesNotContain("NumFrames =", txt);
    }
}
