using System.Text;

public static class IrDumper
{
    public static string Dump(IrModule m)
    {
        StringBuilder sb = new StringBuilder();
        foreach (IrConstStr s in m.Strings)
            sb.AppendLine($"const_str {s.Label} = \"{s.Value}\"");
        sb.AppendLine();

        foreach (IrFunction f in m.Functions)
        {
            string paramList = "";
            foreach (IrLocal param in f.Parameters)
            {
                paramList += $"{param.Name} : {param.Type.Dump()}, ";
            }
            paramList = paramList.TrimEnd(' ', ',');
            if (f.isExternal)
            {
                sb.Append("external ");
            }
            sb.AppendLine($"func @{f.Name}({paramList}) : {f.ReturnType.Dump()}");
            foreach (IrLocal l in f.Locals)
                sb.AppendLine($"    local {l.Name} : {l.Type.Dump()}");
            foreach (IrBlock b in f.Blocks)
            {
                sb.AppendLine($"{b.Label}:");
                foreach (IrInstr i in b.Instructions)
                    sb.AppendLine($"    {i.Dump()}");
                sb.AppendLine();
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}