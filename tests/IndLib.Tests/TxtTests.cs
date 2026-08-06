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

    [Fact]
    public void Grafics_ImportCompacto_Estatico()
    {
        const string txt = "'comentario\r\n[Graphics]\r\n\r\nGrh1=1-1-64-0-32-32-\r\n";
        var data = TxtImporter.Import(txt, IndFormatCatalog.Grafics, new byte[8]);
        Assert.Single(data.GrhEntries);
        var e = data.GrhEntries[0];
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
    public void Grafics_ImportCompacto_Animacion()
    {
        const string txt = "[Graphics]\r\n\r\nGrh23=6-1-2-3-4-5-6-1-\r\n";
        var data = TxtImporter.Import(txt, IndFormatCatalog.Grafics, new byte[8]);
        var e = Assert.Single(data.GrhEntries);
        Assert.Equal(23, e.Grh);
        Assert.Equal(6, e.NumFrames);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, e.Frames);
        Assert.Equal(1f, e.Speed);
    }

    [Fact]
    public void Grafics_Import_RechazaFormatoViejo()
    {
        const string txt = "[1]\r\nGrh = 1\r\nNumFrames = 1\r\n";
        var ex = Assert.Throws<FormatException>(() =>
            TxtImporter.Import(txt, IndFormatCatalog.Grafics, new byte[8]));
        Assert.Contains("Línea 2", ex.Message);
    }
}
