public class PhysicalRegister
{
    public string Name { get; }
    public RegisterClass Class { get; }
    public bool CallerSaved { get; }
    public bool CalleeSaved { get; }

    public PhysicalRegister(string name, RegisterClass cls, bool callerSaved, bool calleeSaved)
    {
        Name = name;
        Class = cls;
        CallerSaved = callerSaved;
        CalleeSaved = calleeSaved;
    }

    public override string ToString() => Name;
    public RegisterInfo GetInfo()
    {
        return new RegisterInfo(Name, 0);
    }
}
