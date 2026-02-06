namespace TangoFlexCompiler.Generator.Backend.Registers
{
    public class RegisterInfo
    {
        public readonly string Name;
        public int Index { get; set; }

        public RegisterInfo(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public override string ToString() => Name;
    }
}