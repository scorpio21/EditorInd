using System.Globalization;
using System.Text;

namespace IndLib;

public static class TxtExporter
{
    public static string Export(IndFileData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# IndEditor v1.0");
        sb.AppendLine($"# Archivo: {data.FileName}");
        sb.AppendLine($"# Formato: {data.Format.Name}");
        switch (data.Format.Kind)
        {
            case IndFormatKind.FixedRecords:
                sb.AppendLine($"# Registros: {data.Records.Count}");
                sb.AppendLine($"# Variante: {data.Variant?.Name ?? "default"}");
                var fields = data.Variant?.Fields ?? data.Format.Fields;
                foreach (var rec in data.Records)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{rec.Index}]");
                    WriteRecord(sb, fields, rec);
                }
                break;
            case IndFormatKind.GrhData:
                sb.AppendLine($"# Entradas: {data.GrhEntries.Count}");
                for (int i = 0; i < data.GrhEntries.Count; i++)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{i + 1}]");
                    WriteGrhEntry(sb, data.GrhEntries[i]);
                }
                break;
            case IndFormatKind.TexDefault:
                sb.AppendLine();
                sb.AppendLine("[1]");
                WriteRecord(sb, data.Format.Fields, data.Records[0]);
                break;
            case IndFormatKind.Minimap:
                sb.AppendLine($"# Grhs: {data.MinimapEntries.Count}");
                for (int i = 0; i < data.MinimapEntries.Count; i++)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{i + 1}]");
                    sb.AppendLine($"Grh = {data.MinimapEntries[i].Grh}");
                    sb.AppendLine($"Color = {data.MinimapEntries[i].Color:X8}");
                }
                break;
        }
        return sb.ToString();
    }

    private static void WriteRecord(StringBuilder sb, IndField[] fields, IndRecord rec)
    {
        foreach (var f in fields)
        {
            if (f.Type == IndFieldType.Int32Array || f.Type == IndFieldType.Int16Array)
            {
                var arr = (int[])rec.Values[f.Name];
                for (int j = 0; j < arr.Length; j++)
                    sb.AppendLine($"{f.Name}.{j + 1} = {arr[j]}");
            }
            else if (f.Type == IndFieldType.ByteArray)
            {
                sb.AppendLine($"{f.Name} = {string.Join(",", (byte[])rec.Values[f.Name])}");
            }
            else if (f.Type == IndFieldType.Single)
            {
                sb.AppendLine($"{f.Name} = {((float)rec.Values[f.Name]).ToString("R", CultureInfo.InvariantCulture)}");
            }
            else if (f.Type == IndFieldType.Boolean)
            {
                sb.AppendLine($"{f.Name} = {(((short)rec.Values[f.Name]) != 0 ? "True" : "False")}");
            }
            else
            {
                sb.AppendLine($"{f.Name} = {rec.Values[f.Name]}");
            }
        }
    }

    private static void WriteGrhEntry(StringBuilder sb, GrhEntry e)
    {
        sb.AppendLine($"Grh = {e.Grh}");
        if (!e.HasData) return;
        sb.AppendLine($"NumFrames = {e.NumFrames}");
        if (e.NumFrames > 1)
        {
            sb.AppendLine($"Frames = {string.Join(",", e.Frames)}");
            sb.AppendLine($"Velocidad = {e.Speed.ToString("R", CultureInfo.InvariantCulture)}");
        }
        else
        {
            sb.AppendLine($"FileNum = {e.FileNum}");
            sb.AppendLine($"SX = {e.SX}");
            sb.AppendLine($"SY = {e.SY}");
            sb.AppendLine($"Ancho = {e.PixelWidth}");
            sb.AppendLine($"Alto = {e.PixelHeight}");
        }
    }

    public static string ExportDesinddat(IndFileData data)
    {
        if (data.Format.Kind != IndFormatKind.FixedRecords)
            throw new InvalidOperationException("El formato DESINDDAT solo aplica a archivos de registros fijos.");
        var sb = new StringBuilder();
        var fecha = DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);
        sb.AppendLine($"' {data.FileName}, desindexado: {fecha}");
        sb.AppendLine();
        sb.AppendLine();
        var fields = data.Variant?.Fields ?? data.Format.Fields;
        switch (data.Format.DisplayName)
        {
            case "Personajes": WriteDesinddatBody(sb, data, "NumBodies", "Body", fields); break;
            case "Ataques": WriteDesinddatBody(sb, data, "NumAtaques", "Body", fields); break;
            case "FXs": WriteDesinddatFx(sb, data, fields); break;
            case "Cabezas": WriteDesinddatHead(sb, data, "NumHeads", "Head", zeroBased: true, fields); break;
            case "Cascos": WriteDesinddatHead(sb, data, "NumCascos", "Casco", zeroBased: false, fields); break;
            default:
                throw new InvalidOperationException($"No hay exportación DESINDDAT para '{data.Format.DisplayName}'.");
        }
        return sb.ToString();
    }

    private static void WriteDesinddatBody(StringBuilder sb, IndFileData data, string initKey, string section, IndField[] fields)
    {
        sb.AppendLine("[INIT]");
        sb.AppendLine($"{initKey}={data.Records.Count}");
        sb.AppendLine();
        foreach (var rec in data.Records)
        {
            sb.AppendLine($"[{section}{rec.Index}]");
            var body = (int[])rec.Values["Body"];
            for (int j = 0; j < body.Length; j++)
                sb.AppendLine($"Walk{j + 1}={body[j]}");
            sb.AppendLine($"HeadOffsetX={Convert.ToInt32(rec.Values["HeadOffsetX"])}");
            sb.AppendLine($"HeadOffsetY={Convert.ToInt32(rec.Values["HeadOffsetY"])}");
            sb.AppendLine();
        }
    }

    private static void WriteDesinddatFx(StringBuilder sb, IndFileData data, IndField[] fields)
    {
        sb.AppendLine("[INIT]");
        sb.AppendLine($"NumFxs={data.Records.Count}");
        sb.AppendLine();
        foreach (var rec in data.Records)
        {
            sb.AppendLine($"[FX{rec.Index}]");
            sb.AppendLine($"Animacion={Convert.ToInt32(rec.Values["Animacion"])}");
            sb.AppendLine($"OffsetX={Convert.ToInt32(rec.Values["offsetX"])}");
            sb.AppendLine($"OffsetY={Convert.ToInt32(rec.Values["offsetY"])}");
            sb.AppendLine();
        }
    }

    private static void WriteDesinddatHead(StringBuilder sb, IndFileData data, string initKey, string section, bool zeroBased, IndField[] fields)
    {
        sb.AppendLine("[INIT]");
        sb.AppendLine($"{initKey}={data.Records.Count}");
        sb.AppendLine();
        bool has4 = fields.Length >= 4;
        foreach (var rec in data.Records)
        {
            sb.AppendLine($"[{section}{rec.Index}]");
            int[] vals = has4
                ? new[] { Convert.ToInt32(rec.Values["Head0"]), Convert.ToInt32(rec.Values["Head1"]), Convert.ToInt32(rec.Values["Head2"]), Convert.ToInt32(rec.Values["Head3"]) }
                : new[] { Convert.ToInt32(rec.Values["Texture"]), Convert.ToInt32(rec.Values["startX"]), Convert.ToInt32(rec.Values["startY"]), 0 };
            for (int j = 0; j < 4; j++)
                sb.AppendLine($"Head{j + (zeroBased ? 0 : 1)}={vals[j]}");
            sb.AppendLine();
        }
    }
}
