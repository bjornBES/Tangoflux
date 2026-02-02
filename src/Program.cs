using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using BjornBEs.Libs.EasyArgs;
using CompilerTangoFlex.lexer;
using TangoFlex.preprocessor;

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
    [Arg("-o", "")]
    [ArgAttributeHelp("Place the output into the file", "", HelpPlaceholder = "<file>")]
    [DefaultValue("./out")]
    public string OutputFile { get; set; }

    [DefaultValue(CallingConventions.SysV)]
    [Arg("", "--cc", AllowedValues = ["SysV", "cdecl", "stdcall", "fastcall"])]
    [ArgAttributeHelp("Select calling convention", "Systems", HelpPlaceholder = "<calling_convention>",
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

    [Arg("", "--fat-string")]
    [ArgAttributeHelp("Uses the string struct for String types", "Systems", ShowByDefault = true, ShowList = false)]
    [DefaultValue(false)]
    public bool UseFatStrings { get; set; } = false;

    [DefaultValue(false)]
    public bool OneCharNewLine { get; set; } = false;

    [Arg("", "--dump-tokens")]
    [DefaultValue(false)]
    public bool dumpTokens { get; set; } = false;
    [Arg("", "--json")]
    [DefaultValue(false)]
    public bool json { get; set; } = false;

    [Arg("", "--disable-debug-print-out")]
    [DefaultValue(false)]
    public bool disableDebugPrinting { get; set; } = false;

    [Arg("-debug", "")]
    [DefaultValue(true)]
    public bool debug { get; set; }

}

/*
for i386    --bits 32 --arch x86
for AMD64   --bits 64 --arch amd
*/

public class Program
{
    public static Arguments Arguments;
    public static void Main(string[] args)
    {
        TangoFlexPreprocessor preprocessor;
        TangoFlexLexer tangoFlexLexer;
        Arguments = EasyArgs.Parse<Arguments>(args);

        DebugConsole.InitDebugging(Arguments);


        if (Arguments.dumpTokens == false && Arguments.json == false)
        {
            string src = File.ReadAllText(Arguments.InputFile);
            preprocessor = new TangoFlexPreprocessor(src, Arguments);
            tangoFlexLexer = new TangoFlexLexer(preprocessor.Source, Arguments);
            // 1. the one input file that is it
            TangoFlexPreprocessorEval tangoFlexPreprocessorEval = new TangoFlexPreprocessorEval(tangoFlexLexer.Tokens.ToArray(), tangoFlexLexer, preprocessor, Arguments);
            // 2. now with all extensions and includes
            TangoFlexParser tangoFlexParser = new TangoFlexParser(tangoFlexPreprocessorEval.tokenOut, Arguments);
            TangoFlexIRParser tangoFlexIRParser = new TangoFlexIRParser(tangoFlexParser.AST, Arguments);
            TangoFlexGenerator tangoFlexGenerator = new TangoFlexGenerator(tangoFlexIRParser.Module, Arguments);
            File.WriteAllText(Arguments.OutputFile, string.Join(Environment.NewLine, tangoFlexGenerator.Output));
        }
        else
        {
            Arguments.disableDebugPrinting = true;
            Arguments.debug = false;
            // Lexer shit
            string src = Console.In.ReadToEnd();
            preprocessor = new TangoFlexPreprocessor(src, Arguments);
            tangoFlexLexer = new TangoFlexLexer(preprocessor.Source, Arguments);
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
            {
                MaxDepth = 64,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            serializerOptions.Converters.Add(new JsonStringEnumConverter());
            string json = JsonSerializer.Serialize(tangoFlexLexer.Tokens, serializerOptions);

            Console.Out.WriteLine(json);
        }
    }
}