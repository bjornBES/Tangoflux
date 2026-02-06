using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection.PortableExecutable;
using System.Text;
using Common;
using TangoFlexCompiler;
using TangoFlexCompiler.debugConsole;

namespace TangoFlexCompiler.Lexer
{
    public class TangoFlexLexer
    {
        public bool debug {get => true && Arguments?.debug == true; }
        public string debugFile = Path.GetFullPath(Path.Combine("debug", "lexer.txt"));
        StringBuilder debugString = new StringBuilder();

        Dictionary<string, FileId> files = new Dictionary<string, FileId>();
        int id = 0;

        private Arguments Arguments;
        public string Source { get; internal set; }
        int index;
        private ImmutableList<FSA> FSAs { get; }
        public IEnumerable<Token> Tokens { get; private set; }
        private int Line;
        private int Column;
        private string CurrentFile;

        private FSAPreprocessor FSAPreprocessor;
        private FSAOperator FSAOperator;
        private FSAInt FSAInt;
        private int errorCount = 0;
        public TangoFlexLexer(Arguments arguments)
        {
            if (debug)
            {
                File.WriteAllText(debugFile, "");
            }
            errorCount = 0;
            CurrentFile = arguments.InputFile;
            Arguments = arguments;
            index = 0;
            FSAPreprocessor = new FSAPreprocessor();
            FSAOperator = new FSAOperator();
            FSAInt = new FSAInt();
            FSAs =
            [
                new FSAFloat(),
                new FSAInt(),
                new FSACharConst(),
            ];
        }

        public void Process(string src)
        {
            errorCount = 0;
            Source = src;
            index = 0;
            Tokens = Lex(src, CurrentFile);

            if (debug == true && Arguments.WriteDebugFiles == true)
            {
                File.WriteAllText(debugFile, debugString.ToString());
            }
        }

        public IEnumerable<Token> Lex(string src, string file)
        {
            Source = src;
            CurrentFile = file;
            index = 0;
            Line = 1;
            Column = 1;
            errorCount = 0;
            FSAs.ForEach(fsa => fsa.Reset());
            FSAOperator.Reset();
            FSAPreprocessor.Reset();

            return Lex();
        }

        private IEnumerable<Token> Lex()
        {
            files.Add(CurrentFile, new FileId(id));
            CompilerInfo.Files.Add(new FileId(id), CurrentFile);
            id++;
            var tokens = new List<Token>();

            int currentLine = Line;
            int currentColumn = Column;

            while (peek().HasValue)
            {
                currentLine = Line;
                currentColumn = Column;
                if (peek().Value == '#')
                {
                    advance(); // #
                    // preprocessor keywords
                    string preprocessorKeyword = "#" + getIdent(isIdent);
                    Token token = FSAPreprocessor.RetrieveToken(preprocessorKeyword);
                    if (token.GetType() == typeof(EmptyToken))
                    {
                        DebugConsole.WriteLine($"preprocessorKeyword = {preprocessorKeyword}");
                    }
                    if (((TokenPreprocessor)token).Val == PreprocessorVal.PREINCLUDE)
                    {
                        AddToken(token, ref tokens, currentLine, currentColumn);
                        if (peek().HasValue && peek().Value == ' ')
                        {
                            advance();
                        }
                        if (peek().HasValue && peek().Value == '"')
                        {
                            advance();
                            string buffer = getIdent((c) =>
                            {
                                return c != '"';
                            });
                            advance();
                            buffer = $"\"{buffer}\"";
                            AddToken(new TokenString(buffer, TokenString.StringPrefix.U8, buffer), ref tokens, currentLine, currentColumn);
                        }
                        else if (peek().HasValue && peek().Value == '<')
                        {
                            advance();
                            string buffer = getIdent((c) =>
                            {
                                return c != '>';
                            });
                            advance();
                            buffer = $"<{buffer}>";
                            AddToken(new TokenString(buffer, TokenString.StringPrefix.U8, buffer), ref tokens, currentLine, currentColumn);
                        }
                    }
                    else
                    {
                        AddToken(token, ref tokens, currentLine, currentColumn);
                    }
                    FSAs.ForEach(fsa => fsa.Reset());
                }
                else if (peek().Value == '/' && peek(1).HasValue && peek(1).Value == '/')
                {
                    // comment
                    advance(); // consume /
                    advance(); // consume /
                    getIdent((c) => !isNewline(c));
                    FSAs.ForEach(fsa => fsa.Reset());
                }
                else if (peek().Value.ToString().Equals("u", StringComparison.CurrentCultureIgnoreCase) && peek(1).HasValue && char.IsDigit(peek(1).Value))
                {
                    string prefix = getIdent((c) =>
                    {
                        return c != '"';
                    });
                    advance();
                    string buffer = getIdent((c) =>
                    {
                        return c != '"';
                    });
                    advance();
                    AddToken(new TokenString(buffer, Enum.Parse<TokenString.StringPrefix>(prefix, true), buffer), ref tokens, currentLine, currentColumn);
                    FSAs.ForEach(fsa => fsa.Reset());
                }
                else if (peek().Value == '"')
                {
                    advance();
                    string buffer = getIdent((c) =>
                    {
                        return c != '"';
                    });
                    advance();
                    AddToken(new TokenString(buffer, TokenString.StringPrefix.U8, buffer), ref tokens, currentLine, currentColumn);
                    FSAs.ForEach(fsa => fsa.Reset());

                }
                else if (char.IsLetter(peek().Value) || peek().Value == '_')
                {
                    string ident = getIdent(isIdent);

                    if (TokenKeyword.Keywords.TryGetValue(ident.ToUpper(), out KeywordVal keywordVal))
                    {
                        AddToken(new TokenKeyword(keywordVal), ref tokens, currentLine, currentColumn);
                    }
                    else
                    {
                        AddToken(new TokenIdentifier(ident), ref tokens, currentLine, currentColumn);
                    }

                    FSAs.ForEach(fsa => fsa.Reset());
                }
                else if (char.IsDigit(peek().Value))
                {
                    string literal = "";

                    while (peek().HasValue && (char.IsLetterOrDigit(peek().Value) || peek().Value == '_'))
                    {
                        if (FSAInt.GetStatus() == FSAStatus.END)
                        {
                            break;
                        }
                        char c = advance();
                        literal += c;
                        FSAInt.ReadChar(c);
                        if (FSAInt.GetStatus() == FSAStatus.ERROR)
                        {
                            DebugConsole.WriteLine($"Error with int FSA with {literal}");
                            break;
                        }
                    }

                    AddToken(FSAInt.RetrieveToken(), ref tokens, currentLine, currentColumn);

                    FSAInt.Reset();
                }
                else if (isNewline(peek().Value))
                {
                    AddToken(new NewlineToken(), ref tokens, currentLine, currentColumn);
                    if (peek().Value == '\r')
                    {
                        // windows fuck you
                        advance();
                        if (peek().HasValue && peek().Value == '\n')
                        {
                            advance();
                        }
                        else
                        {
                            if (Arguments.OneCharNewLine)
                            {
                                DebugConsole.WriteLine($"you do you with that \\r");
                            }
                            else
                            {
                                // wait what? only \r? ok you do you
                                DebugConsole.WriteLine($"at {formatLineColumn()} only \\r was consumed");
                                DebugConsole.WriteLine($"enable the --one-char-new-line flag to only get a message");
                            }
                        }
                    }
                    else
                    {
                        // unix thanks
                        // see windows you only need 1 char for new lines
                        // and not 2
                        // it's probably bc windows is 20% AI now
                        // dame
                        advance();
                    }
                    FSAs.ForEach(fsa => fsa.Reset());
                    Line++;
                    Column = 1;
                }
                else if (peek().Value == ' ')
                {
                    advance();
                }
                else
                {
                    string OperatorBuffer = getIdent((c, buffer) =>
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            return false;
                        }
                        if (!string.IsNullOrEmpty(buffer))
                        {
                            if (TokenOperator.Operators.ContainsKey(buffer + c) && !(peek(1).HasValue && isOperator(peek(1).Value)))
                            {
                                return true;
                            }
                        }
                        if (peek(1).HasValue && isOperator(peek(1).Value))
                        {
                            if (TokenOperator.Operators.ContainsKey(buffer + peek(1).Value) == true)
                            {
                                return true;
                            }
                            return false;
                        }
                        return isOperator(c) && !TokenOperator.Operators.ContainsKey(buffer);
                    });

                    if (TokenOperator.Operators.TryGetValue(OperatorBuffer, out OperatorVal operatorVal))
                    {
                        AddToken(new TokenOperator(operatorVal), ref tokens, currentLine, currentColumn);
                    }
                    else
                    {
                        DebugConsole.WriteLine($"Error: unknown operator '{OperatorBuffer}'");
                    }
                }
            }

            currentLine = Line;
            currentColumn = Column;

            FSAs.ForEach(fsa => fsa.ReadEOF());
            int idx2 = FSAs.FindIndex(fsa => fsa.GetStatus() == FSAStatus.END);
            if (idx2 != -1)
            {
                Token token = FSAs[idx2].RetrieveToken();
                if (token.Kind != TokenKind.NONE)
                {
                    AddToken(token, ref tokens, currentLine, currentColumn);
                }
            }
            else
            {
                DebugConsole.WriteLine($"error idx2 = {idx2}");
            }

            AddToken(new EmptyToken(), ref tokens, currentLine, currentColumn);
            return tokens;
        }

        string formatLineColumn()
        {
            return $"{Line}:{Column}";
        }

        bool isOperator(char c)
        {
            return FSAOperator.OperatorChars.Contains(c);
        }
        bool isIdent(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
        bool isNewline(char c)
        {
            return Environment.NewLine.StartsWith(c);
        }

        string getIdent(Func<char, bool> selector)
        {
            string buffer = "";
            while (peek().HasValue && selector(peek().Value))
            {
                buffer += advance();
            }
            return buffer;
        }
        string getIdent(Func<char, string, bool> selector)
        {
            string buffer = "";
            while (peek().HasValue && selector(peek().Value, buffer))
            {
                buffer += advance();
            }
            return buffer;
        }

        char? peek(int offset = 0)
        {
            return index + offset < Source.Length ? Source[index + offset] : null;
        }
        char advance()
        {
            Column++;
            return Source[index++];
        }
        public void AddToken(Token token, ref List<Token> tokens, int startLine, int startColumn)
        {
            SourceSpan source = new SourceSpan(files[CurrentFile], startLine, startColumn, Line, Column);
            errorCount = 0;
            token.SourceSpan = source;
            token.Line = Line;
            token.Column = Column;
            token.File = CurrentFile;
            tokens.Add(token);
            if (debug)
            {
                debugString.AppendLine(token.ToString());
            }
        }
    }
}