using BjornBEs.Libs.EasyArgs;
using CompilerTangoFlex.lexer;

public enum CallingConventions
{
    SysV,
    cdecl,
}


public class Arguments
{
    [ArgAttribute("-i", "-input", Help ="Input file", Required = true)]
    public string InputFile { get; set; }
    [ArgAttribute("-o", "", Help = "", Required = true)]
    public string OutputFile { get; set; }


    [ArgAttribute("", "--cc", Help = "--cc (SysV|cdecl)", Required = false)]
    public CallingConventions CallingConventions { get; set; }

    [ArgAttribute("", "--backend", Help = "--backend (asm|c)", Required = false)]
    public string Backend { get; set; }

    [ArgAttribute("", "--bits", Help = "--bits (32|64)", Required = false)]
    public int Bits { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        Arguments arguments = EasyArgs.Parse<Arguments>(args);
        string src = File.ReadAllText(arguments.InputFile);

        TangoFlexPreprocessor preprocessor = new TangoFlexPreprocessor(src, arguments);
        TangoFlexLexer tangoFlexLexer = new TangoFlexLexer(preprocessor.SourceCode, arguments);
        TangoFlexParser tangoFlexParser = new TangoFlexParser(tangoFlexLexer.Tokens.ToArray(), arguments);
        TangoFlexIRParser tangoFlexIRParser = new TangoFlexIRParser(tangoFlexParser.AST, arguments);
        TangoFlexGenerator tangoFlexGenerator = new TangoFlexGenerator(tangoFlexIRParser.Module, arguments);
        File.WriteAllText(arguments.OutputFile, string.Join(Environment.NewLine, tangoFlexGenerator.Output));

        Console.WriteLine("Hello world");
    }
}