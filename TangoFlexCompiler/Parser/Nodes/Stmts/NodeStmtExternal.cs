
using System.Text.Json.Serialization;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.Externals;

namespace TangoFlexCompiler.Parser.Nodes.Stmts
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ExternalFunc), "ExternalFunc")]
    public interface IExternal
    {
    }

    public class NodeStmtExternal : IStmt
    {
        public string From { get; set; }
        public IExternal External { get; set; }
        public bool GetTrueType<T>(out T out_stmt) where T : IExternal
        {
            if (External.GetType() == typeof(T) && External is T inst)
            {
                out_stmt = inst;
                return true;
            }
            out_stmt = default;
            return false;
        }
    }
}