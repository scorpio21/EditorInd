using IndLib;
using Xunit;

namespace IndLib.Tests;

public class RoundTripTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    private static void AssertRoundTrip(string file, string? grafics = null)
    {
        var original = File.ReadAllBytes(P(file));
        var data = IndFileReader.Read(P(file), grafics == null ? null : P(grafics));
        var bytes = IndFileWriter.ToBytes(data);
        Assert.Equal(original, bytes);
    }

    [Fact] public void Ataques_RoundTrip() => AssertRoundTrip("ataques.ind");
    [Fact] public void Personajes_RoundTrip() => AssertRoundTrip("personajes.ind");
    [Fact] public void Fxs_RoundTrip() => AssertRoundTrip("fxs.ind");
    [Fact] public void Cabezas_RoundTrip() => AssertRoundTrip("cabezas.ind");
    [Fact] public void Cascos_RoundTrip() => AssertRoundTrip("cascos.ind");
    [Fact] public void Grafics_RoundTrip() => AssertRoundTrip("graficos.ind");
    [Fact] public void TexDefault1_RoundTrip() => AssertRoundTrip("texdefault1.dat");
    [Fact] public void TexDefault2_RoundTrip() => AssertRoundTrip("texdefault2.dat");
    [Fact] public void TexDefault3_RoundTrip() => AssertRoundTrip("texdefault3.dat");
    [Fact] public void Minimap_RoundTrip() => AssertRoundTrip("minimap.dat", "graficos.ind");
}
