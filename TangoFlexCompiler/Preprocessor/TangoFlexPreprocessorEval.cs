
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Common;
using TangoFlexCompiler.Lexer;
using TangoFlexCompiler.ProblemLogging;

namespace TangoFlexCompiler.Preprocessor
{
    internal enum PPValueKind
    {
        Int,
        String,
        Bool,
        Undefined
    }

    internal struct PPValue
    {
        public PPValueKind Kind;
        public long Int;
        public string String;
        public bool Bool;

        public static PPValue FromInt(long v) => new() { Kind = PPValueKind.Int, Int = v };
        public static PPValue FromString(string v) => new() { Kind = PPValueKind.String, String = v };
        public static PPValue FromBool(bool v) => new() { Kind = PPValueKind.Bool, Bool = v };
        public static PPValue Undefined() => new() { Kind = PPValueKind.Undefined };
    }

    internal class Symbol
    {
        public string Name { get; init; }
        public string? Value { get; set; } // optional
        public Token[] Tokens { get; init; }
        public bool Locked { get; init; } // can it be undef (e.g. __windows__, __unix__, ect)
        public Symbol(string name, Token[]? tokens = null)
        {
            Name = name;
            Tokens = tokens;
        }
    }

    public sealed class ConditionalState
    {
        public Token Token;
        public bool ParentEnabled;   // Whether parent blocks allow emission
        public bool AnyTaken;        // Whether any #if/#else if branch already evaluated true
        public bool CurrentEnabled;  // Whether this branch emits code
    }

    public class TangoFlexPreprocessorEval
    {
        public bool debug = true;
        public string debugFile = Path.GetFullPath(Path.Combine("debug", "preprocessor_eval.tf"));
        public Token[] tokenOut;
        private List<Token> tokens = new List<Token>();
        private int TokensIndex = 0;

        private readonly Dictionary<string, Symbol> symbols = new();
        private readonly TangoFlexLexer lexer;
        private readonly TangoFlexPreprocessor preprocessor;
        private Arguments arguments;
        public TangoFlexPreprocessorEval(TangoFlexLexer tangoFlexLexer, TangoFlexPreprocessor tangoFlexPreprocessor, Arguments arguments)
        {
            this.arguments = arguments;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                symbols.Add("__windows__", new Symbol("__windows__", [new TokenInt(1, TokenInt.IntSuffix.NONE, "1")]));
                // Windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                symbols.Add("__unix__", new Symbol("__unix__", [new TokenInt(1, TokenInt.IntSuffix.NONE, "1")]));
                symbols.Add("__linux__", new Symbol("__linux__", [new TokenInt(1, TokenInt.IntSuffix.NONE, "1")]));
                // Linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                symbols.Add("__unix__", new Symbol("__unix__", [new TokenInt(1, TokenInt.IntSuffix.NONE, "1")]));
                symbols.Add("__mac__", new Symbol("__mac__", [new TokenInt(1, TokenInt.IntSuffix.NONE, "1")]));
                // macOS
            }

            if (arguments.UseFatStrings)
            {
                symbols.Add("__fat_strings__", new Symbol("__fat_strings__", [new TokenInt(1, TokenInt.IntSuffix.NONE, "1")]));
            }


            preprocessor = tangoFlexPreprocessor;
            lexer = tangoFlexLexer;
        }

        public void Process(Token[] tokens)
        {
            this.tokens = tokens.ToList();
            tokenOut = Process();

            if (debug && arguments.debug && arguments.WriteDebugFiles)
            {
                File.WriteAllText(debugFile, string.Join("\n", tokenOut.Select(t => t.ToString())));
            }
        }

        /* ========================== PROCESS ========================== */

        private Token[] Process()
        {
            List<Token> output = new();
            Stack<ConditionalState> condStack = new();
            bool currentEnabled = true;

            while (TokensIndex < tokens.Count)
            {
                Token tok = Peek();

                if (tok.Kind == TokenKind.PREPROCESSOR)
                {
                    TokenPreprocessor pp = (TokenPreprocessor)tok;

                    if (pp.Val == PreprocessorVal.PREDEFINE)
                    {
                        HandleDefine();
                        continue;
                    }
                    if (pp.Val == PreprocessorVal.PREUNDEF)
                    {
                        HandleUndef();
                        continue;
                    }
                    if (pp.Val == PreprocessorVal.PREFILE || pp.Val == PreprocessorVal.PREENDFILE)
                    {
                        Advance();
                        if (pp.Val == PreprocessorVal.PREFILE)
                        {
                            Advance();
                        }
                        continue;
                    }
                    if (pp.Val == PreprocessorVal.PREINCLUDE)
                    {
                        Advance();
                        if (Peek().Kind == TokenKind.STRING)
                        {
                            TokenString tokenString = (TokenString)Advance();
                            bool localFile = tokenString.Val.StartsWith('"');
                            string file = tokenString.Val.Trim('<', '>', '"');
                            string basePath = "";
                            if (localFile == false)
                            {
                                basePath = Path.Combine("lib");
                            }
                            string path = Path.Combine(basePath, file);
                            if (!File.Exists(path))
                            {
                                throw new Exception($"File dose not exist {path}");
                            }
                            string src = File.ReadAllText(path);
                            string fileSrc = preprocessor.Process(src, file);
                            Token[] fileTokens = lexer.Lex(fileSrc, file).ToArray();
                            tokens.InsertRange(TokensIndex, fileTokens[0..(fileTokens.Length - 1)]);
                        }
                        continue;
                    }
                    else if (pp.Val == PreprocessorVal.PRENAMESPACE)
                    {
                        output.Add(pp);
                        Advance();
                        continue;
                    }
                    else if (pp.Val == PreprocessorVal.PREIMPORT)
                    {
                        output.Add(pp);
                        Advance();
                        continue;
                    }

                    HandleConditional(condStack, ref currentEnabled);
                    continue;
                }

                if (tok.Kind == TokenKind.NEWLINE)
                {
                    Advance();
                    continue;
                }

                if (currentEnabled)
                    output.Add(Advance());
                else
                    Advance();
            }

            if (condStack.Count != 0)
                ThrowError($"Unmatched #if/#endif at {condStack.First().Token.Line}:{condStack.First().Token.Column}");

            Token[] processed = output.ToArray();
            processed = ExpandMacros(processed);
            return processed;

        }

        private Token[] ExpandMacros(Token[] input)
        {
            List<Token> output = new();

            for (int i = 0; i < input.Length; i++)
            {
                Token tok = input[i];

                if (tok.Kind == TokenKind.IDENTIFIER)
                {
                    TokenIdentifier id = (TokenIdentifier)tok;

                    if (symbols.TryGetValue(id.Val, out Symbol sym) &&
                        !sym.Locked &&
                        sym.Tokens != null)
                    {
                        output.AddRange(ExpandSymbol(sym, tok));
                        continue;
                    }
                }

                output.Add(tok);
            }

            return output.ToArray();
        }

        private readonly HashSet<string> expanding = new();

        private IEnumerable<Token> ExpandSymbol(Symbol sym, Token origin)
        {
            if (expanding.Contains(sym.Name))
                ThrowError($"Recursive macro expansion: {sym.Name}");

            expanding.Add(sym.Name);

            foreach (Token t in sym.Tokens)
                yield return CloneToken(t, origin);

            expanding.Remove(sym.Name);
        }



        /* ======================= CONDITIONALS ======================= */

        private void HandleConditional(Stack<ConditionalState> condStack, ref bool currentEnabled)
        {
            TokenPreprocessor pp = (TokenPreprocessor)Advance();

            switch (pp.Val)
            {
                case PreprocessorVal.PREIF:
                case PreprocessorVal.PREIFDEF:
                case PreprocessorVal.PREIFNDEF:
                    {
                        bool result = EvaluateConditional(pp.Val);

                        condStack.Push(new ConditionalState
                        {
                            Token = pp,
                            ParentEnabled = currentEnabled,
                            AnyTaken = result,
                            CurrentEnabled = currentEnabled && result
                        });

                        currentEnabled = condStack.Peek().CurrentEnabled;
                        break;
                    }

                case PreprocessorVal.PREELIF:
                    {
                        bool result = EvaluateConditional(PreprocessorVal.PREELIF);
                        ConditionalState state = condStack.Pop();

                        bool enabled = !state.AnyTaken && state.ParentEnabled && result;
                        state.AnyTaken |= enabled;
                        state.CurrentEnabled = enabled;

                        condStack.Push(state);
                        currentEnabled = enabled;
                        break;
                    }

                case PreprocessorVal.PREELSE:
                    {
                        ConditionalState state = condStack.Pop();
                        state.CurrentEnabled = !state.AnyTaken && state.ParentEnabled;
                        state.AnyTaken = true;

                        condStack.Push(state);
                        currentEnabled = state.CurrentEnabled;
                        break;
                    }

                case PreprocessorVal.PREENDIF:
                    {
                        if (condStack.Count == 0)
                            ThrowError("Unmatched #endif");

                        currentEnabled = condStack.Pop().ParentEnabled;
                        break;
                    }

                default:
                    ThrowError($"Unexpected preprocessor directive: {pp.Val}");
                    break;
            }
        }

        private bool EvaluateConditional(PreprocessorVal type)
        {
            List<Token> exprTokens = new();
            while (TokensIndex < tokens.Count && Peek().Kind != TokenKind.NEWLINE)
                exprTokens.Add(Advance());

            if (type == PreprocessorVal.PREIFDEF || type == PreprocessorVal.PREIFNDEF)
            {
                if (exprTokens.Count != 1 || exprTokens[0].Kind != TokenKind.IDENTIFIER)
                    ThrowError("#ifdef/#ifndef requires identifier");

                bool defined = symbols.ContainsKey(((TokenIdentifier)exprTokens[0]).Val);
                return type == PreprocessorVal.PREIFDEF ? defined : !defined;
            }

            PPValue v = EvalExpr(exprTokens);
            return v.Kind == PPValueKind.Bool ? v.Bool :
                   v.Kind == PPValueKind.Int ? v.Int != 0 :
                   false;
        }

        /* ======================= EXPRESSIONS ======================= */

        private PPValue EvalExpr(List<Token> tokens)
        {
            int idx = 0;
            return EvalOr(ref idx, tokens);
        }

        private PPValue EvalOr(ref int idx, List<Token> t)
        {
            PPValue left = EvalAnd(ref idx, t);

            while (idx < t.Count && MatchOp(t[idx], OperatorVal.OR))
            {
                idx++;
                PPValue right = EvalAnd(ref idx, t);
                left = PPValue.FromBool(ToBool(left) || ToBool(right));
            }

            return left;
        }

        private PPValue EvalAnd(ref int idx, List<Token> t)
        {
            PPValue left = EvalEquality(ref idx, t);

            while (idx < t.Count && MatchOp(t[idx], OperatorVal.AND))
            {
                idx++;
                PPValue right = EvalEquality(ref idx, t);
                left = PPValue.FromBool(ToBool(left) && ToBool(right));
            }

            return left;
        }

        private PPValue EvalEquality(ref int idx, List<Token> t)
        {
            PPValue left = EvalUnary(ref idx, t);

            while (idx < t.Count &&
                  (MatchOp(t[idx], OperatorVal.EQ) || MatchOp(t[idx], OperatorVal.NEQ)))
            {
                bool eq = MatchOp(t[idx], OperatorVal.EQ);
                idx++;
                PPValue right = EvalUnary(ref idx, t);

                bool result = Compare(left, right);
                left = PPValue.FromBool(eq ? result : !result);
            }

            return left;
        }

        private PPValue EvalUnary(ref int idx, List<Token> t)
        {
            if (MatchOp(t[idx], OperatorVal.NOT))
            {
                idx++;
                return PPValue.FromBool(!ToBool(EvalUnary(ref idx, t)));
            }

            if (MatchOp(t[idx], OperatorVal.SUB))
            {
                idx++;
                return PPValue.FromInt(-EvalUnary(ref idx, t).Int);
            }

            if (MatchOp(t[idx], OperatorVal.ADD))
            {
                idx++;
                return EvalUnary(ref idx, t);
            }

            return EvalPrimary(ref idx, t);
        }

        private PPValue EvalPrimary(ref int idx, List<Token> t)
        {
            Token tok = t[idx++];

            switch (tok.Kind)
            {
                case TokenKind.INT:
                    TokenInt i = (TokenInt)tok;
                    return PPValue.FromInt(i.Val);
                case TokenKind.STRING:
                    TokenString s = (TokenString)tok;
                    return PPValue.FromString(s.Val);
                case TokenKind.IDENTIFIER:
                    TokenIdentifier id = (TokenIdentifier)tok;
                    if (symbols.TryGetValue(id.Val, out Symbol sym))
                    {
                        return ParseSymbol(sym);
                    }
                    else
                    {
                        return PPValue.FromBool(false);
                    }
            }

            return tok switch
            {
                TokenInt i => PPValue.FromInt(i.Val),
                TokenString s => PPValue.FromString(s.Val),
                TokenIdentifier id => symbols.TryGetValue(id.Val, out Symbol sym)
                                        ? ParseSymbol(sym)
                                        : PPValue.FromBool(false),
                _ => ThrowValue("Invalid preprocessor expression")
            };
        }

        private PPValue ParseSymbol(Symbol s)
        {
            if (string.IsNullOrEmpty(s.Value))
            {
                PPValue pValue = EvalExpr(s.Tokens.ToList());
                switch (pValue.Kind)
                {
                    case PPValueKind.Int:
                        s.Value = pValue.Int.ToString();
                        break;
                    case PPValueKind.String:
                        s.Value = pValue.String;
                        break;
                    case PPValueKind.Bool:
                        s.Value = pValue.Bool.ToString();
                        break;
                    case PPValueKind.Undefined:
                        s.Value = PPValue.FromBool(false).Bool.ToString();
                        break;
                }
            }
            if (long.TryParse(s.Value, out long i))
                return PPValue.FromInt(i);

            return PPValue.FromString(s.Value);
        }

        private static bool Compare(PPValue a, PPValue b)
        {
            if (a.Kind == PPValueKind.Int && b.Kind == PPValueKind.Int)
                return a.Int == b.Int;

            return a.String == b.String;
        }

        private static bool ToBool(PPValue v)
        {
            return v.Kind switch
            {
                PPValueKind.Bool => v.Bool,
                PPValueKind.Int => v.Int != 0,
                PPValueKind.String => !string.IsNullOrEmpty(v.String),
                _ => false
            };
        }

        /* ======================= DEFINES ======================= */

        private void HandleDefine()
        {
            Advance(); // #define

            TokenIdentifier name = Expect<TokenIdentifier>(
                TokenKind.IDENTIFIER, "Expected macro name");

            // optional `as` or `=`
            if ((Peek().Kind == TokenKind.OPERATOR && PeekOperator().Val == OperatorVal.ASSIGN) ||
                (Peek().Kind == TokenKind.KEYWORD && PeekKeyword().Val == KeywordVal.AS))
            {
                Advance();
            }

            List<Token> body = new();

            while (TokensIndex < tokens.Count && Peek().Kind != TokenKind.NEWLINE)
                body.Add(Advance());

            symbols[name.Val] = new Symbol(name.Val, body.ToArray());
        }

        private static Token CloneToken(Token t, Token origin)
        {
            Token clone = t.Clone(); // or manual copy
            clone.Line = origin.Line;
            clone.Column = origin.Column;
            return clone;
        }

        private void HandleUndef()
        {
            Advance();
            TokenIdentifier name = Expect<TokenIdentifier>(TokenKind.IDENTIFIER, "Expected symbol");
            symbols.Remove(name.Val);
        }

        /* ======================= HELPERS ======================= */

        private static bool MatchOp(Token t, OperatorVal op)
            => t.Kind == TokenKind.OPERATOR && ((TokenOperator)t).Val == op;

        private PPValue ThrowValue(string msg)
        {
            ThrowError(msg);
            return default;
        }

        Token Peek(int o = 0) => TokensIndex + o < tokens.Count ? tokens[TokensIndex + o] : null;
        Token Advance() => tokens[TokensIndex++];

        T Expect<T>(TokenKind k, string m) where T : Token
        {
            if (Peek().Kind != k) ThrowError(m);
            return (T)Advance();
        }

        void ThrowError(string msg)
        {
            ProblemLog.LogProblem(State.Error, Peek(-1).SourceSpan, msg);
            // throw new Exception(msg + $" at ({Peek(-1)?.Line}:{Peek(-1)?.Column})");
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