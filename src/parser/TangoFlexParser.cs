using System.Text.Json;
using CompilerTangoFlex.lexer;

public class TangoFlexParser
{
    public bool debug = true;
    public string debugFile = Path.GetFullPath(Path.Combine("debug", "parser.json"));
    Token[] Tokens;
    Arguments Arguments;
    int TokensIndex { get; set; }
    public NodeProg AST;

    public TangoFlexParser(Token[] tokens, Arguments arguments)
    {
        Tokens = tokens;
        Arguments = arguments;

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
        }

        return nodeTerm;
    }

    NodeExpr parseExpr()
    {
        NodeExpr expr = new NodeExpr();

        {
            expr.expr = parseTerm();
        }

        return expr;
    }

    NodeType parseType()
    {
        NodeType nodeType = new NodeType();
        TokenKeyword keywordType = Peek<TokenKeyword>(TokenKind.KEYWORD);
        switch (keywordType.Val)
        {
            case KeywordVal.FLOAT:
            case KeywordVal.BOOL:
            case KeywordVal.INT:
            case KeywordVal.STRING:
            case KeywordVal.VOID:
                nodeType.type = keywordType.Val;
                Advance();
                break;
        }
        return nodeType;
    }

    NodeStmtFuncDecl parseStmtFuncDecl()
    {
        NodeStmtFuncDecl stmtFuncDecl = new NodeStmtFuncDecl();
        List<FuncArguments> Arguments = new List<FuncArguments>();
        ExpectKeyword(KeywordVal.FUNC, "", out TokenKeyword _);
        Expect(TokenKind.IDENTIFIER, "", out TokenIdentifier funcName);
        ExpectOperator(OperatorVal.LPAREN, "", out TokenOperator _);
        // TODO:
        ExpectOperator(OperatorVal.RPAREN, "", out TokenOperator _);
        ExpectOperator(OperatorVal.COLON, "", out TokenOperator _);
        NodeType type = parseType();
        NodeStmtScope scope = parseScope();

        stmtFuncDecl.returnType = type;
        stmtFuncDecl.funcName = funcName.Val;
        stmtFuncDecl.scope = scope;
        stmtFuncDecl.arguments = Arguments;

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
            if (keyword.Val == KeywordVal.FUNC)
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
            else
            {
                Console.WriteLine($"Keyword dose not have a case {keyword}");
            }
        }
        else
        {
        }

        return stmt;
    }

    public void ParseProgNode()
    {
        List<NodeStmt> stmts = new List<NodeStmt>();

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
                    string json = JsonSerializer.Serialize(AST, new JsonSerializerOptions() { WriteIndented = true });
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
            string json = JsonSerializer.Serialize(AST, new JsonSerializerOptions() { WriteIndented = true });
            File.WriteAllText(debugFile, json);
        }
    }

    Token Peek(int offset = 0)
        => TokensIndex + offset < Tokens.Length ? Tokens[TokensIndex + offset] : null;

    Token Advance()
        => Tokens[TokensIndex++];

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

    void ExpectKeyword(KeywordVal keyword, string msg, out TokenKeyword value)
    {
        if (!Match(TokenKind.KEYWORD, out value) && value.Val == keyword)
            ThrowError($"Expected keyword {keyword}: {msg}");
    }

    void ExpectOperator(OperatorVal keyword, string msg, out TokenOperator value)
    {
        if (!Match(TokenKind.OPERATOR, out value) && value == null && value.Val == keyword)
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
}