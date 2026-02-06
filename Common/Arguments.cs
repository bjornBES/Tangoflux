using System.ComponentModel;
using BjornBEs.Libs.EasyArgs;

namespace Common
{
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

        public bool dumpTokens { get; set; } = false;
        public bool json { get; set; } = false;

        [Arg("", "--disable-debug-print-out")]
        [DefaultValue(false)]
        public bool disableDebugPrinting { get; set; } = false;
        public bool debug { get; set; }
        public bool UseLSP { get; set; }
        public bool UseStdio { get; set; }
        public bool WriteDebugFiles { get; set; } = false;
    }
}