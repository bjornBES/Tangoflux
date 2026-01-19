
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

        bool terminated = LowerScope(stmtFuncDecl.scope, ctx);

        IrBlock lastBlock = function.Blocks.Last();
        if (!terminated && (!lastBlock.Instrs.Any() || !IsTerminator(lastBlock.Instrs.Last())))
        {
            if (function.ReturnType == IrType.Int)
                lastBlock.Instrs.Add(new IrInstr("ret", null, new IrConstInt(0)));
            else
                lastBlock.Instrs.Add(new IrInstr("ret"));
        }
    }

    private static bool IsTerminator(IrInstr instr)
    {
        if (instr == null) return false;
        return instr.Instr is "ret" or "jump" or "cjump";
    }

    bool LowerScope(NodeStmtScope scope, LowerFuncContext ctx)
    {
        foreach (NodeStmt s in scope.stmts)
        {
            if (s.GetTrueType(out NodeStmtReturn ret))
            {
                LowerReturn(ret, ctx);
                return true;
            }
            else if (s.GetTrueType(out NodeStmtVarDecl vdecl))
            {
                LowerVarDecl(vdecl, ctx);
            }
            else if (s.GetTrueType(out NodeStmtIf stmtIf))
            {
                bool innerTerminated = LowerIf(stmtIf, ctx);
                if (innerTerminated)
                {
                    return true;
                }
            }
            else if (s.GetTrueType(out NodeStmtForLoop stmtForLoop))
            {
                LowerForLoop(stmtForLoop, ctx);
            }
            else if (s.GetTrueType(out NodeStmtWhileLoop stmtWhileLoop))
            {
                LowerWhileLoop(stmtWhileLoop, ctx);
            }
            else if (s.GetTrueType(out NodeStmtVarReassign varReassign))
            {
                LowerVarReassign(varReassign, ctx);
            }

            else if (s.GetTrueType(out NodeStmtScope innerScope))
            {
                IrBlock newBlock = ctx.Function.NewBlock("inner_scope");
                ctx.CurrentBlock = newBlock;

                bool innerTerminated = LowerScope(innerScope, ctx);
                if (innerTerminated)
                {
                    return true;
                }
            }

            else if (s.GetTrueType(out NodeStmtFuncDecl func))
            {
                throw new NotSupportedException("Nested functions are not supported in this lowering.");
            }
            else
            {
                // other statements, like expressions
            }
        }

        return false;
    }

    private void LowerVarReassign(NodeStmtVarReassign varReassign, LowerFuncContext ctx)
    {
        if (!ctx.Locals.TryGetValue(varReassign.Name, out IrLocal local))
            throw new Exception($"Undefined local/var '{varReassign.Name}' in function {ctx.Function.Name}");

        IrOperand exprOp = LowerExpr(varReassign.Expr, ctx);
        ctx.CurrentBlock.Instrs.Add(new IrInstr("move", null, local, exprOp));
    }

    private void LowerWhileLoop(NodeStmtWhileLoop stmtWhileLoop, LowerFuncContext ctx)
    {
        IrLabel loopCondLabel = ctx.NewLabel("while_loop_cond");
        IrLabel loopBodyLabel = ctx.NewLabel("while_loop_body");
        IrLabel loopEndLabel = ctx.NewLabel("while_loop_end");

        // ctx.EmitJump(loopCondLabel);

        // condition
        ctx.EmitLabel(loopCondLabel);
        IrOperand condOp = LowerExpr(stmtWhileLoop.Condition, ctx);
        ctx.CurrentBlock.Instrs.Add(new IrInstr("cjump", null, new IrConstInt(1), condOp, loopEndLabel));

        // body
        // ctx.EmitLabel(loopBodyLabel);
        bool bodyTerminated = LowerScope(stmtWhileLoop.Scope, ctx);
        if (!bodyTerminated)
        {
            ctx.EmitJump(loopCondLabel);
        }

        // end
        ctx.EmitLabel(loopEndLabel);
    }

    private void LowerForLoop(NodeStmtForLoop stmtForLoop, LowerFuncContext ctx)
    {
        // init
        IrOperand startOp = LowerExpr(stmtForLoop.Start, ctx);
        IrOperand endOp = LowerExpr(stmtForLoop.End, ctx);

        IrLocal loopVar = ctx.Function.NewLocal(stmtForLoop.Name, MapType(stmtForLoop.Type));
        ctx.Locals[stmtForLoop.Name] = loopVar;

        ctx.CurrentBlock.Instrs.Add(new IrInstr("move", null, loopVar, startOp));

        IrLabel loopCondLabel = ctx.NewLabel("for_loop_cond");
        IrLabel loopBodyLabel = ctx.NewLabel("for_loop_body");
        IrLabel loopEndLabel = ctx.NewLabel("for_loop_end");

        // ctx.EmitJump(loopCondLabel);

        // condition
        ctx.EmitLabel(loopCondLabel);
        IrTemp condTemp = ctx.Function.NewTemp(IrType.Int);
        ctx.CurrentBlock.Instrs.Add(new IrInstr("leq", condTemp, loopVar, endOp));
        ctx.CurrentBlock.Instrs.Add(new IrInstr("cjump", null, new IrConstInt(1), condTemp, loopEndLabel));

        // body
        ctx.EmitLabel(loopBodyLabel);
        bool bodyTerminated = LowerScope(stmtForLoop.Scope, ctx);
        if (!bodyTerminated)
        {
            // increment
            IrTemp oneTemp = ctx.Function.NewTemp(IrType.Int);
            ctx.CurrentBlock.Instrs.Add(new IrInstr("move", oneTemp, new IrConstInt(1)));
            IrTemp incTemp = ctx.Function.NewTemp(IrType.Int);
            ctx.CurrentBlock.Instrs.Add(new IrInstr("add", incTemp, loopVar, oneTemp));
            ctx.CurrentBlock.Instrs.Add(new IrInstr("move", null, loopVar, incTemp));

            ctx.EmitJump(loopCondLabel);
        }

        // end
        ctx.EmitLabel(loopEndLabel);
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

    bool LowerIf(NodeStmtIf stmtIf, LowerFuncContext ctx)
    {
        IrLabel elseLabel = ctx.NewLabel("if_else");
        IrLabel endLabel = ctx.NewLabel("if_end");

        IrOperand cond = LowerExpr(stmtIf.Condition, ctx);
        ctx.EmitJumpIfFalse(cond, elseLabel);

        IrLabel thenLabel = ctx.NewLabel("if_then");
        ctx.EmitLabel(thenLabel);
        ctx.CurrentBlock = ctx.Function.NewBlock("if_then_block");

        bool thenTerminated = LowerScope(stmtIf.Scope, ctx);
        if (!thenTerminated)
            ctx.EmitJump(endLabel);

        ctx.EmitLabel(elseLabel);
        ctx.CurrentBlock = ctx.Function.NewBlock("if_else_block");

        bool elseTerminated = false;
        if (stmtIf.Pred != null)
        {
            elseTerminated = LowerIfPred(stmtIf.Pred, ctx);
        }
        else
        {
            ctx.EmitJump(endLabel);
        }

        ctx.EmitLabel(endLabel);
        ctx.CurrentBlock = ctx.Function.NewBlock("after_if");
        return thenTerminated && elseTerminated;
    }


    bool LowerIfPred(NodeIfPred pred, LowerFuncContext ctx)
    {
        if (pred is NodeIfPredElse e)
            return LowerScope(e.Scope, ctx);

        if (pred is NodeIfPredElseIf ei)
        {
            bool thenTerminated = LowerScope(ei.Scope, ctx);

            bool elseTerminated = false;
            if (ei.Pred != null)
                elseTerminated = LowerIfPred(ei.Pred, ctx);

            return thenTerminated && elseTerminated;
        }

        return false;
    }

    IrOperand LowerExpr(NodeExpr expr, LowerFuncContext ctx)
    {
        if (expr.GetTrueType(out NodeTerm term))
        {
            return LowerTerm(term, ctx);
        }

        switch (expr.expr)
        {
            case NodeExprBinary eb:
                // left-to-right: lower left, then right, then emit op
                IrOperand leftOp = LowerExpr(eb.Lhs, ctx);
                IrOperand rightOp = LowerExpr(eb.Rhs, ctx);
                // coerces to temps if necessary
                // IrOperand leftTemp = EnsureTemp(leftOp, ctx, IrType.Int);
                // IrOperand rightTemp = EnsureTemp(rightOp, ctx, IrType.Int);
                IrTemp outTemp = ctx.Function.NewTemp(IrType.Int);
                ctx.CurrentBlock.Instrs.Add(new IrInstr(OperatorToOp(eb.Operator.Val), outTemp, leftOp, rightOp));
                return outTemp;

            case NodeExprCall call:
                // evaluate args left-to-right into temps
                var argTemps = new List<IrOperand>();
                if (call.Args != null)
                {
                    foreach (NodeExpr a in call.Args)
                    {
                        IrOperand v = LowerExpr(a, ctx);
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
            
            case NodeExprAssign assign:
                if (!ctx.Locals.TryGetValue(assign.varName, out IrLocal local))
                    throw new Exception($"Undefined local/var '{assign.varName}' in function {ctx.Function.Name}");

                IrOperand exprOp = LowerExpr(assign.value, ctx);
                // ctx.CurrentBlock.Instrs.Add(new IrInstr("move", null, local, exprOp));
                return exprOp;
            
            case NodeExprArrayAccess arrayAccess:
                IrType baseType = IrType.Int;
                IrOperand arrayOp = LowerExpr(arrayAccess.array, ctx);
                IrOperand indexOp = LowerExpr(arrayAccess.index, ctx);
                if (arrayOp is IrTemp temp)
                {
                    baseType = temp.Type.RefType;
                }
                IrTemp resultTemp = ctx.Function.NewTemp(baseType);
                ctx.CurrentBlock.Instrs.Add(new IrInstr("load", resultTemp, arrayOp, indexOp));
                return resultTemp;


        }
        throw new NotSupportedException($"Expr node type not supported: {expr.GetType().Name}");
    }

    private IrType ArgTypeForOperand(IrOperand v)
    {
        if (v is IrConstInt) return IrType.Int;
        if (v is IrLocal l) return l.Type;
        if (v is IrTemp t) return t.Type;
        throw new NotSupportedException($"Cannot determine argument type for operand: {v.Dump()}");
    }

    private string OperatorToOp(OperatorVal val)
    {
        return val switch
        {
            OperatorVal.ADD => "add",
            OperatorVal.SUB => "sub",
            OperatorVal.MULT => "mult",
            OperatorVal.DIV => "div",
            OperatorVal.EQ => "eq",
            OperatorVal.NEQ => "neq",
            OperatorVal.LT => "lt",
            OperatorVal.LEQ => "leq",
            OperatorVal.GT => "gt",
            OperatorVal.GEQ => "geq",
            _ => throw new NotSupportedException($"Operator not supported: {val}"),
        };
    }

    private IrOperand EnsureTemp(IrOperand leftOp, LowerFuncContext ctx, IrType @int)
    {
        if (leftOp is IrTemp)
            return leftOp;
        IrTemp t = ctx.Function.NewTemp(@int);
        ctx.CurrentBlock.Instrs.Add(new IrInstr("move", t, leftOp));
        return t;
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
        else if (term.GetTrueType(out NodeTermStringlit strlit))
        {
            IrConstStr constStr = ctx.Module.InternString(strlit.value);
            IrTemp t = ctx.Function.NewTemp(IrType.String);
            ctx.CurrentBlock.Instrs.Add(new IrInstr("addr_of", t, new IrSymbol(constStr.Label)));
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
    public int LabelCounter = 0;

    public LowerFuncContext(IrModule module, IrFunction function, Dictionary<string, IrFunction> globalFunctions)
    {
        Module = module; Function = function; GlobalFunctions = globalFunctions;
    }

    public IrLabel NewLabel(string name)
    {
        return new IrLabel(name + "_" + LabelCounter++);
    }
    public void EmitLabel(IrLabel label)
    {
        CurrentBlock.Instrs.Add(new IrInstr("label", null, label));
    }
    public void EmitJump(IrLabel label)
    {
        CurrentBlock.Instrs.Add(new IrInstr("jump", null, label));
    }
    public void EmitJumpIfFalse(IrOperand cond, IrLabel label)
    {
        CurrentBlock.Instrs.Add(new IrInstr("cjump", null, new IrConstInt(1), cond, label));
    }
    public void EmitBranch(IrOperand cond, IrLabel thenLabel, IrLabel elseLabel)
    {
        CurrentBlock.Instrs.Add(new IrInstr("branch", null, cond, thenLabel, elseLabel));
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
                sb.AppendLine();
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}