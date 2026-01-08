public sealed class RegOperand : AsmOperand
{
    public RegisterInfo Register;

    public RegOperand(RegisterInfo reg)
    {
        Register = reg;
    }

    public override string ToString() => Register.Name;
}
