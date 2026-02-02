
using System.Drawing;
using System.Text;
using CompilerTangoFlex.lexer;

//public enum IrType { Int, Float, Bool, String, Void, Ptr }

public class IrType
{
    public static IrType Void = new IrType(KeywordVal.VOID);
    public static IrType VoidPtr = new IrType(new NodeType() { Type = KeywordVal.PTR, NestedTypes = new NodeType() { Type = KeywordVal.VOID }});
    public static IrType Boolean = new IrType(KeywordVal.BOOL);
    public static IrType ULong = new IrType(KeywordVal.UINT64);
    public static IrType Int = new IrType(KeywordVal.INT);
    public static IrType UShort = new IrType(KeywordVal.UINT16);
    public static IrType Byte = new IrType(KeywordVal.UINT8);
    public static IrType BytePtr = new IrType(new NodeType() { Type = KeywordVal.PTR, NestedTypes = new NodeType() { Type = KeywordVal.VOID }});
    public static IrType String = new IrType(KeywordVal.STRING);

    public int SizeInBits { get; set; }
    public int SizeInBytes { get; set; }
    public bool IsSigned { get; set; }
    public int Alignment { get; set; }
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
                if (Program.Arguments.Bits == 32)
                {
                    SizeInBits = 32;
                    SizeInBytes = 4;
                    Alignment = 4;
                }
                else if (Program.Arguments.Bits == 64)
                {
                    SizeInBits = 64;
                    SizeInBytes = 8;
                    Alignment = 8;
                }
                IsSigned = false;
                RefType = new IrType(type.NestedTypes);
                break;

            case KeywordVal.INT8:
                SizeInBits = 8;
                SizeInBytes = 1;
                Alignment = 1;
                IsSigned = true;
                break;

            case KeywordVal.UINT8:
            case KeywordVal.BOOL:
                SizeInBits = 8;
                SizeInBytes = 1;
                Alignment = 1;
                IsSigned = false;
                break;

            case KeywordVal.INT16:
                SizeInBits = 16;
                SizeInBytes = 2;
                Alignment = 2;
                IsSigned = true;
                break;

            case KeywordVal.UINT16:
                SizeInBits = 16;
                SizeInBytes = 2;
                Alignment = 2;
                IsSigned = false;
                break;

            case KeywordVal.INT32:
                SizeInBits = 32;
                SizeInBytes = 4;
                Alignment = 4;
                IsSigned = true;
                break;

            case KeywordVal.UINT32:
                SizeInBits = 32;
                SizeInBytes = 4;
                Alignment = 4;
                IsSigned = false;
                break;

            case KeywordVal.FLOAT:
                // TODO: floats
                break;

            case KeywordVal.INT64:
                SizeInBits = 64;
                SizeInBytes = 8;
                Alignment = 8;
                IsSigned = true;
                break;

            case KeywordVal.UINT64:
                SizeInBits = 64;
                SizeInBytes = 8;
                Alignment = 8;
                IsSigned = false;
                break;

            case KeywordVal.STRING:
            if (Program.Arguments.UseFatStrings)
                {
                    // TODO: LowerStringType
                    // use fat strings at some point (not now)
                    // def: fat strings is a string that has the length in it
                }
                else
                {
                    // cstring just the value
                    Alignment = 8;
                    SizeInBytes = 8;
                    SizeInBits = 64;
                    IsSigned = false;
                    RefType = new IrType(KeywordVal.UINT8);
                }
                break;

            case KeywordVal.VOID:
                Alignment = 0;
                SizeInBytes = 0;
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

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj.GetType() == typeof(IrType))
        {
            return Equals((IrType)obj);
        }
        return false;
    }

    public bool Equals(IrType type)
    {
        if (SizeInBytes == type.SizeInBytes && IsSigned == type.IsSigned && Alignment == type.Alignment)
        {
            if (RefType != null && type.RefType != null)
            {
                return RefType.Equals(type.RefType); 
            }
            return true;
        }
        return false;
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
    public IrType Type { get; }
    public IrConstInt(long v, IrType type)
    {
        Value = v;
        Type = type;
    }
    public override string Dump()
    {
        switch (Type.SizeInBits)
        {
            case 64:
                return $"constI64 {Value}";
            case 32:
                return $"constI32 {Value}";
            case 16:
                return $"constI16 {Value}";
            case 8:
                return $"constI8 {Value}";
                
        }
        return $"constI64 {Value}";
    }
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

public class IrLabel : IrOperand
{
    public string Name { get; }
    public IrLabel(string name) { Name = name; }
    public override string Dump() => $"{Name}";
}

public class IrInstr
{
    /// <summary>
    /// This is this > [this] = Something
    /// </summary>
    public IrTemp Result { get; set; }
    public string Instructions { get; }
    public List<IrOperand> Operands { get; } = new List<IrOperand>();
    public string Extra { get; set; }

    public IrInstr(string instr, IrTemp result = null, params IrOperand[] operands)
    {
        Instructions = instr;
        Result = result;
        Operands = operands.ToList();
    }

    public string Dump()
    {
        var sb = new StringBuilder();
        if (Result != null) sb.Append($"{Result.Dump()} = ");
        sb.Append(Instructions);
        if (Operands.Count > 0)
            sb.Append(" " + string.Join(", ", Operands.Select(o => o.Dump())));
        if (!string.IsNullOrEmpty(Extra)) sb.Append(" " + Extra);
        return sb.ToString();
    }

}

public class IrBlock
{
    public string Label { get; set; }
    public List<IrInstr> Instructions { get; set; } = new List<IrInstr>();
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
    public bool isExternal = false;
    public int tempCounter = 0;
    public int blockCounter = 0;

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
        var b = new IrBlock(label + $"{blockCounter++}");
        Blocks.Add(b);
        return b;
    }
}


public sealed class IrStructField
{
    public string Name { get; }
    public IrType Type { get; }
    public NodeVisibility Visibility { get; }
    public int Index { get; }   // VERY important

    public IrStructField(string name, IrType type, NodeVisibility visibility, int index)
    {
        Name = name;
        Type = type;
        Visibility = visibility;
        Index = index;
    }
}

public sealed class IrStruct
{
    public string Name { get; }
    public NodeVisibility Visibility { get; }
    public StructLayout Layout {get; set;}
    public bool IsPacked;
    public List<IrStructField> Fields { get; } = new();

    public IrStruct(string name, NodeVisibility visibility)
    {
        Name = name;
        Visibility = visibility;
    }
}

public sealed class StructLayout
{
    public int Size;
    public int Alignment;
    public List<int> FieldOffsets;
}


public class IrModule
{
    public List<IrConstStr> Strings { get; } = new();
    public List<IrStruct> Structs { get; } = new();
    public List<IrFunction> Functions { get; } = new();
    public List<IrLocal> Globals { get; } = new();
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
    public IrStruct AddStruct(IrStruct s)
    {
        Structs.Add(s);
        return s;
    }
    public IrFunction AddExternalFunction(IrFunction f)
    {
        f.isExternal = true;
        Functions.Add(f);
        return f;
    }
}