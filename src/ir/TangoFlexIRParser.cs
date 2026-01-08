
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using CompilerTangoFlex.lexer;

public class TangoFlexIRParser
{
    public bool debug = true;
    public string debugFile = Path.GetFullPath(Path.Combine("debug", "ir.txt"));


    public NodeProg AST;
    public Arguments Arguments;

    public IrModule Module;
    private readonly Dictionary<string, IrFunction> functions = new Dictionary<string, IrFunction>();

    public TangoFlexIRParser(NodeProg ast, Arguments args)
    {
        AST = ast;
        Arguments = args;
        Module = new IrModule();

        NodeStmt[] stmts = ast.stmts.ToArray();

        File.WriteAllText(debugFile, "");

        foreach (NodeStmt stmt in stmts)
        {
            if (stmt.GetTrueType(out NodeStmtFuncDecl funcDecl))
            {
                IrType retType = MapType(funcDecl.returnType);
                // var paramList = funcDecl.parameters.Select(p => (p.name, MapType(p.Item2))).ToArray();
                IrFunction f = new IrFunction(funcDecl.funcName, retType);
                Module.AddFunction(f);
                functions[funcDecl.funcName] = f;
            }
        }

        GenerateIR(ast.stmts.ToArray());

        string dump = IrDumper.Dump(Module);

        File.WriteAllText(debugFile, dump);
    }

    private IrType MapType(NodeType nt)
    {
        if (nt == null)
        {
            return IrType.Void;
        }
        return new IrType(nt);
    }
    void LowerFunction(NodeStmtFuncDecl stmtFuncDecl)
    {
        IrFunction function = functions[stmtFuncDecl.funcName];

        IrBlock entry = function.NewBlock("entry");

        if (stmtFuncDecl.scope == null)
        {

        }

        LowerFuncContext ctx = new LowerFuncContext(Module, function, functions);
        ctx.CurrentBlock = entry;

        if (stmtFuncDecl.parameters.Count > 0)
        {
            foreach (FuncArguments param in stmtFuncDecl.parameters)
            {
                IrType type = MapType(param.Type);
                IrLocal paramLocal = function.NewLocal(param.Name, type);
                function.Parameters.Add(paramLocal);
                ctx.Locals[param.Name] = paramLocal;
            }
        }

        LowerScope(stmtFuncDecl.scope, ctx);

        IrBlock lastBlock = function.Blocks.Last();
        if (!lastBlock.Instrs.Any() || !IsTerminator(lastBlock.Instrs.Last()))
        {
            // default return 0 for int, null for void
            if (function.ReturnType == IrType.Int)
            {
                lastBlock.Instrs.Add(new IrInstr("ret", null, new IrConstInt(0)));
            }
            else
            {
                lastBlock.Instrs.Add(new IrInstr("ret"));
            }
        }
    }

    private static bool IsTerminator(IrInstr instr)
    {
        if (instr == null) return false;
        return instr.Instr is "ret" or "jmp" or "cjmp";
    }

    void LowerScope(NodeStmtScope scope, LowerFuncContext ctx)
    {
        foreach (NodeStmt s in scope.stmts)
        {
            if (s.GetTrueType(out NodeStmtReturn ret))
            {
                LowerReturn(ret, ctx);
            }
            else if (s.GetTrueType(out NodeStmtVarDecl vdecl))
            {
                LowerVarDecl(vdecl, ctx);
            }
            else if (s.GetTrueType(out NodeStmtScope innerScope))
            {
                // optionally create a new block before entering a nested scope
                IrBlock newBlock = ctx.Function.NewBlock("inner_scope");
                ctx.CurrentBlock = newBlock;

                LowerScope(innerScope, ctx);

                // after nested scope, create another new block for subsequent instructions
                IrBlock afterBlock = ctx.Function.NewBlock("after_inner_scope");
                ctx.CurrentBlock = afterBlock;
            }
            else if (s.GetTrueType(out NodeStmtFuncDecl func))
            {
                throw new NotSupportedException("Nested functions are not supported in this lowering.");
            }
            else
            {
                // other statements, like expressions
            }

/*
            if (ctx.CurrentBlock.Instrs.Count > 0)
            {
                IrInstr lastInstr = ctx.CurrentBlock.Instrs.Last();
                if (IsTerminator(lastInstr))
                {
                    // create a new block for subsequent instructions
                    IrBlock newBlock = ctx.Function.NewBlock("after_terminator");
                    ctx.CurrentBlock = newBlock;
                }
            }
*/
        }
    }

    private void LowerReturn(NodeStmtReturn ret, LowerFuncContext ctx)
    {
        if (ret.expr == null)
        {
            ctx.CurrentBlock.Instrs.Add(new IrInstr("ret"));
            return;
        }
        IrOperand v = LowerExpr(ret.expr, ctx);
        ctx.CurrentBlock.Instrs.Add(new IrInstr("ret", null, v));
    }

    void LowerVarDecl(NodeStmtVarDecl vdecl, LowerFuncContext ctx)
    {
        string name = vdecl.name;
        IrType type = MapType(vdecl.type);
        IrLocal local = ctx.Function.NewLocal(name, type);
        ctx.Locals[name] = local;


        if (vdecl.expr != null)
        {
            IrOperand operand = LowerExpr(vdecl.expr, ctx);
            ctx.CurrentBlock.Instrs.Add(new IrInstr("move", null, local, operand));
            local.HasInit = true;
        }
        else
        {
            if (type == IrType.Int)
            {
                local.HasInit = true;
                ctx.CurrentBlock.Instrs.Add(new IrInstr("move", null, local, new IrConstInt(0)));
            }
        }
    }

    IrOperand LowerExpr(NodeExpr expr, LowerFuncContext ctx)
    {
        if (expr.GetTrueType(out NodeTerm term))
        {
            return LowerTerm(term, ctx);
        }

        /*
                switch (expr.expr)
                {
                    case NodeExprBinary eb:
                        // left-to-right: lower left, then right, then emit op
                        var leftOp = LowerExpr(eb.Left.Expr, ctx) as IrOperand;
                        var rightOp = LowerExpr(eb.Right.Expr, ctx) as IrOperand;
                        // coerces to temps if necessary
                        IrOperand leftTemp = EnsureTemp(leftOp, ctx, IrType.Int);
                        IrOperand rightTemp = EnsureTemp(rightOp, ctx, IrType.Int);
                        IrTemp outTemp = ctx.Function.NewTemp(IrType.Int);
                        ctx.CurrentBlock.Instrs.Add(new IrInstr(OperatorToOp(eb.Operator.Val), outTemp, leftTemp, rightTemp));
                        return outTemp;

                    case NodeExprCall call:
                        // evaluate args left-to-right into temps
                        var argTemps = new List<IrOperand>();
                        if (call.Args != null)
                        {
                            foreach (var a in call.Args)
                            {
                                IrOperand v = LowerExpr(a.Expr, ctx);
                                argTemps.Add(EnsureTemp(v, ctx, ArgTypeForOperand(v)));
                            }
                        }
                        // create call instr (result temp depends on callee)
                        if (!functions.TryGetValue(call.FuncName, out IrFunction calleeFn))
                        {
                            // unknown function -> treat as external returning int for now
                            calleeFn = new IrFunction(call.FuncName, IrType.Int);
                            Module.AddFunction(calleeFn);
                            functions[call.FuncName] = calleeFn;
                        }
                        IrTemp retTemp = calleeFn.ReturnType == IrType.Void ? null : ctx.Function.NewTemp(calleeFn.ReturnType);
                        // produce call instruction
                        var callInstr = new IrInstr($"call @{call.FuncName}", retTemp);
                        foreach (IrOperand a in argTemps) callInstr.Operands.Add(a);
                        ctx.CurrentBlock.Instrs.Add(callInstr);
                        return retTemp ?? (IrOperand)new IrConstInt(0);

                }
        */
        throw new NotSupportedException($"Expr node type not supported: {expr.GetType().Name}");
    }

    private IrOperand LowerTerm(NodeTerm term, LowerFuncContext ctx)
    {
        Console.WriteLine($"term is {term.term}");
        if (term.GetTrueType(out NodeTermIntlit intlit))
        {
            return new IrConstInt(intlit.value);
        }
        else if (term.GetTrueType(out NodeTermVar var))
        {
            if (!ctx.Locals.TryGetValue(var.name, out IrLocal local))
                throw new Exception($"Undefined local/var '{var.name}' in function {ctx.Function.Name}");
            IrTemp t = ctx.Function.NewTemp(local.Type);
            ctx.CurrentBlock.Instrs.Add(new IrInstr("move", t, local));
            return t;
        }
        else
        {
            Console.WriteLine($"term is {term.term}");
        }

        throw new NotSupportedException($"Term node unsupported: {term.term.GetType().Name}");
    }

    void generateStmt(NodeStmt stmt)
    {
        if (stmt.GetTrueType(out NodeStmtFuncDecl stmtFuncDecl))
        {
            LowerFunction(stmtFuncDecl);
        }
    }

    public void GenerateIR(NodeStmt[] nodeStmts)
    {
        for (int i = 0; i < nodeStmts.Length; i++)
        {
            NodeStmt nodeStmt = nodeStmts[i];
            generateStmt(nodeStmt);
        }
    }
}

public class LowerFuncContext
{
    public IrModule Module { get; }
    public IrFunction Function { get; }
    public Dictionary<string, IrFunction> GlobalFunctions { get; }
    public IrBlock CurrentBlock { get; set; }
    public Dictionary<string, IrLocal> Locals { get; } = new Dictionary<string, IrLocal>(StringComparer.Ordinal);

    public LowerFuncContext(IrModule module, IrFunction function, Dictionary<string, IrFunction> globalFunctions)
    {
        Module = module; Function = function; GlobalFunctions = globalFunctions;
    }
}

public static class IrDumper
{
    public static string Dump(IrModule m)
    {
        StringBuilder sb = new StringBuilder();
        foreach (IrConstStr s in m.Strings)
            sb.AppendLine($"const_str {s.Label} = \"{s.Value}\"");
        sb.AppendLine();

        foreach (IrFunction f in m.Functions)
        {
            string paramList = "";
            foreach (IrLocal param in f.Parameters)
            {
                paramList += $"{param.Name} : {param.Type.Dump()}, ";
            }
            paramList = paramList.TrimEnd(' ', ',');
            sb.AppendLine($"func @{f.Name}({paramList}) : {f.ReturnType.Dump()}");
            foreach (IrLocal l in f.Locals)
                sb.AppendLine($"    local {l.Name} : {l.Type.Dump()}");
            foreach (IrBlock b in f.Blocks)
            {
                sb.AppendLine($"{b.Label}:");
                foreach (IrInstr i in b.Instrs)
                    sb.AppendLine($"    {i.Dump()}");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}