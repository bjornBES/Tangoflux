using System;

namespace CompilerTangoFlex.lexer {
    /// <summary>
    /// String literal
    /// </summary>
    public sealed class TokenString : Token {
        public TokenString(string val, string raw) {
            if (val == null) {
                throw new ArgumentNullException(nameof(val));
            }
            Val = val;
            Raw = raw;
        }

        public override TokenKind Kind { get; } = TokenKind.STRING;
        public string Raw { get; }
        public string Val { get; }

        public override string ToString() =>
            $"({Line}:{Column}): " + $"{Kind}: \"{Raw}\"\n\"{Val}\"";
    }

    public sealed class FSAString : FSA {
        private enum State {
            START,
            END,
            ERROR,
            L,
            Q,
            QQ
        };

        private State _state;
        private readonly FSAChar _fsachar;
        private string _val;
        private string _raw;

        public FSAString() {
            _state = State.START;
            _fsachar = new FSAChar('\"');
            _raw = "";
            _val = "";
        }

        public override void Reset() {
            _state = State.START;
            _fsachar.Reset();
            _raw = "";
            _val = "";
        }

        public override FSAStatus GetStatus() {
            if (_state == State.START) {
                return FSAStatus.NONE;
            }
            if (_state == State.END) {
                return FSAStatus.END;
            }
            if (_state == State.ERROR) {
                return FSAStatus.ERROR;
            }
            return FSAStatus.RUNNING;
        }

        public override Token RetrieveToken() {
            return new TokenString(_val, _raw);
        }

        public override void ReadChar(char ch) {
            switch (_state) {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    switch (ch) {
                        case 'L':
                            _state = State.L;
                            break;
                        case '\"':
                            _state = State.Q;
                            _fsachar.Reset();
                            break;
                        default:
                            _state = State.ERROR;
                            break;
                    }
                    break;
                case State.L:
                    if (ch == '\"') {
                        _state = State.Q;
                        _fsachar.Reset();
                    } else {
                        _state = State.ERROR;
                    }
                    break;
                case State.Q:
                    if (_fsachar.GetStatus() == FSAStatus.NONE && ch == '\"') {
                        _state = State.QQ;
                        _fsachar.Reset();
                    } else {
                        _fsachar.ReadChar(ch);
                        switch (_fsachar.GetStatus()) {
                            case FSAStatus.END:
                                _state = State.Q;
                                _val = _val + _fsachar.RetrieveChar();
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

        public override void ReadEOF() {
            if (_state == State.QQ) {
                _state = State.END;
            } else {
                _state = State.ERROR;
            }
        }

    }
}