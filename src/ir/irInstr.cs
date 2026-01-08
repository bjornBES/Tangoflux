
using System.Drawing;
using System.Text;
using CompilerTangoFlex.lexer;

//public enum IrType { Int, Float, Bool, String, Void, Ptr }

public class IrType
{
    public static IrType Void = new IrType(KeywordVal.VOID);
    public static IrType Boolean = new IrType(KeywordVal.BOOL);
    public static IrType Int = new IrType(KeywordVal.INT);

    public int SizeInBits { get; set; }
    public bool IsSigned { get; set; }
    public IrType? RefType { get; set; }

    public IrType(KeywordVal keyword)
    {
        Init(new() { Type = keyword });
    }

    public IrType(NodeType nt)
    {
        Init(nt);
    }

    private void Init(NodeType type)
    {
        switch (type.Type)
        {
            case KeywordVal.PTR:
                SizeInBits = 32;
                IsSigned = false;
                RefType = new IrType(type.NestedTypes);
                break;

            case KeywordVal.INT8:
                SizeInBits = 8;
                IsSigned = true;
                break;

            case KeywordVal.UINT8:
            case KeywordVal.BOOL:
                SizeInBits = 8;
                IsSigned = false;
                break;

            case KeywordVal.INT16:
                SizeInBits = 16;
                IsSigned = true;
                break;

            case KeywordVal.UINT16:
                SizeInBits = 16;
                IsSigned = false;
                break;

            case KeywordVal.INT32:
                SizeInBits = 32;
                IsSigned = true;
                break;

            case KeywordVal.UINT32:
                SizeInBits = 32;
                IsSigned = false;
                break;

            case KeywordVal.FLOAT:
                // TODO: floats
                break;

            case KeywordVal.INT64:
                SizeInBits = 64;
                IsSigned = true;
                break;

            case KeywordVal.UINT64:
                SizeInBits = 64;
                IsSigned = false;
                break;

            case KeywordVal.STRING:
                // TODO: LowerStringType
                // use fat strings at some point (not now)
                // def: fat strings is a string that has the length in it
                SizeInBits = 64;
                IsSigned = false;
                RefType = new IrType(KeywordVal.UINT8);
                break;

            case KeywordVal.VOID:
                SizeInBits = 0;
                IsSigned = false;
                break;
            default:
                throw new Exception($"Unhandled type {type.Type}");
        }
    }
    public string Dump()
    {
        return DumpInternal(this);
    }

    private static string DumpInternal(IrType type)
    {
        // pointer
        if (type.RefType != null)
        {
            return $"ptr({DumpInternal(type.RefType)})";
        }

        // base types by size/sign
        if (type.SizeInBits == 0)
            return "void";

        if (type.SizeInBits == 1)
            return "bool";

        if (type.SizeInBits == 8 && type.IsSigned)
            return "int8";

        if (type.SizeInBits == 8 && !type.IsSigned)
            return "uint8";

        if (type.SizeInBits == 16 && type.IsSigned)
            return "int16";

        if (type.SizeInBits == 16 && !type.IsSigned)
            return "uint16";

        if (type.SizeInBits == 32 && type.IsSigned)
            return "int32";

        if (type.SizeInBits == 32 && !type.IsSigned)
            return "uint32";

        if (type.SizeInBits == 64 && type.IsSigned)
            return "int64";

        if (type.SizeInBits == 64 && !type.IsSigned)
            return "uint64";

        // fat string (value type)
        /*
        Only when done
        if (type.SizeInBits == Target.PointerSizeBits + 64)
            return "string";
        */

        return $"<unknown:{type.SizeInBits}>";
    }
}

public abstract class IrOperand
{
    public abstract string Dump();
}

public class IrTemp : IrOperand
{
    public int Id { get; }
    public IrType Type { get; }
    public IrTemp(int id, IrType type) { Id = id; Type = type; }
    public override string Dump() => $"%t{Id}";
}

public class IrLocal : IrOperand
{
    public string Name;
    public IrType Type;
    public bool HasInit { get; set; }
    public IrLocal(string name, IrType type)
    {
        Name = name;
        Type = type;
        HasInit = false;
    }


    public override string Dump()
    {
        return $"@{Name}";
    }
}

public class IrConstInt : IrOperand
{
    public long Value { get; }
    public IrConstInt(long v) { Value = v; }
    public override string Dump() => $"constI64 {Value}";
}

public class IrConstFloat : IrOperand
{
    public double Value { get; }
    public IrConstFloat(double v) { Value = v; }
    public override string Dump() => $"constFloat {Value}";
}

public class IrConstStr : IrOperand
{
    public string Value { get; }
    public string Label { get; set; } // assigned by module
    public IrConstStr(string v) { Value = v; }
    public override string Dump() => $"constStr \"{Value.Replace("\"", "\\\"")}\"";
}

public class IrSymbol : IrOperand
{
    public string Name { get; }
    public IrSymbol(string name) { Name = name; }
    public override string Dump() => $"@{Name}";
}

public class IrInstr
{
    /// <summary>
    /// This is this > [this] = Something
    /// </summary>
    public IrTemp Result { get; set; }
    public string Instr { get; }
    public List<IrOperand> Operands { get; } = new List<IrOperand>();
    public string Extra { get; set; }

    public IrInstr(string instr, IrTemp result = null, params IrOperand[] operands)
    {
        Instr = instr;
        Result = result;
        Operands = operands.ToList();
    }

    public string Dump()
    {
        var sb = new StringBuilder();
        if (Result != null) sb.Append($"{Result.Dump()} = ");
        sb.Append(Instr);
        if (Operands.Count > 0)
            sb.Append(" " + string.Join(", ", Operands.Select(o => o.Dump())));
        if (!string.IsNullOrEmpty(Extra)) sb.Append(" " + Extra);
        return sb.ToString();
    }

}

public class IrBlock
{
    public string Label { get; set; }
    public List<IrInstr> Instrs { get; set; } = new List<IrInstr>();
    public IrBlock(string label)
    {
        Label = label;
    }
}

public class IrFunction
{
    public string Name { get; set; }
    public IrType ReturnType { get; set; }
    public List<IrLocal> Locals { get; set; } = new List<IrLocal>();
    public List<IrTemp> Temps { get; set; } = new List<IrTemp>();
    public List<IrBlock> Blocks { get; set; } = new List<IrBlock>();
    public List<IrLocal> Parameters { get; set; } = new List<IrLocal>();
    public int tempCounter = 0;

    public IrFunction(string name, IrType retType)
    {
        Name = name;
        ReturnType = retType;
    }

    public IrTemp NewTemp(IrType type)
    {
        IrTemp temp = new IrTemp(tempCounter++, type);
        Temps.Add(temp);
        return temp;
    }
    public IrLocal NewLocal(string name, IrType type)
    {
        var l = new IrLocal(name, type);
        Locals.Add(l);
        return l;
    }
    public IrBlock NewBlock(string label)
    {
        var b = new IrBlock(label);
        Blocks.Add(b);
        return b;
    }
}

public class IrModule
{
    public List<IrConstStr> Strings { get; } = new();
    public List<IrFunction> Functions { get; } = new();
    public IrConstStr InternString(string s)
    {
        var found = Strings.FirstOrDefault(x => x.Value == s);
        if (found != null) return found;
        var cs = new IrConstStr(s) { Label = $"S{Strings.Count}" };
        Strings.Add(cs);
        return cs;
    }
    public IrFunction AddFunction(IrFunction f)
    {
        Functions.Add(f);
        return f;
    }
}