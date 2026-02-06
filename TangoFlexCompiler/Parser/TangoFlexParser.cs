using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using TangoFlexCompiler.Lexer;
using Microsoft.Win32.SafeHandles;
using TangoFlexCompiler.ProblemLogging;
using TangoFlexCompiler.Parser.Nodes;
using TangoFlexCompiler.Parser.Nodes.Terms;
using TangoFlexCompiler.Parser.Nodes.NodeData;
using TangoFlexCompiler.Parser.Nodes.Intrinsics;
using TangoFlexCompiler.Parser.Nodes.exprs;
using TangoFlexCompiler.Parser.Nodes.Stmts;
using TangoFlexCompiler.Parser.Nodes.Externals;
using Common;

namespace TangoFlexCompiler.Parser
{
    public class TangoFlexParser
    {
        public bool debug = true;
        public string debugFile = Path.GetFullPath(Path.Combine("debug", "parser.json"));
        Token[] Tokens;
        Arguments args;
        int TokensIndex { get; set; }
        public NodeProg AST;

        public TangoFlexParser(Arguments arguments)
        {
            args = arguments;
        }

        public void Process(Token[] tokens)
        {
            Tokens = tokens;
            ParseProgNode();
        }

        string evaluateString(TokenString token)
        {
            string str = token.Val;
            List<byte> result = new List<byte>();

            for (int i = 0; i < str.Length; i++)
            {
                long number = 0;
                switch (token.Prefix)
                {
                    case TokenString.StringPrefix.U8:
                        number = (byte)str[i];
                        if (number > 0xFF)
                            throw new Exception("Character out of range for u8 string");
                        result.Add((byte)number);
                        break;
                    case TokenString.StringPrefix.U16:
                        result.AddRange(BitConverter.GetBytes((ushort)str[i]));
                        break;
                    case TokenString.StringPrefix.U32:
                        result.AddRange(BitConverter.GetBytes((uint)str[i]));
                        break;
                }
            }
            if (CompilerProgram.Arguments.UseFatStrings == false)
            {
                // any strings are then cstrings only
                result.Add(0);
            }
            return string.Join(",", result.Select(b => $"0x{b:X2}"));
        }

        NodeTerm parseTerm()
        {
            NodeTerm nodeTerm = new NodeTerm();

            Token token = Peek();

            // Console.WriteLine($"[parseTerm] Kind = {token.Kind}");
            switch (token.Kind)
            {
                case TokenKind.INT:
                    TokenInt tokenInt = (TokenInt)Advance();
                    nodeTerm.Term = new NodeTermIntlit() { Value = tokenInt.Val };
                    break;
                case TokenKind.IDENTIFIER:
                    TokenIdentifier tokenIdentifier = (TokenIdentifier)Advance();
                    nodeTerm.Term = new NodeTermVar() { Name = tokenIdentifier.Val };
                    break;
                case TokenKind.STRING:
                    TokenString tokenString = (TokenString)Advance();
                    nodeTerm.Term = new NodeTermStringlit() { Value = evaluateString(tokenString) };
                    break;
            }

            return nodeTerm;
        }

        // from hydrogen-cpp
        // https://github.com/orosmatthew/hydrogen-cpp/blob/master/src/parser.hpp
        NodeExpr parseExpr(int min_precedence = 0)
        {
            if (min_precedence == 0)
            {
                if (Peek().Kind == TokenKind.KEYWORD && PeekKeyword().Val == KeywordVal.CALL)
                {
                    // yes we are calling to get a stmt
                    NodeStmtCallFunction callFunction = parseStmtCallFunction();
                    return new NodeExpr() { Expr = new NodeExprCall() { FuncName = callFunction.FunctionName, Args = callFunction.FuncArguments } };
                }
            }

            if (Peek().Kind == TokenKind.OPERATOR)
            {
                if (PeekOperator().IsUnary())
                {
                    NodeExprUnary nodeExprUnary = new NodeExprUnary();
                    nodeExprUnary.SourceSpan = Peek().SourceSpan;
                    nodeExprUnary.Operator = parseOperator();
                    nodeExprUnary.Operand = parseExpr();
                    return new NodeExpr() { Expr = nodeExprUnary };
                }
            }

            NodeExpr exprLhs = null;
            SourceSpan sourceSpan = Peek().SourceSpan;
            if (Peek().Kind == TokenKind.OPERATOR)
            {
                if (PeekOperator().Val == OperatorVal.INTRINSICS)
                {
                    exprLhs = new NodeExpr() { Expr = parseExprIntrinsics() };
                }
            }
            else
            {

                NodeTerm? term_lhs = parseTerm();
                if (term_lhs == null || term_lhs.Term == null)
                {
                    return null;
                }
                exprLhs = new NodeExpr() { Expr = term_lhs };
            }
            exprLhs.SourceSpan = sourceSpan;

            while (true)
            {
                Token? currToken = Peek();
                if (currToken == null || currToken.Kind != TokenKind.OPERATOR)
                {
                    break;
                }

                TokenOperator op = (TokenOperator)currToken;
                NodeExpr exprRhs;
                if (op.IsCompound())
                {
                    Advance();
                    exprRhs = parseExpr();

                    NodeExprBinary binaryOp = new NodeExprBinary()
                    {
                        Lhs = exprLhs,
                        Operator = new NodeOperator() { Val = op.FromAssign() },
                        Rhs = exprRhs
                    };

                    exprLhs = new NodeExpr
                    {
                        Expr = binaryOp
                    };
                    return exprLhs;
                }
                else if ((op.Val == OperatorVal.PERIOD || op.Val == OperatorVal.RIGHTARROW) && Peek(1).Kind != TokenKind.OPERATOR)
                {
                    FieldAccessKind fieldAccess;
                    NodeExprFieldAccess nodeExprField = new NodeExprFieldAccess();
                    if (op.Val == OperatorVal.PERIOD)
                    {
                        fieldAccess = FieldAccessKind.Direct;
                    }
                    else
                    {
                        fieldAccess = FieldAccessKind.Indirect;
                    }
                    if (exprLhs.GetTrueType(out NodeTerm term))
                    {
                        if (term.GetTrueType(out NodeTermVar var))
                        {
                            nodeExprField.StructName = var.Name;
                        }
                        else
                        {
                            ThrowError($"Can't access field {term.Term.GetType()}");
                        }
                    }
                    else
                    {
                        ThrowError($"Can't access {exprLhs.Expr.GetType()}");
                    }
                    Advance();
                    NodeExpr field = parseExpr();
                    nodeExprField.BaseExpr = exprLhs;
                    nodeExprField.FieldExpr = field;
                    nodeExprField.FieldAccessKind = fieldAccess;
                    return new NodeExpr() { Expr = nodeExprField };
                }
                if (op.Val == OperatorVal.LBRACKET)
                {
                    Advance();
                    NodeExpr indexExpr = parseExpr();
                    ExpectOperator(OperatorVal.RBRACKET, "Expected ] after array index", out _);
                    return new NodeExpr()
                    {
                        Expr = new NodeExprArrayAccess()
                        {
                            Array = exprLhs,
                            Index = indexExpr
                        }
                    };
                }

                int? precedence = ((TokenOperator)currToken).binPrecedence();
                if (precedence == null || precedence < min_precedence)
                {
                    break;
                }

                // Expr is binary operator
                Advance();
                int nextMinPrecedence = precedence.Value + 1;
                exprRhs = parseExpr(nextMinPrecedence);
                if (exprRhs == null)
                {
                    ThrowError("Expected right hand side expression");
                }

                NodeExprBinary expr = new NodeExprBinary()
                {
                    Lhs = exprLhs,
                    Operator = new NodeOperator() { Val = op.Val },
                    Rhs = exprRhs
                };
                exprLhs = new NodeExpr
                {
                    Expr = expr
                };
            }

            return exprLhs;
        }

        NodeExprIntrinsic parseExprIntrinsics()
        {
            NodeExprIntrinsic exprIntrinsics = new NodeExprIntrinsic();

            ExpectOperator(OperatorVal.INTRINSICS, "expected '@'", out _);
            Expect(TokenKind.IDENTIFIER, "expected identifier", out TokenIdentifier ident);

            if (ident == null)
            {
                Console.WriteLine("this should not throw if it dose something 100% wrong");
                Console.WriteLine("make an issue on github and something will happen");
                ThrowError("what?");
                // this should not throw if it dose
                // something 100% wrong
            }
            exprIntrinsics.Instruction = ident.Val;

            if (ident.Val.Equals("cast"))
            {
                exprIntrinsics.Intrinsic = new NodeExpr() { Expr = parseCast() };
            }
            else if (ident.Val.Equals("systemcall"))
            {
                exprIntrinsics.Intrinsic = new NodeExpr() { Expr = parseSystemcall() };
            }

            return exprIntrinsics;
        }

        NodeExprSystemcall parseSystemcall()
        {
            NodeExprSystemcall exprSystemcall = new NodeExprSystemcall();
            ExpectOperator(OperatorVal.LEFTPAREN, "", out _);
            NodeExpr intNumber = parseExpr();
            ExpectOperator(OperatorVal.COMMA, "", out _);

            List<NodeExpr> parameters = new List<NodeExpr>();
            while (true)
            {
                parameters.Add(parseExpr());
                // Console.WriteLine($"right now {Peek()}");
                if (Peek().Kind == TokenKind.OPERATOR && PeekOperator().Val != OperatorVal.COMMA)
                {
                    // Console.WriteLine($"exiting args {Peek()}");
                    break;
                }
                ExpectOperator(OperatorVal.COMMA, "", out _);
            }

            ExpectOperator(OperatorVal.RIGHTPAREN, "", out _);

            exprSystemcall.IntNumber = intNumber;
            exprSystemcall.Args = parameters.ToArray();
            return exprSystemcall;
        }

        NodeExprCast parseCast()
        {
            NodeExprCast nodeExprCast = new NodeExprCast();
            ExpectOperator(OperatorVal.LEFTPAREN, "", out _);
            NodeType castType = parseType();
            ExpectOperator(OperatorVal.COMMA, "", out _);
            NodeExpr expr = parseExpr();
            ExpectOperator(OperatorVal.RIGHTPAREN, "", out _);
            nodeExprCast.Type = castType;
            nodeExprCast.Expr = expr;
            nodeExprCast.CastKind = CastKind.Explicit;
            return nodeExprCast;
        }

        NodeOperator parseOperator()
        {
            TokenOperator tokenOperator = Expect<TokenOperator>(TokenKind.OPERATOR, "Expected Operator");

            NodeOperator nodeOperator = new NodeOperator();
            nodeOperator.Val = tokenOperator.Val;

            return nodeOperator;
        }

        NodeType parseType()
        {
            // 1. Parse base type
            TokenKeyword baseToken = Expect<TokenKeyword>(TokenKind.KEYWORD, "Expected Type");

            switch (baseToken.Val)
            {
                case KeywordVal.INT8:
                case KeywordVal.UINT8:
                case KeywordVal.INT16:
                case KeywordVal.UINT16:
                case KeywordVal.INT:
                case KeywordVal.UINT32:
                case KeywordVal.INT64:
                case KeywordVal.UINT64:
                case KeywordVal.FLOAT:
                case KeywordVal.BOOL:
                case KeywordVal.VOID:
                case KeywordVal.STRING:
                    break;

                case KeywordVal.PTR:
                    ThrowError("Type cannot start with ptr");
                    break;
            }

            NodeType type = new NodeType
            {
                Type = baseToken.Val,
                NestedTypes = null
            };

            // 2. Wrap in ptrs (LEFT → RIGHT)
            while (MatchKeyword(KeywordVal.PTR, out _))
            {
                type = new NodeType
                {
                    Type = KeywordVal.PTR,
                    NestedTypes = type
                };
            }

            return type;
        }

        NodeVisibility parseVisibility()
        {
            NodeVisibility visibility = new NodeVisibility();
            TokenKeyword baseToken = PeekKeyword();
            if (baseToken == null || baseToken.Val == KeywordVal.NONE)
            {
                return new NodeVisibility() { Visibility = KeywordVal.INTERNAL };
            }

            switch (baseToken.Val)
            {
                case KeywordVal.PUBLIC:
                case KeywordVal.INTERNAL:
                case KeywordVal.PRIVATE:
                    visibility.Visibility = baseToken.Val;
                    Advance();
                    break;
                default:
                    ThrowError("Expected visibility modifier");
                    break;
            }
            return visibility;
        }

        NodeStmtFuncDecl parseStmtFuncDecl()
        {
            NodeStmtFuncDecl stmtFuncDecl = new NodeStmtFuncDecl();
            List<FuncArguments> Arguments = new List<FuncArguments>();
            ExpectKeyword(KeywordVal.FUNC, "", out TokenKeyword _);

            string cc = "";
            // if this is an ident and next is an operator 
            if (Peek().Kind == TokenKind.IDENTIFIER && Peek(1).Kind != TokenKind.OPERATOR)
            {
                Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier identifier);
                cc = identifier.Val;
            }
            else
            {
                cc = args.CallingConventions.ToString().ToUpper();
            }

            NodeVisibility nodeVisibility = parseVisibility();
            Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier funcName);
            ExpectOperator(OperatorVal.LEFTPAREN, "", out TokenOperator _);
            while (true)
            {
                if (Peek().Kind != TokenKind.IDENTIFIER)
                {
                    break;
                }
                Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier argName);
                ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
                NodeType argType = parseType();


                Arguments.Add(new FuncArguments(argName.Val, argType));

                if (!MatchOperator(OperatorVal.COMMA, out TokenOperator _))
                {
                    // Console.WriteLine(Peek());
                    break;
                }
            }
            ExpectOperator(OperatorVal.RIGHTPAREN, "", out TokenOperator _);
            ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
            NodeType type = parseType();
            NodeStmtScope scope = null;
            if (Peek().Kind == TokenKind.OPERATOR && PeekOperator().Val == OperatorVal.LEFTCURL)
            {
                scope = parseScope();
            }

            stmtFuncDecl.ReturnType = type;
            stmtFuncDecl.CallingConvention = cc;
            stmtFuncDecl.NodeVisibility = nodeVisibility;
            stmtFuncDecl.FuncName = funcName.Val;
            stmtFuncDecl.Scope = scope;
            stmtFuncDecl.Parameters = Arguments;

            return stmtFuncDecl;
        }

        NodeStmtReturn parseStmtReturn()
        {
            NodeStmtReturn nodeStmtReturn = new NodeStmtReturn();
            ExpectKeyword(KeywordVal.RETURN, "", out TokenKeyword _);
            NodeExpr expr = parseExpr();

            nodeStmtReturn.Expr = expr;

            return nodeStmtReturn;
        }

        NodeStmtVarDecl parseStmtVarDecl()
        {
            NodeStmtVarDecl nodeStmtVarDecl = new NodeStmtVarDecl();
            ExpectKeyword(KeywordVal.VAR, "", out _);
            NodeVisibility visibility = parseVisibility();
            Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier varName);
            ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
            NodeType type = parseType();
            NodeExpr? expr = null;
            if (PeekOperator() != null)
            {
                ExpectOperator(OperatorVal.ASSIGN, "", out TokenOperator _);
                expr = parseExpr();
            }
            // Console.WriteLine($"next token = {Peek()}");

            nodeStmtVarDecl.Expr = expr;
            nodeStmtVarDecl.Type = type;
            nodeStmtVarDecl.Name = varName.Val;
            nodeStmtVarDecl.NodeVisibility = visibility;

            return nodeStmtVarDecl;
        }

        NodeStmtIf parseStmtIf()
        {
            NodeStmtIf nodeStmtIf = new NodeStmtIf();
            ExpectKeyword(KeywordVal.IF, "", out TokenKeyword _);
            ExpectOperator(OperatorVal.LEFTPAREN, "", out TokenOperator _);
            NodeExpr condition = parseExpr();
            ExpectOperator(OperatorVal.RIGHTPAREN, "", out TokenOperator _);
            NodeStmtScope ifScope = parseScope();

            nodeStmtIf.Condition = condition;
            nodeStmtIf.Scope = ifScope;
            NodeIfPred? pred = parseIfPred();
            if (pred != null)
            {
                nodeStmtIf.Pred = pred;
            }

            return nodeStmtIf;
        }

        NodeIfPred? parseIfPred()
        {
            if (MatchKeyword(KeywordVal.ELSE, out TokenKeyword _))
            {
                if (MatchKeyword(KeywordVal.IF, out TokenKeyword _))
                {
                    NodeIfPredElseIf elseIfPred = new NodeIfPredElseIf();
                    ExpectOperator(OperatorVal.LEFTPAREN, "", out TokenOperator _);
                    NodeExpr elseIfCondition = parseExpr();
                    ExpectOperator(OperatorVal.RIGHTPAREN, "", out TokenOperator _);
                    NodeStmtScope elseIfScope = parseScope();

                    elseIfPred.Condition = elseIfCondition;
                    elseIfPred.Scope = elseIfScope;

                    NodeIfPred? elsePred = parseIfPred();
                    if (elsePred != null)
                    {
                        elseIfPred.Pred = elsePred;
                    }

                    return elseIfPred;
                }
                else
                {
                    NodeIfPredElse elsePred = new NodeIfPredElse();
                    NodeStmtScope elseScope = parseScope();

                    elsePred.Scope = elseScope;

                    return elsePred;
                }
            }

            return null;
        }

        NodeStmtForLoop parseStmtForLoop()
        {
            NodeStmtForLoop stmtForLoop = new NodeStmtForLoop();
            NodeType? type = null;

            ExpectKeyword(KeywordVal.FOR, "Expected for loop", out _);
            ExpectOperator(OperatorVal.LEFTPAREN, "Expected ( after for", out _);
            if (PeekKeyword().Val == KeywordVal.VAR)
            {
                ExpectKeyword(KeywordVal.VAR, "", out _);
            }
            Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier varName);
            if (PeekOperator().Val == OperatorVal.COLON)
            {
                ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
                type = parseType();
            }
            ExpectOperator(OperatorVal.ASSIGN, "", out TokenOperator _);

            NodeExpr startExpr = parseExpr();
            ExpectOperator(OperatorVal.PERIOD, "Expected .. in for loop", out _);
            ExpectOperator(OperatorVal.PERIOD, "Expected .. in for loop", out _);
            NodeExpr endExpr = parseExpr();
            // optional step
            NodeExpr stepExpr;
            if (MatchKeyword(KeywordVal.STEP, out _))
            {
                stepExpr = parseExpr();
            }
            else
            {
                stepExpr = new NodeExpr()
                {
                    Expr = new NodeTerm()
                    {
                        Term = new NodeTermIntlit()
                        {
                            Value = 1
                        }
                    }
                };
            }
            ExpectOperator(OperatorVal.RIGHTPAREN, "Expected ) after for loop", out _);
            NodeStmtScope scope = parseScope();

            stmtForLoop.Name = varName.Val;
            stmtForLoop.Type = type;
            stmtForLoop.Start = startExpr;
            stmtForLoop.End = endExpr;
            stmtForLoop.Step = stepExpr;
            stmtForLoop.Scope = scope;

            return stmtForLoop;
        }

        private NodeStmtVarReassign parseStmtVarReassign()
        {
            // must be a var reassignment
            NodeStmtVarReassign varReassign = new NodeStmtVarReassign();
            string varName = "";
            TokenOperator assign = (TokenOperator)Peek(1);

            if (assign.IsCompound())
            {
                varReassign.Op = new NodeOperator() { Val = OperatorVal.ASSIGN };
                if (Peek() != null && Peek().Kind == TokenKind.IDENTIFIER)
                {
                    varName = ((TokenIdentifier)Peek()).Val;
                }
            }
            else
            {
                Expect(TokenKind.IDENTIFIER, "Expected identifier for var reassignment", out TokenIdentifier varIdent);
                varName = varIdent.Val;
                ExpectOperator(OperatorVal.ASSIGN, "Expected = for var reassignment", out TokenOperator tokenOperator);
                varReassign.Op = new NodeOperator() { Val = tokenOperator.Val };
            }

            NodeExpr expr = parseExpr();


            varReassign.Name = varName;
            varReassign.Expr = expr;
            return varReassign;
        }

        NodeStmtWhileLoop parseStmtWhileLoop()
        {
            NodeStmtWhileLoop stmtWhileLoop = new NodeStmtWhileLoop();
            ExpectKeyword(KeywordVal.WHILE, "Expected while loop", out _);
            ExpectOperator(OperatorVal.LEFTPAREN, "Expected ( after while", out _);
            NodeExpr condition = parseExpr();
            ExpectOperator(OperatorVal.RIGHTPAREN, "Expected ) after while condition", out _);
            NodeStmtScope scope = parseScope();

            stmtWhileLoop.Condition = condition;
            stmtWhileLoop.Scope = scope;

            return stmtWhileLoop;
        }

        NodeStmtCallFunction parseStmtCallFunction()
        {
            NodeStmtCallFunction nodeStmtCallFunction = new NodeStmtCallFunction();
            // for the expr
            nodeStmtCallFunction.SourceSpan = Peek().SourceSpan;
            ExpectKeyword(KeywordVal.CALL, "Expected Call to call function", out _);
            TokenIdentifier tokenIdentifier = Expect<TokenIdentifier>(TokenKind.IDENTIFIER, "Expected Identifier");
            List<NodeExpr> arguments = new List<NodeExpr>();
            ExpectOperator(OperatorVal.LEFTPAREN, "", out TokenOperator _);
            while (true)
            {
                arguments.Add(parseExpr());

                if (!MatchOperator(OperatorVal.COMMA, out TokenOperator _))
                {
                    // Console.WriteLine(Peek());
                    break;
                }
            }
            ExpectOperator(OperatorVal.RIGHTPAREN, "", out TokenOperator _);
            nodeStmtCallFunction.FuncArguments = arguments.ToArray();
            nodeStmtCallFunction.FunctionName = tokenIdentifier.Val;
            return nodeStmtCallFunction;
        }

        NodeStmtExternal parseStmtExternal()
        {
            NodeStmtExternal nodeStmtExternal = new NodeStmtExternal();
            ExpectKeyword(KeywordVal.EXTERNAL, "", out _);
            TokenString from = null;
            if (Peek().Kind == TokenKind.STRING)
            {
                from = Expect<TokenString>(TokenKind.STRING, "Expected string");
            }
            TokenKeyword type = PeekKeyword();

            IExternal external = null;
            if (type != null)
            {
                if (type.Val == KeywordVal.FUNC)
                {
                    NodeStmtFuncDecl funcDecl = parseStmtFuncDecl();
                    external = new ExternalFunc()
                    {
                        FuncName = funcDecl.FuncName,
                        NodeVisibility = funcDecl.NodeVisibility,
                        CallingConvention = funcDecl.CallingConvention,
                        ReturnType = funcDecl.ReturnType,
                        Parameters = funcDecl.Parameters,
                    };
                }
                else if (type.Val == KeywordVal.VAR)
                {

                }
                else
                {
                    ThrowError("Expected type");
                    return null;
                }
            }
            else
            {
                ThrowError("Expected type");
                return null;
            }

            nodeStmtExternal.From = from?.Val;
            nodeStmtExternal.External = external;
            return nodeStmtExternal;
        }

        NodeStmtNamespace parseStmtNameSpace()
        {
            NodeStmtNamespace nodeStmtNamespace = new NodeStmtNamespace();
            Expect(TokenKind.PREPROCESSOR, "Expected Namespace");
            ExpectIdent("", out TokenIdentifier identifier);

            NodeStmtScope scope = parseScope();
            nodeStmtNamespace.Scope = scope;
            nodeStmtNamespace.Name = identifier.Val;
            return nodeStmtNamespace;
        }

        NodeStmtScope parseScope()
        {
            NodeStmtScope stmtScope = new NodeStmtScope();
            ExpectOperator(OperatorVal.LEFTCURL, "", out _);
            List<NodeStmt> stmts = new List<NodeStmt>();

            while (Peek() != null && Peek().Kind != TokenKind.NONE)
            {
                NodeStmt stmt = parseStmt();
                if (stmt == null || stmt.Stmt == null)
                {
                    break;
                }
                stmts.Add(stmt);
            }

            ExpectOperator(OperatorVal.RIGHTCURL, "", out _);

            stmtScope.Stmts = stmts;

            return stmtScope;
        }

        NodeStmt parseStmt()
        {
            Token startToken = Peek();
            SourceSpan sourceSpan = startToken.SourceSpan;
            NodeStmt stmt = new NodeStmt();

            if (startToken.Kind == TokenKind.PREPROCESSOR)
            {
                TokenPreprocessor preprocessorToken = (TokenPreprocessor)startToken;
                if (preprocessorToken.Val == PreprocessorVal.PRENAMESPACE)
                {
                    stmt.Stmt = parseStmtNameSpace();
                }
            }

            if (startToken.Kind == TokenKind.KEYWORD)
            {
                TokenKeyword keyword = Peek<TokenKeyword>(TokenKind.KEYWORD);
                if (keyword.Val == KeywordVal.EXTERNAL)
                {
                    stmt.Stmt = parseStmtExternal();
                }
                else if (keyword.Val == KeywordVal.FUNC)
                {
                    stmt.Stmt = parseStmtFuncDecl();
                }
                else if (keyword.Val == KeywordVal.RETURN)
                {
                    stmt.Stmt = parseStmtReturn();
                }
                else if (keyword.Val == KeywordVal.VAR)
                {
                    stmt.Stmt = parseStmtVarDecl();
                }
                else if (keyword.Val == KeywordVal.IF)
                {
                    stmt.Stmt = parseStmtIf();
                }
                else if (keyword.Val == KeywordVal.FOR)
                {
                    stmt.Stmt = parseStmtForLoop();
                }
                else if (keyword.Val == KeywordVal.WHILE)
                {
                    stmt.Stmt = parseStmtWhileLoop();
                }
                else if (keyword.Val == KeywordVal.CALL)
                {
                    stmt.Stmt = parseStmtCallFunction();
                }
                else
                {
                    Console.WriteLine($"Keyword dose not have a case {keyword}");
                }
            }
            else if (startToken.Kind == TokenKind.OPERATOR)
            {
                TokenOperator _operator = Peek<TokenOperator>(TokenKind.OPERATOR);
                // it's 100% a scope
                if (_operator.Val == OperatorVal.LEFTCURL)
                {
                    stmt.Stmt = parseScope();
                }
                else if (_operator.Val == OperatorVal.INTRINSICS)
                {
                    stmt.Stmt = new NodeStmtExpr()
                    {
                        Expr = new NodeExpr() { Expr = parseExprIntrinsics() }
                    };
                }
            }
            else if (startToken.Kind == TokenKind.IDENTIFIER)
            {
                stmt.Stmt = parseStmtVarReassign();
            }
            if (stmt.Stmt != null)
            {
                stmt.Stmt.SourceSpan = sourceSpan;
            }
            return stmt;
        }

        public void ParseProgNode()
        {
            List<NodeStmt> stmts = new List<NodeStmt>();

            JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                MaxDepth = 64,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            serializerOptions.Converters.Add(new JsonStringEnumConverter());

            while (Peek() != null && Peek().Kind != TokenKind.NONE)
            {
                NodeStmt stmt = parseStmt();
                if (stmt == null || stmt.Stmt == null)
                {

                    Console.WriteLine("Something fucked up in the parser");
                    if (debug && args.debug && args.WriteDebugFiles)
                    {
                        AST = new NodeProg();
                        AST.Stmts = stmts;
                        string json = JsonSerializer.Serialize(AST, serializerOptions);
                        File.WriteAllText(debugFile, json);
                        Console.WriteLine("Writing into parser debug file");
                    }
                    Environment.Exit(1);
                }
                stmts.Add(stmt);
            }

            AST = new NodeProg();
            AST.Stmts = stmts;
            if (debug && args.debug && args.WriteDebugFiles)
            {
                string json = JsonSerializer.Serialize(AST, serializerOptions);
                File.WriteAllText(debugFile, json);
            }
        }

        Token Peek(int offset = 0)
        {
            if (TokensIndex + offset < Tokens.Length)
            {
                return Tokens[TokensIndex + offset];
            }
            return null;
        }

        Token Advance()
        {
            // Console.WriteLine($"{Tokens[TokensIndex]}");
            return Tokens[TokensIndex++];
        }

        bool Match(TokenKind type)
        {
            if (Peek().Kind == type)
            {
                Advance();
                return true;
            }
            return false;
        }

        bool Match<T>(TokenKind type, out T value) where T : Token
        {
            if (Peek().Kind == type)
            {
                value = (T)Advance();
                return true;
            }

            value = null;
            return false;
        }
        bool MatchOperator(OperatorVal keyword, out TokenOperator value)
        {
            bool IsX = Match(TokenKind.OPERATOR, out value);

            if (IsX && value != null && value.Val == keyword)
            {
                return true;
            }
            if (value != null)
            {
                TokensIndex--;
            }
            return false;
        }
        bool MatchKeyword(KeywordVal keyword, out TokenKeyword value)
        {
            if (Match(TokenKind.KEYWORD, out value) && value != null && value.Val == keyword)
            {
                return true;
            }
            if (value != null)
            {
                TokensIndex--;
            }
            return false;
        }

        void Expect(TokenKind type, string msg)
        {
            if (!Match(type))
                ThrowError($"Expected {type}: {msg}");
        }

        void Expect<T>(TokenKind type, string msg, out T value) where T : Token
        {
            if (!Match(type, out value))
                ThrowError($"Expected {type}: {msg}");
        }
        T Expect<T>(TokenKind type, string msg) where T : Token
        {
            if (!Match(type, out T value))
                ThrowError($"Expected {type}: {msg}");
            return value;
        }

        void ExpectIdent(string msg, out TokenIdentifier value)
        {
            if (!Match(TokenKind.IDENTIFIER, out value) || value == null || value.Val == null)
                ThrowError($"Expected Identifier: {msg}");
        }
        void ExpectKeyword(KeywordVal keyword, string msg, out TokenKeyword value)
        {
            if (!Match(TokenKind.KEYWORD, out value) || value == null || value.Val != keyword)
                ThrowError($"Expected keyword {keyword}: {msg}");
        }

        void ExpectOperator(OperatorVal keyword, string msg, out TokenOperator value)
        {
            if (!Match(TokenKind.OPERATOR, out value) || value == null || value.Val != keyword)
                ThrowError($"Expected operator {keyword}: {msg}");
        }

        void ThrowError(string msg)
        {
            ProblemLog.LogProblem(State.Error, Peek(-1).SourceSpan, msg);
            throw new Exception(msg + $" at ({Peek(-1).File}:{Peek(-1).Line}:{Peek(-1).Column})");
        }

        T Peek<T>(TokenKind type, int offset = 0) where T : Token
        {
            Token ogToken = Peek(offset);
            if (ogToken.Kind == type)
            {
                T token = (T)ogToken;
                return token;
            }
            return null;
        }

        TokenKeyword PeekKeyword()
        {
            Token token = Peek();
            if (token != null && token.Kind == TokenKind.KEYWORD)
                return (TokenKeyword)token;
            return null;
        }
        TokenOperator PeekOperator()
        {
            Token token = Peek();
            if (token != null && token.Kind == TokenKind.OPERATOR)
                return (TokenOperator)token;
            return null;
        }
    }
}