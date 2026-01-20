#namespace CompilerTangoFlex.lexer

enum public TokenKind : uint8
{
    None,
    Float,
    Int,
    Char,
    String,
    Identifier,
    Keyword,
    Operator,
    Newline,
}

struct public token {
    var public kind : TokenKind
    var public value : struct ptr
    var public line : int
    var public column : int
}

func public TangoFlexLexer(var src : string, void ptr arguments) : void
{

}

func internal Lex() : void 
{
    
}