using TangoFlexCompiler.SymbolParser;

namespace TangoFlexLSP
{
    public class SymbolTable
    {
        public Symbol[] Symbols{ get; set; }

        public Symbol[] VisibleAt()
        {
            return null;            
        }
    }
}