
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

public class TangoFlexIRParser
{
    public bool debug = true;
    public string debugFile = Path.GetFullPath(Path.Combine("debug", "ir.txt"));

    public List<string> output = new List<string>();

    public NodeProg AST;
    public Arguments Arguments;

    int indent = 0;
    List<Variabel> variabels = new List<Variabel>();
    Stack<int> scopeStack = new Stack<int>();
    Stack<string> allocVariabels = new Stack<string>();

    public TangoFlexIRParser(NodeProg ast, Arguments args)
    {
        AST = ast;
        Arguments = args;

        File.WriteAllText(debugFile, "");

        GenerateIR(ast.stmts.ToArray());

        File.WriteAllLines("./testIR1.irtf", output);
    }

    string generateTerm(NodeTerm term)
    {
        if (term.GetTrueType(out NodeTermIntlit termIntlit))
        {
            return $"const_i64 {termIntlit.value}";
        }
        else if (term.GetTrueType(out NodeTermVar termVar))
        {
            return $"@{termVar.name}";
        }

        return "const_error";
    }

    string generateExpr(NodeExpr expr)
    {
        if (expr.GetTrueType(out NodeTerm term))
        {
            return generateTerm(term);
        }

        return "IDK ERROR";
    }

    bool getVariabel(string name, out Variabel var)
    {
        for (int i = 0; i < variabels.Count; i++)
        {
            if (name == variabels[i].RealName.TrimEnd())
            {
                var = variabels[i];
                return true;
            }
        }
        var = null;
        return false;
    }

    void generateStmt(NodeStmt stmt)
    {
        if (stmt.GetTrueType(out NodeStmtFuncDecl stmtFuncDecl))
        {
            writeToOutput($"func @{stmtFuncDecl.funcName}() : {stmtFuncDecl.returnType.type}");
            pushScope();
            int currIndex = output.Count;
            generateScope(stmtFuncDecl.scope);

            int count = allocVariabels.Count;
            for (int i = 0; i < count; i++)
            {
                string line = allocVariabels.Pop();
                string format = formatToOutput(line);
                output.Insert(currIndex, format);
                currIndex++;
            }
            output.Insert(currIndex, "entry:");
            popScope();
        }
        else if (stmt.GetTrueType(out NodeStmtVarDecl stmtVarDecl))
        {
            string formatedVarName = "@" + stmtVarDecl.name;
            int newIndex = variabels.Count;
            variabels.Add(new Variabel(newIndex + formatedVarName));
            Variabel variabel = variabels[^1];
            allocVariabels.Push($"{variabel.IRName} = alloc 8");
            if (stmtVarDecl.expr == null)
            {
            }
            else
            {
                writeToOutput($"store {variabel.IRName}, {generateExpr(stmtVarDecl.expr)}");
            }
        }
        else if (stmt.GetTrueType(out NodeStmtReturn stmtReturn))
        {
            string expr = generateExpr(stmtReturn.expr);
            if (getVariabel(expr, out Variabel var))
            {
                writeToOutput($"ret {var.IRName}");
            }
            else
            {
                writeToOutput($"ret {expr}");
            }
        }
    }

    void generateScope(NodeStmtScope nodeStmtScope)
    {
        GenerateIR(nodeStmtScope.stmts.ToArray());
    }

    public void GenerateIR(NodeStmt[] nodeStmts)
    {
        for (int i = 0; i < nodeStmts.Length; i++)
        {
            NodeStmt nodeStmt = nodeStmts[i];
            generateStmt(nodeStmt);
        }
    }

    string formatToOutput(string line)
    {
        return "".PadRight(indent, '\t') + line;
    }

    void writeToOutput(string line)
    {
        output.Add(formatToOutput(line));
        File.AppendAllText(debugFile, line + Environment.NewLine);
    }

    void pushScope()
    {
        indent++;
        File.AppendAllText(debugFile, $"enter scope {scopeStack.Count + 1}{Environment.NewLine}");
        scopeStack.Push(variabels.Count);
    }
    void popScope()
    {
        File.AppendAllText(debugFile, $"leave scope {scopeStack.Count}{Environment.NewLine}");
        int popCount = variabels.Count - scopeStack.Peek();
        if (popCount != 0)
        {
            writeToOutput($"leave {popCount}");
        }

        for (int i = 0; i < popCount; i++)
        {
            variabels.Remove(variabels[^1]);
        }
        indent--;
        scopeStack.Pop();
    }
}

public class Variabel
{
    public string IRName
    {
        get
        {
            return $"%v{Index}";
        }
    }

    public string RealName;

    public int Index;

    public Variabel(string readName)
    {
        RealName = readName;
    }

    public void ValueChanged()
    {
        Index++;
    }
}