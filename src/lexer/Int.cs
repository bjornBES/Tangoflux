using System;

namespace CompilerTangoFlex.lexer {
    /// <summary>
    /// There are four types of integers: signed, unsigned, signed long, unsigned long
    /// </summary>
    public sealed class TokenInt : Token {
        public enum IntSuffix {
            NONE,
            U,
            L,
            UL
        };

        public TokenInt(long val, IntSuffix suffix, string raw) {
            Val = val;
            Suffix = suffix;
            Raw = raw;
        }

        public override TokenKind Kind { get; } = TokenKind.INT;

        public override string ToString() {
            string str = $"({Line}:{Column}): " +Kind.ToString();
            switch (Suffix) {
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
            return str + ": " + Val + " \"" + Raw + "\"";
        }
        public override Token Clone()
        {
            return new TokenInt(Val, Suffix, Raw);
        }

        public readonly long Val;
        public readonly string Raw;
        public readonly IntSuffix Suffix;
    }

    public sealed class FSAInt : FSA {
        private enum State {
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
            UL
        };

        private long _val;
        private string _raw;
        private TokenInt.IntSuffix _suffix;
        private State _state;

        public FSAInt() {
            _state = State.START;
            _val = 0;
            _raw = "";
            _suffix = TokenInt.IntSuffix.NONE;
        }

        public override void Reset() {
            _state = State.START;
            _val = 0;
            _raw = "";
            _suffix = TokenInt.IntSuffix.NONE;
        }

        public override FSAStatus GetStatus() {
            switch (_state) {
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

        public override Token RetrieveToken() {
            return new TokenInt(_val, _suffix, _raw.Substring(0, _raw.Length - 1));
        }

        public override void ReadChar(char ch) {
            _raw += ch;
            switch (_state) {
                case State.ERROR:
                case State.END:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (ch == '0') {
                        _state = State.Z;
                    } else if (char.IsDigit(ch)) {
                        _state = State.D;
                        _val += ch - '0';
                    } else {
                        _state = State.ERROR;
                    }
                    break;
                case State.Z:
                    if (ch == 'x' || ch == 'X') {
                        _state = State.ZX;
                    } else if (Utils.IsOctDigit(ch)) {
                        _val *= 8;
                        _val += ch - '0';
                        _state = State.O;
                    } else if (ch == 'u' || ch == 'U') {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    } else if (ch == 'l' || ch == 'L') {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.D:
                    if (char.IsDigit(ch)) {
                        _val *= 10;
                        _val += ch - '0';
                        _state = State.D;
                    } else if (ch == 'u' || ch == 'U') {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    } else if (ch == 'l' || ch == 'L') {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.ZX:
                    if (Utils.IsHexDigit(ch)) {
                        _val *= 0x10;
                        _val += Utils.GetHexDigit(ch);
                        _state = State.H;
                    } else {
                        _state = State.ERROR;
                    }
                    break;
                case State.O:
                    if (Utils.IsOctDigit(ch)) {
                        _val *= 8;
                        _val += ch - '0';
                        _state = State.O;
                    } else if (ch == 'u' || ch == 'U') {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    } else if (ch == 'l' || ch == 'L') {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.L:
                    if (ch == 'u' || ch == 'U') {
                        _suffix = TokenInt.IntSuffix.UL;
                        _state = State.UL;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.H:
                    if (Utils.IsHexDigit(ch)) {
                        _val *= 0x10;
                        _val += Utils.GetHexDigit(ch);
                        _state = State.H;
                    } else if (ch == 'u' || ch == 'U') {
                        _suffix = TokenInt.IntSuffix.U;
                        _state = State.U;
                    } else if (ch == 'l' || ch == 'L') {
                        _suffix = TokenInt.IntSuffix.L;
                        _state = State.L;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.U:
                    if (ch == 'l' || ch == 'L') {
                        _suffix = TokenInt.IntSuffix.UL;
                        _state = State.UL;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.UL:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

        public override void ReadEOF() {
            switch (_state) {
                case State.D:
                case State.Z:
                case State.O:
                case State.L:
                case State.H:
                case State.U:
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