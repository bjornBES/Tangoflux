using System.ComponentModel;
using BjornBEs.Libs.EasyArgs;
using CompilerTangoFlex.lexer;

public enum CallingConventions
{
    SysV,
    cdecl,
}

public enum Architectures
{
    x86,
    amd,
    arm,
    RISCV
}


public class Arguments
{
    // [Arg("-i", "", Help = "Input file", Category = "", Required = true)]
    [Positional(0, Required = true, Help = "")]
    public string InputFile { get; set; }
    [Arg("-o", "", Required = true)]
    [ArgAttributeHelp("Place the output into the file", "", HelpPlaceholder = "<file>")]
    public string OutputFile { get; set; }

    [DefaultValue(CallingConventions.SysV)]
    [Arg("", "--cc", AllowedValues = ["SysV", "cdecl", "stdcall", "fastcall"])]
    [ArgAttributeHelp("Select calling convention", "Systems", HelpPlaceholder = "<calling_conv>",
    ValueDescriptions = [
        "",
        "C default calling convention",
        "Windows stdcall ABI",
        "Pass arguments in registers"
    ])]
    public CallingConventions CallingConventions { get; set; }

    [Arg("", "--backend")]
    [ArgAttributeHelp("Select backend", "Systems", HelpPlaceholder = "<asm|c>")]
    public string Backend { get; set; }

    [Arg("", "--bits")]
    [ArgAttributeHelp("Select bits", "Systems", HelpPlaceholder = "<32|64>")]
    public int Bits { get; set; }

    [Arg("", "--arch")]
    [ArgAttributeHelp("Select arch", "Systems", HelpPlaceholder = "<x86|amd>")]
    public Architectures Arch { get; set; }

    [DefaultValue(false)]
    [Arg("", "--scale-pointers")]
    [ArgAttributeHelp("Scaled pointer arithmetic by size", "Systems", ShowByDefault = false)]
    public bool ScalePointers { get; set; }

    [Arg("", "--ext", AllowedValues = ["io"])]
    [ArgAttributeHelp("Adds an extension into the program", "Systems", HelpPlaceholder = "<module>", ShowByDefault = true, ShowList = false)]
    public List<string> Extensions { get; set; }

}

/*
for i386    --bits 32 --arch x86
for AMD64   --bits 64 --arch amd
*/

public class Program
{
    public static void Main(string[] args)
    {
        Arguments arguments = EasyArgs.Parse<Arguments>(args);
        
        string src = File.ReadAllText(arguments.InputFile);

        TangoFlexPreprocessor preprocessor = new TangoFlexPreprocessor(src, arguments);
        TangoFlexLexer tangoFlexLexer = new TangoFlexLexer(preprocessor.Source, arguments);
        TangoFlexPreprocessorEval tangoFlexPreprocessorEval = new TangoFlexPreprocessorEval(tangoFlexLexer.Tokens.ToArray(), arguments);
        TangoFlexParser tangoFlexParser = new TangoFlexParser(tangoFlexPreprocessorEval.tokenOut, arguments);
        TangoFlexIRParser tangoFlexIRParser = new TangoFlexIRParser(tangoFlexParser.AST, arguments);
        TangoFlexGenerator tangoFlexGenerator = new TangoFlexGenerator(tangoFlexIRParser.Module, arguments);
        File.WriteAllText(arguments.OutputFile, string.Join(Environment.NewLine, tangoFlexGenerator.Output));

        Console.WriteLine("Hello world");
    }
}