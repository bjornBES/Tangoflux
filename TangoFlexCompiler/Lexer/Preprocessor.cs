using System;
using System.Collections.Generic;

namespace TangoFlexCompiler.Lexer
{
    public enum PreprocessorVal
    {
        NONE = 0,
        // preprocessor
        PREDEFINE,
        PREUNDEF,
        PREENDIF,
        PREIF,
        PREIFDEF,
        PREIFNDEF,
        PREELSE,
        PREELIF,
        PREFILE,
        PREENDFILE,
        PREINCLUDE,
        PRENAMESPACE,
        PREIMPORT,
    }

    public class TokenPreprocessor : Token
    {
        public TokenPreprocessor(PreprocessorVal val)
        {
            Val = val;
        }

        public override TokenKind Kind { get; } = TokenKind.PREPROCESSOR;
        public PreprocessorVal Val { get; }
        public static Dictionary<string, PreprocessorVal> PreprocessorKeywords { get; } = new Dictionary<string, PreprocessorVal>(StringComparer.InvariantCultureIgnoreCase) {

            { "#IF",        PreprocessorVal.PREIF        },
            { "#ELIF",      PreprocessorVal.PREELIF      },
            { "#ELSE",      PreprocessorVal.PREELSE      },
            { "#IFDEF",     PreprocessorVal.PREIFDEF     },
            { "#IFNDEF",    PreprocessorVal.PREIFNDEF    },
            { "#DEFINE",    PreprocessorVal.PREDEFINE    },
            { "#UNDEF",     PreprocessorVal.PREUNDEF     },
            { "#ENDIF",     PreprocessorVal.PREENDIF     },
            { "#FILE",      PreprocessorVal.PREFILE      },
            { "#ENDFILE",   PreprocessorVal.PREENDFILE   },
            { "#INCLUDE",   PreprocessorVal.PREINCLUDE   },
            { "#NAMESPACE", PreprocessorVal.PRENAMESPACE },
            { "#IMPORT",    PreprocessorVal.PREIMPORT    },
        };

        public override string ToString()
        {
            return $"({File}:{Line}:{Column}): " + Kind + ": " + Val;
        }

        public override Token Clone()
        {
            return new TokenPreprocessor(Val);
        }

        public override object GetData()
        {
            int valueIndex = PreprocessorKeywords.Values.ToList().IndexOf(Val);
            return PreprocessorKeywords.Keys.ElementAt(valueIndex).ToString();
        }
    }

    public sealed class FSAPreprocessor : FSA
    {
        private enum State
        {
            START,
            END,
            ERROR,
            ID
        };
        private State _state;
        private string _scanned;

        public FSAPreprocessor()
        {
            _state = State.START;
            _scanned = "";
        }

        public override void Reset()
        {
            _state = State.START;
            _scanned = "";
        }

        public override FSAStatus GetStatus()
        {
            if (_state == State.START)
            {
                return FSAStatus.NONE;
            }
            if (_state == State.END)
            {
                return FSAStatus.END;
            }
            if (_state == State.ERROR)
            {
                return FSAStatus.ERROR;
            }
            return FSAStatus.RUNNING;
        }

        public override Token RetrieveToken()
        {
            string name = _scanned.Substring(0, _scanned.Length - 1);
            if (TokenPreprocessor.PreprocessorKeywords.ContainsKey(name))
            {
                return new TokenPreprocessor(TokenPreprocessor.PreprocessorKeywords[name]);
            }
            return new EmptyToken();
        }
        public Token RetrieveToken(string keyword)
        {
            string name = keyword.Substring(0, keyword.Length);
            if (TokenPreprocessor.PreprocessorKeywords.ContainsKey(name))
            {
                return new TokenPreprocessor(TokenPreprocessor.PreprocessorKeywords[name]);
            }
            return new EmptyToken();
        }

        public override void ReadChar(char ch)
        {
            _scanned = _scanned + ch;
            switch (_state)
            {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (ch == '#')
                    {
                        _state = State.ID;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.ID:
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        _state = State.ID;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
            }
        }

        public override void ReadEOF()
        {
            _scanned = _scanned + '0';
            switch (_state)
            {
                case State.ID:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }
    }
}