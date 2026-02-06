using System;
using System.Reflection.PortableExecutable;

namespace TangoFlexCompiler.Lexer
{
    /// <summary>
    /// String literal
    /// </summary>
    public sealed class TokenString : Token
    {
        public enum StringPrefix
        {
            NONE,
            U8,
            U16,
            U32,
        };
        public TokenString(string val, StringPrefix prefix, string raw)
        {
            if (val == null)
            {
                throw new ArgumentNullException(nameof(val));
            }
            Prefix = prefix;
            Val = val;
            Raw = raw;
        }

        public override TokenKind Kind { get; } = TokenKind.STRING;
        public StringPrefix Prefix { get; }
        public string Raw { get; }
        public string Val { get; }

        public override string ToString()
        {
            string str = $"({File}:{Line}:{Column}): " +Kind.ToString();
            switch (Prefix) {
                case StringPrefix.U32:
                    str += "(u32)";
                    break;
                case StringPrefix.U16:
                    str += "(u16) ";
                    break;
                case StringPrefix.U8:
                    str += "(u8) ";
                    break;
                default:
                    break;
            }
            return str + ": " + $"\"{Raw}\" \"{Val}\"";
        }
        public override Token Clone()
        {
            return new TokenString(Val, Prefix, Raw);
        }

        public override object GetData()
        {
            return Raw;
        }
    }

    public sealed class FSAString : FSA
    {
        private enum State
        {
            START,
            END,
            ERROR,
            GetPrefix,
            L,
            Q,
            QQ
        };

        private State _state;
        private readonly FSAChar _fsachar;
        private string _val;
        private string _raw;
        private TokenString.StringPrefix _prefix;

        public FSAString()
        {
            _state = State.START;
            _fsachar = new FSAChar('\"');
            _raw = "";
            _val = "";
            _prefix = TokenString.StringPrefix.NONE;
        }

        public override void Reset()
        {
            _state = State.START;
            _fsachar.Reset();
            _raw = "";
            _val = "";
            _prefix = TokenString.StringPrefix.NONE;
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
            return new TokenString(_val, _prefix, _raw);
        }

        public override void ReadChar(char ch)
        {
            switch (_state)
            {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    switch (ch)
                    {
                        case 'u':
                        case 'U':
                        _fsachar.Reset();
                            _state = State.GetPrefix;
                            break;
                        case 'L':
                            _state = State.L;
                            break;
                        case '\"':
                            _prefix = TokenString.StringPrefix.U8;
                            _state = State.Q;
                            _fsachar.Reset();
                            break;
                        default:
                            _state = State.ERROR;
                            break;
                    }
                    break;
                case State.GetPrefix:
                    switch (ch)
                    {
                        case '\"':
                            string strPreFix = _fsachar.RetrieveRaw();
                            if (strPreFix.Equals("u8", StringComparison.OrdinalIgnoreCase))
                            {
                                _prefix = TokenString.StringPrefix.U8;
                            }
                            else if (strPreFix.Equals("u16", StringComparison.OrdinalIgnoreCase))
                            {
                                _prefix = TokenString.StringPrefix.U16;
                            }
                            else if (strPreFix.Equals("u32", StringComparison.OrdinalIgnoreCase))
                            {
                                _prefix = TokenString.StringPrefix.U32;
                            }
                            _state = State.Q;
                            _fsachar.Reset();
                            break;
                        default:
                            _fsachar.ReadChar(ch);
                            break;
                    }
                    break;
                case State.L:
                    if (ch == '\"')
                    {
                        _state = State.Q;
                        _fsachar.Reset();
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.Q:
                    if (_fsachar.GetStatus() == FSAStatus.NONE && ch == '\"')
                    {
                        _state = State.QQ;
                        _fsachar.Reset();
                    }
                    else
                    {
                        _fsachar.ReadChar(ch);
                        switch (_fsachar.GetStatus())
                        {
                            case FSAStatus.END:
                                _state = State.Q;
                                char c = _fsachar.RetrieveChar();
                                _val = _val + c;
                                _raw = _raw + _fsachar.RetrieveRaw();
                                _fsachar.Reset();
                                ReadChar(ch);
                                break;
                            case FSAStatus.ERROR:
                                _state = State.ERROR;
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case State.QQ:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

        public override void ReadEOF()
        {
            if (_state == State.QQ)
            {
                _state = State.END;
            }
            else
            {
                _state = State.ERROR;
            }
        }

    }
}