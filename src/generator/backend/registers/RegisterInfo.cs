public class RegisterInfo
{
    public readonly string Name;
    public readonly int Index;

    public RegisterInfo(string name, int index)
    {
        Name = name;
        Index = index;
    }

    public override string ToString() => Name;
}
