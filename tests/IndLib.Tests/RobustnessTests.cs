using IndLib;
using Xunit;

namespace IndLib.Tests;

public class RobustnessTests
{
    // C1: ResolveBoolean preserva el short crudo si la celda no cambió y
    // normaliza solo cuando el usuario conmutó la casilla.
    [Fact]
    public void ResolveBoolean_PreservaShortCrudoSiNoCambio()
    {
        Assert.Equal((short)0x00FF, IndValueLogic.ResolveBoolean(true, (short)0x00FF));
    }

    [Fact]
    public void ResolveBoolean_ConmutadaAFalseNormalizaCero()
    {
        Assert.Equal((short)0, IndValueLogic.ResolveBoolean(false, (short)0x00FF));
    }

    [Fact]
    public void ResolveBoolean_ConmutadaATrueNormalizaUnoNegativo()
    {
        Assert.Equal((short)-1, IndValueLogic.ResolveBoolean(true, 0));
    }

    // I1: WriteGrhEntry rechaza estado inconsistente (NumFrames != Frames.Length).
    [Fact]
    public void WriteGrhEntry_NumFramesDesajustado_Lanza()
    {
        var data = new IndFileData
        {
            Format = IndFormatCatalog.Grafics,
            GrhEntries =
            {
                new GrhEntry { Grh = 5, HasData = true, NumFrames = 3, Frames = new[] { 1, 2 } },
            },
        };
        Assert.Throws<InvalidOperationException>(() => IndFileWriter.ToBytes(data));
    }

    // I2: WriteGrhEntry rechaza wraparound silencioso a Int16.
    [Fact]
    public void WriteGrhEntry_AnchoFueraDeRangoInt16_Lanza()
    {
        var data = new IndFileData
        {
            Format = IndFormatCatalog.Grafics,
            GrhEntries =
            {
                new GrhEntry { Grh = 5, HasData = true, NumFrames = 1, FileNum = 0, SX = 0, SY = 0, PixelWidth = 40000, PixelHeight = 10 },
            },
        };
        Assert.Throws<InvalidOperationException>(() => IndFileWriter.ToBytes(data));
    }

    // I3: TxtImporter reporta el número de línea en errores de TXT inválido.
    [Theory]
    [InlineData("# t\n[1]\nBody.0 = 1")]
    [InlineData("# t\n[1]\nBody.9 = 1")]
    [InlineData("# t\n[1]\nBody.x = 1")]
    public void TxtImporter_FixedRecords_Inválido_LanzaConLinea(string txt)
    {
        var ex = Assert.Throws<FormatException>(() =>
            TxtImporter.Import(txt, IndFormatCatalog.Ataques, Array.Empty<byte>()));
        Assert.StartsWith("Línea ", ex.Message);
    }

    [Fact]
    public void TxtImporter_Grh_NumFramesInválido_LanzaConLinea()
    {
        var txt = "# t\n[1]\nGrh = 1\nNumFrames = abc";
        var ex = Assert.Throws<FormatException>(() =>
            TxtImporter.Import(txt, IndFormatCatalog.Grafics, Array.Empty<byte>()));
        Assert.StartsWith("Línea ", ex.Message);
    }
}
