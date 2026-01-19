
public abstract class GeneratorBase
{
    public List<string> Output = new List<string>();
    internal Arguments Arguments { get; set; }
    internal IrModule Module { get; set; }
    protected ScratchRegisterAllocator ScratchAllocator { get; set; }
    public GeneratorBase(IrModule module, Arguments arguments)
    {
        Module = module;
        Arguments = arguments;
    }
    public abstract void Generate();
    public abstract void GeneratePass();
}

public class TangoFlexGenerator
{
    public List<string> Output = new List<string>();
    Arguments Arguments { get; set; }
    IrModule Module { get; set; }
    GeneratorBase Generator { get; set; }
    public TangoFlexGenerator(IrModule module, Arguments arguments)
    {
        Module = module;
        Arguments = arguments;

        Generate();
    }

    void Generate()
    {
        if (Arguments.Backend == "asm")
        {
            if (Arguments.CallingConventions == CallingConventions.SysV)
            {
                Generator = new GeneratorAsm(Module, Arguments);
                Generator.GeneratePass();
                Generator.Generate();
            }
        }

        Output = Generator.Output;
    }
}
