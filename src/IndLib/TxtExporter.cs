using System.Globalization;
using System.Text;

namespace IndLib;

public static class TxtExporter
{
    public static string Export(IndFileData data)
    {
        var sb = new StringBuilder();
        if (data.Format.Kind != IndFormatKind.GrhData)
        {
            sb.AppendLine("# IndEditor v1.0");
            sb.AppendLine($"# Archivo: {data.FileName}");
            sb.AppendLine($"# Formato: {data.Format.Name}");
        }
        switch (data.Format.Kind)
        {
            case IndFormatKind.FixedRecords:
                sb.AppendLine($"# Registros: {data.Records.Count}");
                foreach (var rec in data.Records)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{rec.Index}]");
                    WriteRecord(sb, data.Format, rec);
                }
                break;
            case IndFormatKind.GrhData:
                sb.AppendLine("'Graficos.ind desindexado con IndEditor");
                sb.AppendLine($"'{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine("[Graphics]");
                foreach (var e in data.GrhEntries)
                {
                    if (!e.HasData) continue;
                    sb.AppendLine();
                    WriteGrhCompact(sb, e);
                }
                break;
            case IndFormatKind.TexDefault:
                sb.AppendLine();
                sb.AppendLine("[1]");
                WriteRecord(sb, data.Format, data.Records[0]);
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

    private static void WriteRecord(StringBuilder sb, IndFormat format, IndRecord rec)
    {
        foreach (var f in format.Fields)
        {
            if (f.Type == IndFieldType.Int32Array)
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

    private static void WriteGrhCompact(StringBuilder sb, GrhEntry e)
    {
        sb.Append("Grh").Append(e.Grh.ToString(CultureInfo.InvariantCulture)).Append('=');
        if (e.NumFrames > 1)
        {
            sb.Append(e.NumFrames.ToString(CultureInfo.InvariantCulture)).Append('-');
            foreach (var f in e.Frames) sb.Append(f.ToString(CultureInfo.InvariantCulture)).Append('-');
            sb.Append(e.Speed.ToString("R", CultureInfo.InvariantCulture)).Append('-');
        }
        else
        {
            sb.Append("1-").Append(e.FileNum.ToString(CultureInfo.InvariantCulture)).Append('-')
              .Append(e.SX.ToString(CultureInfo.InvariantCulture)).Append('-')
              .Append(e.SY.ToString(CultureInfo.InvariantCulture)).Append('-')
              .Append(e.PixelWidth.ToString(CultureInfo.InvariantCulture)).Append('-')
              .Append(e.PixelHeight.ToString(CultureInfo.InvariantCulture)).Append('-');
        }
        sb.AppendLine();
    }
}
