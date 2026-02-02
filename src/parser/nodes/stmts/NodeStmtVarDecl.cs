
using System.Text.Json.Serialization;

public class NodeStmtVarDecl : IStmt
{
    public string name { get; set; }
    public NodeType type{ get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NodeExpr expr{ get; set; }
    public NodeVisibility nodeVisibility{get; set;}
}