using System.ComponentModel;
using BjornBEs.Libs.EasyArgs;

namespace Common
{
    public class LSPArguments
    {
        [Arg("", "--dump-tokens")]
        [DefaultValue(false)]
        public bool dumpTokens { get; set; } = false;
        [Arg("", "--json")]
        [DefaultValue(false)]
        public bool json { get; set; } = false;

        [Arg("-debug", "--debug")]
        [DefaultValue(true)]
        public bool debug { get; set; }

        [Arg("", "--lsp")]
        [DefaultValue(false)]
        public bool UseLSP { get; set; }

        [Arg("", "--stdio")]
        [DefaultValue(false)]
        public bool UseStdio { get; set; }
    }
}