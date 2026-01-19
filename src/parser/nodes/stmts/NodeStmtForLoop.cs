
using System.Text.Json.Serialization;

public class NodeStmtForLoop : IStmt
{
    public string Name { get; set; }
    public NodeType? Type { get; set; }
    public NodeExpr Start { get; set; }
    public NodeExpr End { get; set; }
    public NodeExpr? Step { get; set; }
    public NodeStmtScope Scope { get; set; }
}
