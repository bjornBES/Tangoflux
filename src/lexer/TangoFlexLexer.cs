using System.Collections.Immutable;
using System.Reflection.PortableExecutable;

namespace CompilerTangoFlex.lexer;

internal static class LexerConfig
{
    public static int Line;
    public static int Column;
}

internal class TangoFlexLexer
{
    public bool debug = true;
    public string debugFile = Path.GetFullPath(Path.Combine("debug", "lexer.txt"));

    private Arguments Arguments;
    public string Source { get; }
    private ImmutableList<FSA> FSAs { get; }
    public IEnumerable<Token> Tokens { get; }

    public TangoFlexLexer(string src, Arguments arguments)
    {
        if (debug)
        {
            File.WriteAllText(debugFile, "");
        }
        Source = src;
        Arguments = arguments;
        FSAs =
        [
            new FSAFloat(),
            new FSAInt(),
            new FSAOperator(),
            new FSAIdentifier(),
            new FSASpace(),
            new FSANewLine(),
            new FSACharConst(),
            new FSAString()
,
        ];
        Tokens = Lex();
    }

    int buffer_column = 1;
    char buffer_char = '\0';
    private IEnumerable<Token> Lex()
    {
        var tokens = new List<Token>();
        for (int i = 0; i < Source.Length; ++i)
        {
            string c = (Source[i] == '\n') ? "\\n" : Source[i].ToString();
            LexerConfig.Column++;
            // Console.WriteLine($"new char {c}");
            FSAs.ForEach(fsa => fsa.ReadChar(Source[i]));

            // if no running
            if (FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.RUNNING) == -1)
            {
                int idx = FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.END);
                if (idx != -1)
                {
                    Token token = FSAs[idx].RetrieveToken();
                    if (LexerConfig.Column == 0)
                    {
                        buffer_column = 1;
                        LexerConfig.Column = 1;
                    }
                    if (token.Kind == TokenKind.NEWLINE)
                    {
                    }
                    else if (token.Kind != TokenKind.NONE)
                    {
                        if (buffer_column == 0)
                        {
                            buffer_column = LexerConfig.Column;
                        }
                        AddToken(token, ref tokens);
                        buffer_char = Source[i];
                        buffer_column = LexerConfig.Column;
                        // Console.WriteLine($"buffer column = {buffer_column} LexerConfig.Column = {LexerConfig.Column}");
                    }
                    LexerConfig.Column--;
                    i--;
                    FSAs.ForEach(fsa => fsa.Reset());
                }
                else
                {
                    Console.WriteLine("error");
                }
            }
        }

        FSAs.ForEach(fsa => fsa.ReadEOF());
        // find END
        int idx2 = FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.END);
        if (idx2 != -1)
        {
            Token token = FSAs[idx2].RetrieveToken();
            if (token.Kind != TokenKind.NONE)
            {
                AddToken(token, ref tokens);
            }
        }
        else
        {
            Console.WriteLine("error");
        }

        AddToken(new EmptyToken(), ref tokens);
        return tokens;
    }


    public void AddToken(Token token, ref List<Token> tokens)
    {
        if (token.Kind == TokenKind.NEWLINE)
        {
            return;
        }
        token.Line = LexerConfig.Line;
        token.Column = buffer_column;
        tokens.Add(token);
        if (debug)
        {
            File.AppendAllText(debugFile, token.ToString() + Environment.NewLine);
        }
    }
}