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
    var public line : int
    var public column : int
}

struct public TangoFlexLexer {

}

func public Lex(var lexer : ptr TangoFlexLexer, var src : string, void ptr arguments) : void
{

}