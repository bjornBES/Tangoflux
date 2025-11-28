
public abstract class GeneratorBase
{
    public List<string> Output = new List<string>();
    internal Arguments Arguments { get; set; }
    internal NodeProg AST { get; set; }
    public GeneratorBase(NodeProg ast, Arguments arguments)
    {
        AST = ast;
        Arguments = arguments;
    }
    public abstract void Generate();
}

public class TangoFlexGenerator
{
    public List<string> Output = new List<string>();
    Arguments Arguments { get; set; }
    NodeProg AST { get; set; }
    GeneratorBase Generator { get; set; }
    public TangoFlexGenerator(NodeProg ast, Arguments arguments)
    {
        AST = ast;
        Arguments = arguments;

        Generate();
    }

    void Generate()
    {
        if (Arguments.Backend == "asm")
        {
            if (Arguments.CallingConventions == CallingConventions.SysV)
            {
                Generator = new GeneratorAsm(AST, Arguments);
                Generator.Generate();
            }
        }

        Output = Generator.Output;
    }
}
