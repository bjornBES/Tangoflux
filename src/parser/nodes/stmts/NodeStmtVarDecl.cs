
public class NodeStmtVarDecl : IStmt
{
    public string name { get; set; }
    public NodeType type{ get; set; }
    public NodeExpr expr{ get; set; }
}