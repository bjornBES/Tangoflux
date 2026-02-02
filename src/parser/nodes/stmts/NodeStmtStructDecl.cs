public class NodeStructField
{
    public string fieldName {get; set;}
    public NodeVisibility nodeVisibility{get; set;}
    public NodeType fieldType {get; set;}
    // syntax var <visibility> <Name> : <Type>

}

public class NodeStmtStructDecl : IStmt
{
    public string structName {get; set;}
    public NodeVisibility nodeVisibility{get; set;}
    public List<NodeStructField> fields {get; set;} = new List<NodeStructField>();
}
