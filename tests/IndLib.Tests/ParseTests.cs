using IndLib;
using Xunit;

namespace IndLib.Tests;

public class ParseTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Fact]
    public void Ataques_CuentaRegistros()
    {
        Assert.True(File.Exists(P("ataques.ind")), $"Falta {P("ataques.ind")}. Ajusta AO_INIT_DIR.");
        var data = IndFileReader.Read(P("ataques.ind"));
        Assert.Equal(62, data.Count);
        Assert.Equal(62, data.Records.Count);
    }

    [Fact] public void Personajes_CuentaRegistros() => Assert.Equal(470, IndFileReader.Read(P("personajes.ind")).Count);
    [Fact] public void Fxs_CuentaRegistros() => Assert.Equal(59, IndFileReader.Read(P("fxs.ind")).Count);
    [Fact] public void Cabezas_CuentaRegistros() => Assert.Equal(654, IndFileReader.Read(P("cabezas.ind")).Count);
    [Fact] public void Cascos_CuentaRegistros() => Assert.Equal(44, IndFileReader.Read(P("cascos.ind")).Count);

    [Fact]
    public void Ataques_PrimerRegistro_Valores()
    {
        var rec = IndFileReader.Read(P("ataques.ind")).Records[0];
        var body = (int[])rec.Values["Body"];
        Assert.Equal(20466, body[0]);
        Assert.Equal(20467, body[1]);
        Assert.Equal(20469, body[2]);
        Assert.Equal(20468, body[3]);
        Assert.Equal(0, (short)rec.Values["HeadOffsetX"]);
        Assert.Equal(0, (short)rec.Values["HeadOffsetY"]);
    }

    [Fact]
    public void Cabezas_PrimerRegistro_Valores()
    {
        var rec = IndFileReader.Read(P("cabezas.ind")).Records[0];
        Assert.Equal(202, (short)rec.Values["Texture"]);
    }

    [Fact]
    public void Lectura_ArchivoDesconocido_Lanza()
    {
        Assert.Throws<InvalidDataException>(() => IndFileReader.Read(P("armas.dat")));
    }
}
