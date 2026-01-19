
public class PtrOperand : AsmOperand
{
    public string Label;

    public PtrOperand(string label)
    {
        Label = label;
    }

    public override string GetByteRepresentation()
    {
        return Label;
    }

    public override string GetLongRepresentation()
    {
        return Label;
    }

    public override string ToString()
    {
        return $"[rel {Label}]";
    }
}