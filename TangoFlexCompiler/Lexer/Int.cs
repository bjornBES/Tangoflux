using System;

namespace TangoFlexCompiler.Lexer
{
    /// <summary>
    /// There are four types of integers: signed, unsigned, signed long, unsigned long
    /// </summary>
    public sealed class TokenInt : Token
    {
        public enum IntSuffix
        {
            NONE,
            U,
            L,
            UL
        };

        public TokenInt(long val, IntSuffix suffix, string raw)
        {
            RawDigits = raw;
            Base = 10;
            Val = val;
            Suffix = suffix;
            Raw = raw;
        }

        public TokenInt(long val, IntSuffix suffix, string raw, int _base, string rawDigits)
        {
            RawDigits = rawDigits;
            Base = _base;
            Val = val;
            Suffix = suffix;
            Raw = raw;
        }

        public override TokenKind Kind { get; } = TokenKind.INT;

        public override string ToString()
        {
            string str = $"({File}:{Line}:{Column}): " + Kind.ToString();
            switch (Suffix)
            {
                case IntSuffix.L:
                    str += "(long)";
                    break;
                case IntSuffix.U:
                    str += "(unsigned)";
                    break;
                case IntSuffix.UL:
                    str += "(unsigned long)";
                    break;
                default:
                    break;
            }
            return str + ": base " + Base + " " + Val + " \"" + Raw + "\" & " + RawDigits;
        }
        public override Token Clone()
        {
            return new TokenInt(Val, Suffix, Raw, Base, RawDigits);
        }


        public override object GetData()
        {
            return Raw;
        }

        public readonly string RawDigits;
        public readonly int Base;
        public readonly long Val;
        public readonly string Raw;
        public readonly IntSuffix Suffix;
    }

    public sealed class FSAInt : FSA
    {
        private enum State
        {
            START,
            END,
            ERROR,
            Z,
            O,
            D,
            ZX,
            H,
            L,
            U,
            UL,
            ZB,
            B,
        };

        private long _val;
        private string _raw;
        private TokenInt.IntSuffix _suffix;
        private State _state;
        private int _base;
        private string _rawDigits;
        private int _digitStart;

        public FSAInt()
        {
            _state = State.START;
            _val = 0;
            _raw = "";
            _suffix = TokenInt.IntSuffix.NONE;
            _digitStart = 0;
            _base = 10;
        }

        public override void Reset()
        {
            _state = State.START;
            _val = 0;
            _raw = "";
            _suffix = TokenInt.IntSuffix.NONE;
            _digitStart = 0;
            _base = 10;
        }

        public override FSAStatus GetStatus()
        {
            switch (_state)
            {
                case State.START:
                    return FSAStatus.NONE;
                case State.END:
                    return FSAStatus.END;
                case State.ERROR:
                    return FSAStatus.ERROR;
                default:
                    return FSAStatus.RUNNING;
            }
        }

        public override Token RetrieveToken()
        {
            int suffixLength = 0;
            if (_suffix != TokenInt.IntSuffix.NONE)
            {
                suffixLength = _suffix.ToString().Length;
            }
            _rawDigits = _raw.Substring(_digitStart, _raw.Length - _digitStart - suffixLength);
            _rawDigits = _rawDigits.Replace("_", "");
            return new TokenInt(_val, _suffix, _raw, _base, _rawDigits);
        }

        public override void ReadChar(char ch)
        {
            _raw += ch;
            switch (_state)
            {
                case State.ERROR:
                case State.END:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (ch == '0')
                    {
                        _state = State.Z;
                    }
                    else if (char.IsDigit(ch))
                    {
                        _digitStart = 0;
                        _state = State.D;
                        _val += ch - '0';
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.Z:
                    if (ch == '_')
                    {
                        _val += 0;
                        _state = State.D;
                    }
                    else if (ch == 'x' || ch == 'X')
                    {
                        _digitStart = 2;
                        _base = 16;
                        _state = State.ZX;
                    }
                    else if (ch == 'b' || ch == 'B')
                    {
                        _digitStart = 2;
                        _base = 2;
                        _state = State.ZB;
                    }
                    else if (Utils.IsOctDigit(ch))
                    {
                        _digitStart = 1;
                        _base = 8;
                        _val *= 8;
                        _val += ch - '0';
                        _state = State.O;
                    }
                    else if (ch == 'u' || ch == 'U')
                    {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    }
                    else if (ch == 'l' || ch == 'L')
                    {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.D:
                    if (ch == '_')
                    {
                        _state = State.D;
                    }
                    else if (char.IsDigit(ch))
                    {
                        _val *= 10;
                        _val += ch - '0';
                        _state = State.D;
                    }
                    else if (ch == 'u' || ch == 'U')
                    {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    }
                    else if (ch == 'l' || ch == 'L')
                    {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.ZX:
                    if (ch == '_')
                    {
                        _state = State.B;
                    }
                    else if (Utils.IsHexDigit(ch))
                    {
                        checked
                        {
                            _val *= 0x10;
                            _val += Utils.GetHexDigit(ch);
                        }
                        _state = State.H;
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.O:
                    if (Utils.IsOctDigit(ch))
                    {
                        _val *= 8;
                        _val += ch - '0';
                        _state = State.O;
                    }
                    else if (ch == '_')
                    {
                        _state = State.O;
                    }
                    else if (ch == 'u' || ch == 'U')
                    {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    }
                    else if (ch == 'l' || ch == 'L')
                    {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.L:
                    if (ch == 'u' || ch == 'U')
                    {
                        _suffix = TokenInt.IntSuffix.UL;
                        _state = State.UL;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.H:
                    if (Utils.IsHexDigit(ch))
                    {
                        _val *= 0x10;
                        _val += Utils.GetHexDigit(ch);
                        _state = State.H;
                    }
                    else if (ch == '_')
                    {
                        _state = State.H;
                    }
                    else if (ch == 'u' || ch == 'U')
                    {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    }
                    else if (ch == 'l' || ch == 'L')
                    {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.U:
                    if (ch == 'l' || ch == 'L')
                    {
                        _suffix = TokenInt.IntSuffix.UL;
                        _state = State.UL;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.UL:
                    _state = State.END;
                    break;
                case State.ZB:
                case State.B:
                    if (ch == '0' || ch == '1')
                    {
                        _val *= 2;
                        _val += ch - '0';
                        _state = State.B;
                    }
                    else if (ch == '_')
                    {
                        _state = State.B;
                    }
                    else if (ch == 'u' || ch == 'U')
                    {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    }
                    else if (ch == 'l' || ch == 'L')
                    {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;

                default:
                    _state = State.ERROR;
                    break;
            }
        }

        public override void ReadEOF()
        {
            switch (_state)
            {
                case State.D:
                case State.Z:
                case State.O:
                case State.L:
                case State.H:
                case State.U:
                case State.B:
                case State.UL:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

    }
}