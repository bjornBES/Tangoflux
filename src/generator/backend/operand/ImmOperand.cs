public sealed class ImmOperand : AsmOperand
{
    public long Value;

    public ImmOperand(long value)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString();
}
