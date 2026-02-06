
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Common;
using TangoFlexCompiler.debugConsole;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.exprs;
using TangoFlexCompiler.Parser.Nodes.Externals;
using TangoFlexCompiler.Parser.Nodes.Intrinsics;
using TangoFlexCompiler.Parser.Nodes.NodeData;
using TangoFlexCompiler.Parser.Nodes.Stmts;
using TangoFlexCompiler.Parser.Nodes.Terms;
using TangoFlexCompiler.ProblemLogging;

namespace TangoFlexCompiler.IRParser
{
    public class TangoFlexIRParser
    {
        public bool debug = true;
        public string debugFile = Path.GetFullPath(Path.Combine("debug", "ir.txt"));


        public NodeProg AST;
        public Arguments args;

        public IrModule Module;
        private readonly Dictionary<string, IrFunction> functions = new Dictionary<string, IrFunction>();
        private readonly Dictionary<string, IrStruct> structs = new Dictionary<string, IrStruct>();
        private readonly Dictionary<IrStruct, StructLayout> layouts = new Dictionary<IrStruct, StructLayout>();

        public TangoFlexIRParser(Arguments arguments)
        {
            args = arguments;
            Module = new IrModule();

            if (arguments.UseFatStrings)
            {
                IrStruct stringStruct = new IrStruct("string", new NodeVisibility() { Visibility = KeywordVal.PUBLIC });
                stringStruct.Fields.Add(new IrStructField("length", IrType.Int, new NodeVisibility() { Visibility = KeywordVal.PUBLIC }, 0));
                stringStruct.Fields.Add(new IrStructField("value", IrType.BytePtr, new NodeVisibility() { Visibility = KeywordVal.PUBLIC }, 1));
                structs["string"] = stringStruct;
                Module.AddStruct(stringStruct);
            }
        }

        public void Process(NodeProg ast)
        {
            AST = ast;

            NodeStmt[] stmts = ast.Stmts.ToArray();


            FindSymbols(stmts);

            IrLowerLayout();

            GenerateIR(ast.Stmts.ToArray());

            if (debug && args.debug && args.WriteDebugFiles)
            {
                File.WriteAllText(debugFile, "");
                string dump = IrDumper.Dump(Module);
                File.WriteAllText(debugFile, dump);
            }
        }

        private void FindSymbols(NodeStmt[] stmts)
        {
            foreach (NodeStmt stmt in stmts)
            {
                if (stmt.GetTrueType(out NodeStmtFuncDecl funcDecl))
                {
                    DebugConsole.WriteLine($"Found {funcDecl.FuncName}");
                    IrType retType = MapType(funcDecl.ReturnType);
                    // var paramList = funcDecl.parameters.Select(p => (p.name, MapType(p.Item2))).ToArray();
                    IrFunction f = new IrFunction(funcDecl.FuncName, retType);
                    Module.AddFunction(f);
                    functions[funcDecl.FuncName] = f;
                }
                else if (stmt.GetTrueType(out NodeStmtExternal stmtExternal))
                {
                    IrType retType;
                    if (stmtExternal.GetTrueType(out ExternalFunc func))
                    {
                        retType = MapType(func.ReturnType);
                        IrFunction f = new IrFunction(func.FuncName, retType);
                        Module.AddExternalFunction(f);
                        functions[func.FuncName] = f;
                    }
                    else if (stmtExternal.GetTrueType(out ExternalVar var))
                    {
                        retType = MapType(var.Type);
                        IrLocal global = new IrLocal(var.VarName, retType);
                        Module.Globals.Add(global);
                    }
                }
                else if (stmt.GetTrueType(out NodeStmtStructDecl stmtStructDecl))
                {
                    IrStruct irStruct = new IrStruct(
                        stmtStructDecl.StructName,
                        stmtStructDecl.NodeVisibility
                    );

                    int index = 0;
                    foreach (NodeStructField field in stmtStructDecl.Fields)
                    {
                        IrType fieldType = MapType(field.FieldType);

                        irStruct.Fields.Add(new IrStructField(
                            field.FieldName,
                            fieldType,
                            field.NodeVisibility,
                            index++
                        ));
                    }

                    structs[stmtStructDecl.StructName] = irStruct;
                    Module.AddStruct(irStruct);
                }
                else if (stmt.GetTrueType(out NodeStmtNamespace nodeNS))
                {
                    FindSymbols(nodeNS.Scope.Stmts.ToArray());
                }
            }
        }

        private void IrLowerLayout()
        {
            foreach (IrStruct Struct in Module.Structs)
            {
                StructLayout layout = new StructLayout();

                int offset = 0;
                int structAlignment = Struct.IsPacked ? 1 : 0;

                layout.FieldOffsets = new List<int>();

                for (int i = 0; i < Struct.Fields.Count; i++)
                {
                    IrStructField field = Struct.Fields[i];

                    int fieldAlignment = Struct.IsPacked ? 1 : field.Type.Alignment;

                    if (!Struct.IsPacked)
                    {
                        offset = AlignUp(offset, fieldAlignment);
                    }

                    layout.FieldOffsets.Insert(i, offset);

                    offset += field.Type.SizeInBytes;

                    if (!Struct.IsPacked)
                    {
                        structAlignment = Math.Max(structAlignment, fieldAlignment);
                    }
                }

                layout.Alignment = structAlignment == 0 ? 1 : structAlignment;
                layout.Size = Struct.IsPacked
                    ? offset
                    : AlignUp(offset, layout.Alignment);

                Struct.Layout = layout;
            }
        }


        static int AlignUp(int offset, int alignment)
        {
            if (alignment <= 0)
                throw new ArgumentOutOfRangeException(nameof(alignment));

            int remainder = offset % alignment;
            if (remainder == 0)
                return offset;

            return offset + (alignment - remainder);
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
            IrFunction function = functions[stmtFuncDecl.FuncName];

            IrBlock entry = function.NewBlock("entry");

            if (stmtFuncDecl.Scope == null)
            {

            }

            LowerFuncContext ctx = new LowerFuncContext(Module, function, functions);

            ctx.CurrentBlock = entry;

            if (stmtFuncDecl.Parameters.Count > 0)
            {
                foreach (FuncArguments param in stmtFuncDecl.Parameters)
                {
                    IrType type = MapType(param.Type);
                    IrLocal paramLocal = function.NewLocal(param.Name, type);
                    function.Parameters.Add(paramLocal);
                    ctx.Locals[param.Name] = paramLocal;
                }
            }

            bool terminated = LowerScope(stmtFuncDecl.Scope, ctx);

            IrBlock lastBlock = function.Blocks.Last();
            if (!terminated && (!lastBlock.Instructions.Any() || !IsTerminator(lastBlock.Instructions.Last())))
            {
                if (!function.ReturnType.Equals(IrType.Void))
                {
                    ProblemLog.LogProblem(State.Warning, $"Function {ctx.Function.Name} did not return");
                }
                if (function.ReturnType == IrType.Int)
                {
                    lastBlock.Instructions.Add(new IrInstr("ret", null, new IrConstInt(0, function.ReturnType)));
                }
                else
                {
                    lastBlock.Instructions.Add(new IrInstr("ret"));
                }
            }
        }

        private static bool IsTerminator(IrInstr instr)
        {
            if (instr == null) return false;
            return instr.Instructions is "ret" or "jump" or "cjump";
        }

        bool LowerScope(NodeStmtScope scope, LowerFuncContext ctx)
        {
            foreach (NodeStmt s in scope.Stmts)
            {
                if (s.GetTrueType(out NodeStmtReturn ret))
                {
                    LowerReturn(ret, ctx);
                    return true;
                }
                else if (s.GetTrueType(out NodeStmtVarDecl decl))
                {
                    LowerVarDecl(decl, ctx);
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
                else if (s.GetTrueType(out NodeStmtCallFunction callFunction))
                {
                    LowerCallFunction(callFunction, ctx);
                }
                else if (s.GetTrueType(out NodeStmtExpr stmtExpr))
                {
                    LowerExpr(stmtExpr.Expr, ctx);
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

        private void LowerCallFunction(NodeStmtCallFunction callFunction, LowerFuncContext ctx)
        {
            List<IrOperand> parameters = new List<IrOperand>();
            foreach (var expr in callFunction.FuncArguments)
            {
                IrOperand operand = LowerExpr(expr, ctx);
                parameters.Add(operand);
            }

            IrTemp returnTemp = ctx.Function.NewTemp(IrType.Int);
            IrInstr instr = new IrInstr("call", returnTemp, new IrSymbol(callFunction.FunctionName));
            foreach (var operand in parameters)
            {
                instr.Operands.Add(operand);
            }
            ctx.CurrentBlock.Instructions.Add(instr);
        }

        private void LowerVarReassign(NodeStmtVarReassign varReassign, LowerFuncContext ctx)
        {
            if (!ctx.Locals.TryGetValue(varReassign.Name, out IrLocal local))
                throw new Exception($"Undefined local/var '{varReassign.Name}' in function {ctx.Function.Name}");

            IrOperand exprOp = LowerExpr(varReassign.Expr, ctx);
            if (exprOp.GetOperand(out IrConstInt i))
            {
                exprOp = new IrConstInt(i.Value, local.Type);
            }
            ctx.CurrentBlock.Instructions.Add(new IrInstr("move", null, local, exprOp));
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

            ctx.CurrentBlock.Instructions.Add(new IrInstr("cjump", null, new IrConstInt(1, IrType.Byte), condOp, loopEndLabel));

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

            if (startOp.GetOperand(out IrConstInt i))
            {
                startOp = new IrConstInt(i.Value, loopVar.Type);
            }
            if (endOp.GetOperand(out IrConstInt i2))
            {
                endOp = new IrConstInt(i2.Value, loopVar.Type);
            }

            ctx.CurrentBlock.Instructions.Add(new IrInstr("move", null, loopVar, startOp));

            IrLabel loopCondLabel = ctx.NewLabel("for_loop_cond");
            IrLabel loopBodyLabel = ctx.NewLabel("for_loop_body");
            IrLabel loopEndLabel = ctx.NewLabel("for_loop_end");

            // ctx.EmitJump(loopCondLabel);

            // condition
            ctx.EmitLabel(loopCondLabel);
            IrTemp condTemp = ctx.Function.NewTemp(IrType.Int);
            ctx.CurrentBlock.Instructions.Add(new IrInstr("leq", condTemp, loopVar, endOp));
            ctx.CurrentBlock.Instructions.Add(new IrInstr("cjump", null, new IrConstInt(1, IrType.Byte), condTemp, loopEndLabel));

            // body
            ctx.EmitLabel(loopBodyLabel);
            bool bodyTerminated = LowerScope(stmtForLoop.Scope, ctx);
            if (!bodyTerminated)
            {
                // increment
                IrTemp oneTemp = ctx.Function.NewTemp(IrType.Int);
                ctx.CurrentBlock.Instructions.Add(new IrInstr("move", oneTemp, new IrConstInt(1, IrType.Byte)));
                IrTemp incTemp = ctx.Function.NewTemp(IrType.Int);
                ctx.CurrentBlock.Instructions.Add(new IrInstr("add", incTemp, loopVar, oneTemp));
                ctx.CurrentBlock.Instructions.Add(new IrInstr("move", null, loopVar, incTemp));

                ctx.EmitJump(loopCondLabel);
            }

            // end
            ctx.EmitLabel(loopEndLabel);
        }

        private void LowerReturn(NodeStmtReturn ret, LowerFuncContext ctx)
        {
            if (ret.Expr == null)
            {
                ctx.CurrentBlock.Instructions.Add(new IrInstr("ret"));
                return;
            }
            IrOperand v = LowerExpr(ret.Expr, ctx);
            if (v.GetOperand(out IrConstInt i))
            {
                v = new IrConstInt(i.Value, ctx.Function.ReturnType);
            }
            ctx.CurrentBlock.Instructions.Add(new IrInstr("ret", null, v));
        }

        void LowerVarDecl(NodeStmtVarDecl decl, LowerFuncContext ctx)
        {
            string name = decl.Name;
            IrType type = MapType(decl.Type);
            IrLocal local = ctx.Function.NewLocal(name, type);
            ctx.Locals[name] = local;


            if (decl.Expr != null)
            {
                IrOperand operand = LowerExpr(decl.Expr, ctx);
                if (operand.GetOperand(out IrConstInt i))
                {
                    operand = new IrConstInt(i.Value, type);
                }
                ctx.CurrentBlock.Instructions.Add(new IrInstr("move", null, local, operand));
                local.HasInit = true;
            }
            else
            {
                if (type == IrType.Int)
                {
                    local.HasInit = true;
                    ctx.CurrentBlock.Instructions.Add(new IrInstr("move", null, local, new IrConstInt(0, IrType.Byte)));
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

            switch (expr.Expr)
            {
                case NodeExprBinary eb:
                    {
                        // left-to-right: lower left, then right, then emit op
                        IrOperand leftOp = LowerExpr(eb.Lhs, ctx);
                        IrOperand rightOp = LowerExpr(eb.Rhs, ctx);
                        if (rightOp.GetOperand(out IrConstInt i))
                        {
                            IrType tempType = leftOp switch
                            {
                                IrConstInt i2 => i2.Type,
                                IrLocal l => l.Type,
                                IrTemp t => t.Type,
                                _ => throw new NotImplementedException()
                            };
                            rightOp = new IrConstInt(i.Value, tempType);
                        }
                        // coerces to temps if necessary
                        // IrOperand leftTemp = EnsureTemp(leftOp, ctx, IrType.Int);
                        // IrOperand rightTemp = EnsureTemp(rightOp, ctx, IrType.Int);

                        IrTemp outTemp = ctx.Function.NewTemp(IrType.Int);
                        ctx.CurrentBlock.Instructions.Add(new IrInstr(OperatorToOp(eb.Operator.Val), outTemp, leftOp, rightOp));
                        return outTemp;
                    }

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
                    var callInstr = new IrInstr($"call", retTemp, new IrSymbol(call.FuncName));
                    foreach (IrOperand a in argTemps) callInstr.Operands.Add(a);
                    ctx.CurrentBlock.Instructions.Add(callInstr);
                    return retTemp ?? (IrOperand)new IrConstInt(0, retTemp.Type);

                case NodeExprAssign assign:
                    if (!ctx.Locals.TryGetValue(assign.VarName, out IrLocal local))
                        throw new Exception($"Undefined local/var '{assign.VarName}' in function {ctx.Function.Name}");

                    IrOperand exprOp = LowerExpr(assign.Value, ctx);
                    // ctx.CurrentBlock.Instructions.Add(new IrInstr("move", null, local, exprOp));
                    return exprOp;

                case NodeExprArrayAccess arrayAccess:
                    IrType baseType = IrType.Int;
                    IrOperand arrayOp = LowerExpr(arrayAccess.Array, ctx);
                    IrOperand indexOp = LowerExpr(arrayAccess.Index, ctx);
                    if (arrayOp is IrTemp temp)
                    {
                        baseType = temp.Type.RefType;
                    }
                    IrTemp resultTemp = ctx.Function.NewTemp(baseType);
                    ctx.CurrentBlock.Instructions.Add(new IrInstr("load", resultTemp, arrayOp, indexOp));
                    return resultTemp;

                case NodeExprUnary unary:
                    IrOperand operand = LowerExpr(unary.Operand, ctx);
                    IrTemp t2 = null;
                    if (unary.Operator.Val == OperatorVal.BITAND)
                    {
                        IrInstr instr = ctx.CurrentBlock.Instructions.Last();
                        if (instr.Operands[0].GetOperand(out IrLocal localVar))
                        {
                            ctx.CurrentBlock.Instructions.Remove(instr);
                            operand = localVar;
                        }
                        t2 = ctx.Function.NewTemp(IrType.VoidPtr);
                        ctx.CurrentBlock.Instructions.Add(new IrInstr("addr_of", t2, operand));
                    }
                    else if (unary.Operator.Val == OperatorVal.SUB)
                    {
                        t2 = ctx.Function.NewTemp(IrType.Int);
                        if (operand.GetOperand(out IrConstInt constInt))
                        {
                            ctx.CurrentBlock.Instructions.Add(new IrInstr("move", t2, new IrConstInt(-constInt.Value, constInt.Type)));
                            return t2;
                        }
                        ctx.CurrentBlock.Instructions.Add(new IrInstr("sub", t2, operand, new IrConstInt(0, IrType.Byte)));
                    }
                    return t2;
                case NodeExprFieldAccess fieldAccess:
                    {
                        IrStruct Struct = GetStruct(fieldAccess.FieldExpr, ctx);
                        IrOperand fieldBase = LowerExpr(fieldAccess.BaseExpr, ctx);
                        IrStructField field = GetStructField(fieldAccess.FieldExpr, Struct, ctx);

                        IrTemp baseAddressTemp = ctx.Function.NewTemp(IrType.VoidPtr);
                        IrTemp outTemp = ctx.Function.NewTemp(field.Type);
                        int fieldOffset = Struct.Layout.FieldOffsets[field.Index];
                        if (fieldAccess.FieldAccessKind == FieldAccessKind.Direct)
                        {
                            ctx.CurrentBlock.Instructions.Add(new IrInstr("addr_of", baseAddressTemp, fieldBase));
                            ctx.CurrentBlock.Instructions.Add(new IrInstr("load", outTemp, baseAddressTemp, new IrConstInt(fieldOffset, IrType.Int)));
                        }
                        else
                        {
                            ctx.CurrentBlock.Instructions.Add(new IrInstr("load", outTemp, fieldBase, new IrConstInt(fieldOffset, IrType.Int)));
                        }
                        return outTemp;
                    }
                    throw new NotImplementedException("field access is not implemented yet");

                // t1 = base field: get address
                // t2 = field: get offset in struct
                // t3 = t1 + t2
                // return t3
                case NodeExprCast exprCast:
                    IrOperand exprValue = LowerExpr(exprCast.Expr, ctx);
                    IrTemp castTemp = null;
                    switch (exprCast.CastKind)
                    {
                        case CastKind.Explicit:
                            castTemp = ctx.Function.NewTemp(MapType(exprCast.Type));
                            ctx.CurrentBlock.Instructions.Add(new IrInstr("move", castTemp, exprValue));
                            break;
                    }
                    return castTemp;
                case NodeExprSystemcall exprSystemcall:
                    IrOperand intNumber = LowerExpr(exprSystemcall.IntNumber, ctx);

                    argTemps = new List<IrOperand>();
                    if (exprSystemcall.Args != null)
                    {
                        foreach (NodeExpr a in exprSystemcall.Args)
                        {
                            IrOperand v = LowerExpr(a, ctx);
                            argTemps.Add(EnsureTemp(v, ctx, ArgTypeForOperand(v)));
                        }
                    }
                    resultTemp = ctx.Function.NewTemp(IrType.Int);
                    IrInstr sysCallInstr = new IrInstr("sysCall", resultTemp, intNumber);
                    foreach (IrOperand a in argTemps) sysCallInstr.Operands.Add(a);
                    ctx.CurrentBlock.Instructions.Add(sysCallInstr);
                    return resultTemp;
                case NodeExprIntrinsic exprIntrinsic:
                    {
                        return LowerExpr(exprIntrinsic.Intrinsic, ctx);
                    }

            }
            throw new NotSupportedException($"Expr node type not supported: {expr.Expr.GetType().Name}");
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
            ctx.CurrentBlock.Instructions.Add(new IrInstr("move", t, leftOp));
            return t;
        }

        private IrStructField GetStructField(NodeExpr expr, IrStruct Struct, LowerFuncContext ctx)
        {
            if (expr.GetTrueType(out NodeTerm term))
            {
                if (term.GetTrueType(out NodeTermVar var))
                {
                    int fieldIndex = Struct.Fields.FindIndex((field) => field.Name == var.Name);
                    if (fieldIndex == -1)
                    {
                        throw new Exception($"Undefined struct field '{var.Name}' in struct {Struct.Name}");
                    }
                    return Struct.Fields[fieldIndex];
                }
            }
            throw new Exception($"Undefined field in struct {Struct.Name}");
        }

        private IrStruct GetStruct(NodeExpr expr, LowerFuncContext ctx)
        {
            if (expr.GetTrueType(out NodeTerm term))
            {
                if (term.GetTrueType(out NodeTermVar var))
                {
                    int structIndex = Module.Structs.FindIndex((s) => s.Fields.Any((f) => f.Name == var.Name));
                    if (structIndex != -1)
                    {
                        IrStruct _struct = Module.Structs[structIndex];
                        return _struct;
                    }
                }
                else
                {
                    throw new Exception($"Undefined struct");
                }
            }
            throw new Exception($"Undefined struct");
        }
        private IrStruct GetStruct(string name, LowerFuncContext ctx)
        {
            int structIndex = Module.Structs.FindIndex((s) => s.Name == name);
            if (structIndex != -1)
            {
                IrStruct _struct = Module.Structs[structIndex];
                return _struct;
            }
            throw new Exception($"Undefined struct");
        }

        private IrOperand LowerTerm(NodeTerm term, LowerFuncContext ctx)
        {
            // Console.WriteLine($"term is {term.term}");
            if (term.GetTrueType(out NodeTermIntlit intlit))
            {
                if (args.Bits == 32)
                {
                    return new IrConstInt(intlit.Value, IrType.Int);
                }
                else if (args.Bits == 64)
                {
                    return new IrConstInt(intlit.Value, IrType.ULong);
                }
                return new IrConstInt(intlit.Value, IrType.UShort);
            }
            else if (term.GetTrueType(out NodeTermVar var))
            {
                if (!ctx.Locals.TryGetValue(var.Name, out IrLocal local))
                {
                    throw new Exception($"Undefined local/var '{var.Name}' in function {ctx.Function.Name}");
                }


                // check if last instr was the same
                if (ctx.CurrentBlock.Instructions.Count > 0)
                {
                    IrInstr lastInstr = ctx.CurrentBlock.Instructions.Last();
                    if (lastInstr.Instructions == "move" && lastInstr.Result != null)
                    {
                        if (lastInstr.Operands[0] == local)
                        {
                            return lastInstr.Result;
                        }
                    }
                }

                IrTemp t = ctx.Function.NewTemp(local.Type);
                ctx.CurrentBlock.Instructions.Add(new IrInstr("move", t, local));
                return t;
            }
            else if (term.GetTrueType(out NodeTermStringlit strlit))
            {
                string str = strlit.Value;
                str = str.Trim();
                IrConstStr constStr = ctx.Module.InternString(str);
                IrTemp t = ctx.Function.NewTemp(IrType.String);
                ctx.CurrentBlock.Instructions.Add(new IrInstr("addr_of", t, new IrSymbol(constStr.Label)));
                return t;
            }
            else
            {
                Console.WriteLine($"term is {term.Term}");
            }

            throw new NotSupportedException($"Term node unsupported: {term.Term.GetType().Name}");
        }

        void generateStmt(NodeStmt stmt)
        {
            if (stmt.GetTrueType(out NodeStmtFuncDecl stmtFuncDecl))
            {
                LowerFunction(stmtFuncDecl);
            }
            else if (stmt.GetTrueType(out NodeStmtNamespace nodeNS))
            {
                GenerateIR(nodeNS.Scope.Stmts.ToArray());
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
            CurrentBlock.Instructions.Add(new IrInstr("label", null, label));
        }
        public void EmitJump(IrLabel label)
        {
            CurrentBlock.Instructions.Add(new IrInstr("jump", null, label));
        }
        public void EmitJumpIfFalse(IrOperand cond, IrLabel label)
        {
            CurrentBlock.Instructions.Add(new IrInstr("cjump", null, new IrConstInt(1, IrType.Byte), cond, label));
        }
        public void EmitBranch(IrOperand cond, IrLabel thenLabel, IrLabel elseLabel)
        {
            CurrentBlock.Instructions.Add(new IrInstr("branch", null, cond, thenLabel, elseLabel));
        }
    }
}