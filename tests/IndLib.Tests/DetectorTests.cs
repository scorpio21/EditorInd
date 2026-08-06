using IndLib;
using Xunit;

namespace IndLib.Tests;

public class DetectorTests
{
    [Theory]
    [InlineData("ataques.ind", "tIndiceAtaque")]
    [InlineData("personajes.ind", "tIndiceCuerpo")]
    [InlineData("fxs.ind", "tIndiceFx")]
    [InlineData("cabezas.ind", "tHead")]
    [InlineData("cascos.ind", "tHead")]
    [InlineData("graficos.ind", "GrhData")]
    [InlineData("texdefault1.dat", "texdefault")]
    [InlineData("minimap.dat", "minimap")]
    public void Detect_Conocidos(string file, string expectedName)
    {
        var f = IndFormatDetector.Detect(file);
        Assert.NotNull(f);
        Assert.Equal(expectedName, f!.Name);
    }

    [Fact]
    public void Detect_Desconocido_DevuelveNull()
    {
        Assert.Null(IndFormatDetector.Detect("basura.dat"));
    }

    [Fact]
    public void Detect_CaseInsensitive()
    {
        Assert.Equal(IndFormatCatalog.Ataques, IndFormatDetector.Detect("ATAQUES.IND"));
    }

    [Fact]
    public void Catalog_TamanosDeRegistro()
    {
        Assert.Equal(20, IndFormatCatalog.Ataques.RecordSize);
        Assert.Equal(20, IndFormatCatalog.Personajes.RecordSize);
        Assert.Equal(10, IndFormatCatalog.Fxs.RecordSize);
        Assert.Equal(6, IndFormatCatalog.Cabezas.RecordSize);
        Assert.Equal(6, IndFormatCatalog.Cascos.RecordSize);
        Assert.Equal(263, IndFormatCatalog.Ataques.HeaderSize);
        Assert.Equal(273, IndFormatCatalog.TexDefault1.RecordSize);
    }
}
