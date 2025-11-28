public class GeneratorAsm : GeneratorBase
{
    public List<string> IncludeList = new List<string>();
    public TFAsmGen TFAsmGen;
    ICallingConvention callingConvention;
    RegisterProfile registerProfile;
    public GeneratorAsm(NodeProg ast, Arguments arguments) : base(ast, arguments)
    {
        if (arguments.CallingConventions == CallingConventions.SysV)
        {
            if (arguments.Bits == 64)
            {
                callingConvention = BackendSelector.Create(RegisterProfile.X86_64_SysV);
                registerProfile = RegisterProfile.X86_64_SysV;
            }
            else if (arguments.Bits == 32)
            {
                callingConvention = BackendSelector.Create(RegisterProfile.X86_32_SysV);
                registerProfile = RegisterProfile.X86_32_SysV;
            }
        }

        TFAsmGen = new TFAsmGen(callingConvention);
    }

    public override void Generate()
    {
        // ALSO TEMP
        List<NodeStmt> stmts = AST.stmts;
        for (int i = 0; i < stmts.Count; i++)
        {
            IStmt stmt = stmts[i].stmt;
            genStmt(stmt);
        }
    }

    // **TEMP**
    // TODO:
    string funcName = "";
    void genFunc(NodeStmtFuncDecl stmtFuncDecl)
    {
        funcName = stmtFuncDecl.funcName;
        Output.Add(TFAsmGen.EnterFunction(stmtFuncDecl.funcName, 0, callingConvention, registerProfile));

        genScope(stmtFuncDecl.scope);
    }

    public int scopeCount = 0;

    void genScope(NodeStmtScope scope)
    {
        List<NodeStmt> stmts = scope.stmts;
        scopeCount++;
        for (int i = 0; i < stmts.Count; i++)
        {
            IStmt stmt = stmts[i].stmt;
            genStmt(stmt);
        }
        scopeCount--;
        if (scopeCount == 0)
        {
            Console.WriteLine("Exit function");
            Output.Add(TFAsmGen.LeaveFunction(funcName, 0, callingConvention, registerProfile));
        }
    }

    void genReturn(NodeStmtReturn returnStmt)
    {
        Output.Add($"\tmov rax, 0");
    }

    void genStmt(IStmt stmt)
    {
        Console.WriteLine($"got {stmt}");
        if (stmt is NodeStmtFuncDecl stmtFuncDecl)
        {
            genFunc(stmtFuncDecl);
        }
        else if (stmt is NodeStmtScope scope)
        {
            genScope(scope);
        }
        else if (stmt is NodeStmtReturn stmtReturn)
        {
            genReturn(stmtReturn);
        }
    }


}