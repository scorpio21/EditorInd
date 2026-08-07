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

    [Theory]
    [InlineData("Personajes.ind")]
    [InlineData("Cabezas.ind")]
    [InlineData("Cascos.ind")]
    [InlineData("Fxs.ind")]
    public void Aom2018_RoundTripByteExacto(string file)
    {
        var original = File.ReadAllBytes(A(file));
        var data = IndFileReader.Read(A(file));
        Assert.Equal(original, IndFileWriter.ToBytes(data));
    }

    [Fact]
    public void Personajes_Aom2018_ImportInt16ArrayFueraDeRango_LanzaErrorLinea()
    {
        var data = IndFileReader.Read(A("Personajes.ind"));
        var txt = "# Variante: Aom2018-Int16\n[1]\nBody.1 = 70000\n";
        var ex = Assert.Throws<FormatException>(() => TxtImporter.Import(txt, data.Format, data.HeaderBytes));
        Assert.Contains("Línea", ex.Message);
        Assert.Contains("Body.1", ex.Message);
    }

    [Fact]
    public void Personajes_Aom2018_ImportInt16ArrayEnRango_Ok()
    {
        var data = IndFileReader.Read(A("Personajes.ind"));
        var txt = "# Variante: Aom2018-Int16\n[1]\nBody.1 = 32767\nBody.2 = -32768\nHeadOffsetX = 0\nHeadOffsetY = 0\n";
        var imported = TxtImporter.Import(txt, data.Format, data.HeaderBytes);
        Assert.Equal(32767, ((int[])imported.Records[0].Values["Body"])[0]);
        Assert.Equal(-32768, ((int[])imported.Records[0].Values["Body"])[1]);
    }

    [Fact]
    public void Aom2018_TxtRoundTripByteExacto()
    {
        var data = IndFileReader.Read(A("Personajes.ind"));
        var txt = TxtExporter.Export(data);
        var imported = TxtImporter.Import(txt, data.Format, data.HeaderBytes);
        Assert.Equal(IndFileWriter.ToBytes(data), IndFileWriter.ToBytes(imported));
    }

    [Fact]
    public void Personajes_Aom2018_ExportDesinddat()
    {
        var data = IndFileReader.Read(A("Personajes.ind"));
        var txt = TxtExporter.ExportDesinddat(data);
        Assert.Contains("[INIT]", txt);
        Assert.Contains("NumBodies=660", txt);
        Assert.Contains("[Body1]", txt);
        Assert.Contains("Walk1=4582", txt);
        Assert.Contains("Walk2=4584", txt);
        Assert.Contains("Walk3=4581", txt);
        Assert.Contains("Walk4=4583", txt);
        Assert.Contains("HeadOffsetX=0", txt);
        Assert.Contains("HeadOffsetY=-38", txt);
    }

    [Fact]
    public void Personajes_Aodrag9_ExportDesinddat()
    {
        var data = IndFileReader.Read(I("personajes.ind"));
        var txt = TxtExporter.ExportDesinddat(data);
        Assert.Contains("NumBodies=470", txt);
        Assert.Contains("[Body1]", txt);
        var rec = data.Records[0];
        var body = (int[])rec.Values["Body"];
        Assert.Contains($"Walk1={body[0]}", txt);
    }

    [Fact]
    public void Cabezas_Aodrag9_ExportDesinddat_Mapea3a4()
    {
        var data = IndFileReader.Read(I("cabezas.ind"));
        var txt = TxtExporter.ExportDesinddat(data);
        Assert.Contains("NumHeads=654", txt);
        Assert.Contains("[Head1]", txt);
        var rec = data.Records[0];
        Assert.Contains($"Head0={(short)rec.Values["Texture"]}", txt);
        Assert.Contains($"Head1={(short)rec.Values["startX"]}", txt);
        Assert.Contains($"Head2={(short)rec.Values["startY"]}", txt);
        Assert.Contains("Head3=0", txt);
    }

    [Fact]
    public void Cabezas_Aom2018_ExportDesinddat()
    {
        var data = IndFileReader.Read(A("Cabezas.ind"));
        var txt = TxtExporter.ExportDesinddat(data);
        Assert.Contains("NumHeads=831", txt);
        Assert.Contains("[Head1]", txt);
        var rec = data.Records[0];
        Assert.Contains($"Head0={(short)rec.Values["Head0"]}", txt);
    }

    [Fact]
    public void Fxs_Aodrag9_ExportDesinddat()
    {
        var data = IndFileReader.Read(I("fxs.ind"));
        var txt = TxtExporter.ExportDesinddat(data);
        Assert.Contains("NumFxs=59", txt);
        Assert.Contains("[FX1]", txt);
        Assert.Contains($"Animacion={Convert.ToInt32(data.Records[0].Values["Animacion"])}", txt);
        Assert.Contains($"OffsetX={(short)data.Records[0].Values["offsetX"]}", txt);
    }
}
