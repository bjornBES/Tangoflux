using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using CompilerTangoFlex.lexer;

public class TangoFlexParser
{
    public bool debug = true;
    public string debugFile = Path.GetFullPath(Path.Combine("debug", "parser.json"));
    Token[] Tokens;
    Arguments ProgramArguments;
    int TokensIndex { get; set; }
    public NodeProg AST;

    public TangoFlexParser(Token[] tokens, Arguments arguments)
    {
        Tokens = tokens;
        ProgramArguments = arguments;

        ParseProgNode();
    }

    NodeTerm parseTerm()
    {
        NodeTerm nodeTerm = new NodeTerm();

        Token token = Peek();

        Console.WriteLine($"[parseTerm] Kind = {token.Kind}");
        switch (token.Kind)
        {
            case TokenKind.INT:
                TokenInt tokenInt = (TokenInt)Advance();
                nodeTerm.term = new NodeTermIntlit() { value = tokenInt.Val };
                break;
            case TokenKind.IDENTIFIER:
                TokenIdentifier tokenIdentifier = (TokenIdentifier)Advance();
                nodeTerm.term = new NodeTermVar() { name = tokenIdentifier.Val };
                break;
            case TokenKind.STRING:
                TokenString tokenString = (TokenString)Advance();
                nodeTerm.term = new NodeTermStringlit() { value = tokenString.Val };
                break;
        }

        return nodeTerm;
    }

    // from hydrogen-cpp
    // https://github.com/orosmatthew/hydrogen-cpp/blob/master/src/parser.hpp
    NodeExpr parseExpr(int min_prec = 0)
    {
        if (min_prec == 0)
        {
            if (Peek().Kind == TokenKind.KEYWORD && PeekKeyword().Val == KeywordVal.CALL)
            {
                // yes we are calling to get a stmt
                NodeStmtCallFunction callFunction = parseStmtCallFunction();
                return new NodeExpr() { expr = new NodeExprCall() { FuncName = callFunction.functionName, Args = callFunction.funcArguments } };
            }
        }

        if (Peek().Kind == TokenKind.OPERATOR)
        {
            if (PeekOperator().IsUnary())
            {
                NodeExprUnary nodeExprUnary = new NodeExprUnary();
                nodeExprUnary.Operator = parseOperator();
                nodeExprUnary.Operand = parseExpr();
                return new NodeExpr() { expr = nodeExprUnary };
            }
        }

        NodeExpr exprLhs = null;

        if (Peek().Kind == TokenKind.OPERATOR)
        {
            if (PeekOperator().Val == OperatorVal.INTRINSICS)
            {
                exprLhs = new NodeExpr() {expr = parseCast()};
            }
        }
        else
        {

            NodeTerm? term_lhs = parseTerm();
            if (term_lhs == null || term_lhs.term == null)
            {
                return null;
            }
            exprLhs = new NodeExpr() { expr = term_lhs };
        }

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
                    expr = binaryOp
                };
                return exprLhs;
            }
            else if ((op.Val == OperatorVal.PERIOD || op.Val == OperatorVal.RARROW) && Peek(1).Kind != TokenKind.OPERATOR)
            {
                FieldAccessKind fieldAccess;
                if (op.Val == OperatorVal.PERIOD)
                {
                    fieldAccess = FieldAccessKind.Direct;
                }
                else
                {
                    fieldAccess = FieldAccessKind.Indirect;
                }
                Advance();
                NodeExpr field = parseExpr();
                return new NodeExpr() { expr = new NodeExprFieldAccess() { baseExpr = exprLhs, fieldExpr = field, fieldAccessKind = fieldAccess } };
            }
            if (op.Val == OperatorVal.LBRACKET)
            {
                Advance();
                NodeExpr indexExpr = parseExpr();
                ExpectOperator(OperatorVal.RBRACKET, "Expected ] after array index", out _);
                return new NodeExpr()
                {
                    expr = new NodeExprArrayAccess()
                    {
                        array = exprLhs,
                        index = indexExpr
                    }
                };
            }

            int? prec = ((TokenOperator)currToken).binPrec();
            if (prec == null || prec < min_prec)
            {
                break;
            }

            // Expr is binary operator
            Advance();
            int nextMinPrec = prec.Value + 1;
            exprRhs = parseExpr(nextMinPrec);
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
                expr = expr
            };
        }

        return exprLhs;
    }

    NodeExprCast parseCast()
    {
        NodeExprCast nodeExprCast = new NodeExprCast();
        ExpectOperator(OperatorVal.INTRINSICS, "expected '@'", out _);
        Expect(TokenKind.IDENTIFIER, "expected identifier", out TokenIdentifier castIdent);
        if (castIdent == null || !castIdent.Val.Equals("cast"))
        {
            ThrowError("expected cast");
        }
        ExpectOperator(OperatorVal.LPAREN, "", out _);
        NodeType castType = parseType();
        ExpectOperator(OperatorVal.COMMA, "", out _);
        NodeExpr expr = parseExpr();
        ExpectOperator(OperatorVal.RPAREN, "", out _);
        nodeExprCast.type = castType;
        nodeExprCast.expr = expr;
        nodeExprCast.castKind = CastKind.Explicit;
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
            return new NodeVisibility() { visibility = KeywordVal.INTERNAL };
        }

        switch (baseToken.Val)
        {
            case KeywordVal.PUBLIC:
            case KeywordVal.INTERNAL:
            case KeywordVal.PRIVATE:
                visibility.visibility = baseToken.Val;
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
            cc = ProgramArguments.CallingConventions.ToString().ToUpper();
        }

        NodeVisibility nodeVisibility = parseVisibility();
        Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier funcName);
        ExpectOperator(OperatorVal.LPAREN, "", out TokenOperator _);
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
                Console.WriteLine(Peek());
                break;
            }
        }
        ExpectOperator(OperatorVal.RPAREN, "", out TokenOperator _);
        ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
        NodeType type = parseType();
        NodeStmtScope scope = null;
        if (Peek().Kind == TokenKind.OPERATOR && PeekOperator().Val == OperatorVal.LCURL)
        {
            scope = parseScope();
        }

        stmtFuncDecl.returnType = type;
        stmtFuncDecl.callingConvention = cc;
        stmtFuncDecl.nodeVisibility = nodeVisibility;
        stmtFuncDecl.funcName = funcName.Val;
        stmtFuncDecl.scope = scope;
        stmtFuncDecl.parameters = Arguments;

        return stmtFuncDecl;
    }

    NodeStmtReturn parseStmtReturn()
    {
        NodeStmtReturn nodeStmtReturn = new NodeStmtReturn();
        ExpectKeyword(KeywordVal.RETURN, "", out TokenKeyword _);
        Console.WriteLine($"next token = {Peek()}");
        NodeExpr expr = parseExpr();

        Console.WriteLine($"next token = {Peek()}");

        nodeStmtReturn.expr = expr;

        return nodeStmtReturn;
    }

    NodeStmtVarDecl parseStmtVarDecl()
    {
        NodeStmtVarDecl nodeStmtVarDecl = new NodeStmtVarDecl();
        ExpectKeyword(KeywordVal.VAR, "", out _);
        Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier varName);
        ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
        NodeType type = parseType();
        ExpectOperator(OperatorVal.ASSIGN, "", out TokenOperator _);
        NodeExpr expr = parseExpr();
        Console.WriteLine($"next token = {Peek()}");

        nodeStmtVarDecl.expr = expr;
        nodeStmtVarDecl.type = type;
        nodeStmtVarDecl.name = varName.Val;

        return nodeStmtVarDecl;
    }

    NodeStmtIf parseStmtIf()
    {
        NodeStmtIf nodeStmtIf = new NodeStmtIf();
        ExpectKeyword(KeywordVal.IF, "", out TokenKeyword _);
        ExpectOperator(OperatorVal.LPAREN, "", out TokenOperator _);
        NodeExpr condition = parseExpr();
        ExpectOperator(OperatorVal.RPAREN, "", out TokenOperator _);
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
                ExpectOperator(OperatorVal.LPAREN, "", out TokenOperator _);
                NodeExpr elseIfCondition = parseExpr();
                ExpectOperator(OperatorVal.RPAREN, "", out TokenOperator _);
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
        ExpectOperator(OperatorVal.LPAREN, "Expected ( after for", out _);
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
                expr = new NodeTerm()
                {
                    term = new NodeTermIntlit()
                    {
                        value = 1
                    }
                }
            };
        }
        ExpectOperator(OperatorVal.RPAREN, "Expected ) after for loop", out _);
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
        ExpectOperator(OperatorVal.LPAREN, "Expected ( after while", out _);
        NodeExpr condition = parseExpr();
        ExpectOperator(OperatorVal.RPAREN, "Expected ) after while condition", out _);
        NodeStmtScope scope = parseScope();

        stmtWhileLoop.Condition = condition;
        stmtWhileLoop.Scope = scope;

        return stmtWhileLoop;
    }

    NodeStmtCallFunction parseStmtCallFunction()
    {
        NodeStmtCallFunction nodeStmtCallFunction = new NodeStmtCallFunction();
        ExpectKeyword(KeywordVal.CALL, "Expected Call to call function", out _);
        TokenIdentifier tokenIdentifier = Expect<TokenIdentifier>(TokenKind.IDENTIFIER, "Expected Identifier");
        List<NodeExpr> arguments = new List<NodeExpr>();
        ExpectOperator(OperatorVal.LPAREN, "", out TokenOperator _);
        while (true)
        {
            arguments.Add(parseExpr());

            if (!MatchOperator(OperatorVal.COMMA, out TokenOperator _))
            {
                Console.WriteLine(Peek());
                break;
            }
        }
        ExpectOperator(OperatorVal.RPAREN, "", out TokenOperator _);
        nodeStmtCallFunction.funcArguments = arguments.ToArray();
        nodeStmtCallFunction.functionName = tokenIdentifier.Val;
        return nodeStmtCallFunction;
    }

    NodeStmtExternal parseStmtExternal()
    {
        NodeStmtExternal nodeStmtExternal = new NodeStmtExternal();
        ExpectKeyword(KeywordVal.EXTERNAL, "", out _);
        TokenString tokenString = Expect<TokenString>(TokenKind.STRING, "Expected string");
        TokenKeyword type = PeekKeyword();

        IExternal external = null;
        if (type != null)
        {
            if (type.Val == KeywordVal.FUNC)
            {
                NodeStmtFuncDecl funcDecl = parseStmtFuncDecl();
                external = new ExternalFunc()
                {
                    funcName = funcDecl.funcName,
                    nodeVisibility = funcDecl.nodeVisibility,
                    callingConvention = funcDecl.callingConvention,
                    returnType = funcDecl.returnType,
                    parameters = funcDecl.parameters,
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

        nodeStmtExternal.from = tokenString.Raw;
        nodeStmtExternal.external = external;
        return nodeStmtExternal;
    }

    NodeStmtScope parseScope()
    {
        NodeStmtScope stmtScope = new NodeStmtScope();
        ExpectOperator(OperatorVal.LCURL, "", out _);
        List<NodeStmt> stmts = new List<NodeStmt>();

        while (Peek() != null && Peek().Kind != TokenKind.NONE)
        {
            NodeStmt stmt = parseStmt();
            if (stmt == null || stmt.stmt == null)
            {
                break;
            }
            stmts.Add(stmt);
        }

        ExpectOperator(OperatorVal.RCURL, "", out _);

        stmtScope.stmts = stmts;

        return stmtScope;
    }

    NodeStmt parseStmt()
    {
        NodeStmt stmt = new NodeStmt();

        if (Peek().Kind == TokenKind.KEYWORD)
        {
            TokenKeyword keyword = Peek<TokenKeyword>(TokenKind.KEYWORD);
            if (keyword.Val == KeywordVal.EXTERNAL)
            {
                stmt.stmt = parseStmtExternal();
            }
            else if (keyword.Val == KeywordVal.FUNC)
            {
                stmt.stmt = parseStmtFuncDecl();
            }
            else if (keyword.Val == KeywordVal.RETURN)
            {
                stmt.stmt = parseStmtReturn();
            }
            else if (keyword.Val == KeywordVal.VAR)
            {
                stmt.stmt = parseStmtVarDecl();
            }
            else if (keyword.Val == KeywordVal.IF)
            {
                stmt.stmt = parseStmtIf();
            }
            else if (keyword.Val == KeywordVal.FOR)
            {
                stmt.stmt = parseStmtForLoop();
            }
            else if (keyword.Val == KeywordVal.WHILE)
            {
                stmt.stmt = parseStmtWhileLoop();
            }
            else if (keyword.Val == KeywordVal.CALL)
            {
                stmt.stmt = parseStmtCallFunction();
            }
            else
            {
                Console.WriteLine($"Keyword dose not have a case {keyword}");
            }
        }
        else if (Peek().Kind == TokenKind.OPERATOR)
        {
            TokenOperator _operator = Peek<TokenOperator>(TokenKind.OPERATOR);
            // it's 100% a scope
            if (_operator.Val == OperatorVal.LCURL)
            {
                stmt.stmt = parseScope();
            }
        }
        else if (Peek().Kind == TokenKind.IDENTIFIER)
        {
            stmt.stmt = parseStmtVarReassign();
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
            if (stmt == null || stmt.stmt == null)
            {

                Console.WriteLine("Something fucked up in the parser");
                if (debug)
                {
                    AST = new NodeProg();
                    AST.stmts = stmts;
                    string json = JsonSerializer.Serialize(AST, serializerOptions);
                    File.WriteAllText(debugFile, json);
                    Console.WriteLine("Writing into parser debug file");
                }
                Environment.Exit(1);
            }
            stmts.Add(stmt);
        }

        AST = new NodeProg();
        AST.stmts = stmts;
        if (debug)
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
        Console.WriteLine($"{Tokens[TokensIndex]}");
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
        throw new Exception(msg + $" at ({Peek(-1).Line}:{Peek(-1).Column})");
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