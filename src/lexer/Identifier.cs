using System;

namespace CompilerTangoFlex.lexer
{
    /// <summary>
    /// If the identifier is found to be a keyword, then it will be a keyword
    /// </summary>
    public sealed class TokenIdentifier : Token
    {
        public TokenIdentifier(string val)
        {
            Val = val;
        }

        public override TokenKind Kind { get; } = TokenKind.IDENTIFIER;
        public string Val { get; }
        public override string ToString()
        {
            return $"({File}:{Line}:{Column}): " + Kind + ": " + Val;
        }
        public override Token Clone()
        {
            return new TokenIdentifier(Val);
        }


        public override object GetData()
        {
            return Val;
        }
    }

    public sealed class FSAIdentifier : FSA
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

        public FSAIdentifier()
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
            if (TokenKeyword.Keywords.ContainsKey(name))
            {
                return new TokenKeyword(TokenKeyword.Keywords[name]);
            }
            return new TokenIdentifier(name);
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
                    if (ch == '_' || char.IsLetter(ch))
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