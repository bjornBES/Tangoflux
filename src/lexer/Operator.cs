using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CompilerTangoFlex.lexer {
    /// <summary>
    /// Note that '...' is recognized as three '.'s
    /// </summary>
    public enum OperatorVal {
        LBRACKET,
        RBRACKET,
        LPAREN,
        RPAREN,
        PERIOD,
        COMMA,
        QUESTION,
        COLON,
        TILDE,
        SUB,
        RARROW,
        DEC,
        SUBASSIGN,
        ADD,
        INC,
        ADDASSIGN,
        BITAND,
        AND,
        ANDASSIGN,
        MULT,
        MULTASSIGN,
        LT,
        LEQ,
        LSHIFT,
        LSHIFTASSIGN,
        GT,
        GEQ,
        RSHIFT,
        RSHIFTASSIGN,
        ASSIGN,
        EQ,
        BITOR,
        OR,
        ORASSIGN,
        NOT,
        NEQ,
        DIV,
        DIVASSIGN,
        MOD,
        MODASSIGN,
        XOR,
        XORASSIGN,
        SEMICOLON,
        LCURL,
        RCURL
    }

    public sealed class TokenOperator : Token {
        public TokenOperator(OperatorVal val) {
            Val = val;
        }

        public override TokenKind Kind { get; } = TokenKind.OPERATOR;
        public OperatorVal Val { get; }

        public static Dictionary<string, OperatorVal> Operators { get; } = new Dictionary<string, OperatorVal> {
            { "[",    OperatorVal.LBRACKET     },
            { "]",    OperatorVal.RBRACKET     },
            { "(",    OperatorVal.LPAREN       },
            { ")",    OperatorVal.RPAREN       },
            { ".",    OperatorVal.PERIOD       },
            { ",",    OperatorVal.COMMA        },
            { "?",    OperatorVal.QUESTION     },
            { ":",    OperatorVal.COLON        },
            { "~",    OperatorVal.TILDE        },
            { "-",    OperatorVal.SUB          },
            { "->",   OperatorVal.RARROW       },
            { "--",   OperatorVal.DEC          },
            { "-=",   OperatorVal.SUBASSIGN    },
            { "+",    OperatorVal.ADD          },
            { "++",   OperatorVal.INC          },
            { "+=",   OperatorVal.ADDASSIGN    },
            { "&",    OperatorVal.BITAND       },
            { "&&",   OperatorVal.AND          },
            { "&=",   OperatorVal.ANDASSIGN    },
            { "*",    OperatorVal.MULT         },
            { "*=",   OperatorVal.MULTASSIGN   },
            { "<",    OperatorVal.LT           },
            { "<=",   OperatorVal.LEQ          },
            { "<<",   OperatorVal.LSHIFT       },
            { "<<=",  OperatorVal.LSHIFTASSIGN },
            { ">",    OperatorVal.GT           },
            { ">=",   OperatorVal.GEQ          },
            { ">>",   OperatorVal.RSHIFT       },
            { ">>=",  OperatorVal.RSHIFTASSIGN },
            { "=",    OperatorVal.ASSIGN       },
            { "==",   OperatorVal.EQ           },
            { "|",    OperatorVal.BITOR        },
            { "||",   OperatorVal.OR           },
            { "|=",   OperatorVal.ORASSIGN     },
            { "!",    OperatorVal.NOT          },
            { "!=",   OperatorVal.NEQ          },
            { "/",    OperatorVal.DIV          },
            { "/=",   OperatorVal.DIVASSIGN    },
            { "%",    OperatorVal.MOD          },
            { "%=",   OperatorVal.MODASSIGN    },
            { "^",    OperatorVal.XOR          },
            { "^=",   OperatorVal.XORASSIGN    },
            { ";",    OperatorVal.SEMICOLON    },
            { "{",    OperatorVal.LCURL        },
            { "}",    OperatorVal.RCURL        }
        };

        public override string ToString() {
            return $"({Line}:{Column}): " + Kind + " [" + Val + "]: " + Operators.First(pair => pair.Value == Val).Key;
        }
    }

    public class FSAOperator : FSA {
        private enum State {
            START,
            END,
            ERROR,
            FINISH,
            SUB,
            ADD,
            AMP,
            MULT,
            LT,
            LTLT,
            GT,
            GTGT,
            EQ,
            OR,
            NOT,
            DIV,
            MOD,
            XOR
        };

        public static ImmutableHashSet<char> OperatorChars { get; } = ImmutableHashSet.Create(
            '[',
            ']',
            '(',
            ')',
            '.',
            ',',
            '?',
            ':',
            '-',
            '>',
            '+',
            '&',
            '*',
            '~',
            '!',
            '/',
            '%',
            '<',
            '=',
            '^',
            '|',
            ';',
            '{',
            '}'
            );

        private State _state;
        private string _scanned;

        public FSAOperator() {
            _state = State.START;
            _scanned = "";
        }

        public override sealed void Reset() {
            _state = State.START;
            _scanned = "";
        }

        public override sealed FSAStatus GetStatus() {
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

        public override sealed Token RetrieveToken() {
            return new TokenOperator(TokenOperator.Operators[_scanned.Substring(0, _scanned.Length - 1)]);
        }

        public override sealed void ReadChar(char ch) {
            _scanned = _scanned + ch;
            switch (_state) {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (OperatorChars.Contains(ch)) {
                        switch (ch) {
                            case '-':
                                _state = State.SUB;
                                break;
                            case '+':
                                _state = State.ADD;
                                break;
                            case '&':
                                _state = State.AMP;
                                break;
                            case '*':
                                _state = State.MULT;
                                break;
                            case '<':
                                _state = State.LT;
                                break;
                            case '>':
                                _state = State.GT;
                                break;
                            case '=':
                                _state = State.EQ;
                                break;
                            case '|':
                                _state = State.OR;
                                break;
                            case '!':
                                _state = State.NOT;
                                break;
                            case '/':
                                _state = State.DIV;
                                break;
                            case '%':
                                _state = State.MOD;
                                break;
                            case '^':
                                _state = State.XOR;
                                break;
                            default:
                                _state = State.FINISH;
                                break;
                        }
                    } else {
                        _state = State.ERROR;
                    }
                    break;
                case State.FINISH:
                    _state = State.END;
                    break;
                case State.SUB:
                    switch (ch) {
                        case '>':
                        case '-':
                        case '=':
                            _state = State.FINISH;
                            break;
                        default:
                            _state = State.END;
                            break;
                    }
                    break;
                case State.ADD:
                    switch (ch) {
                        case '+':
                        case '=':
                            _state = State.FINISH;
                            break;
                        default:
                            _state = State.END;
                            break;
                    }
                    break;
                case State.AMP:
                    switch (ch) {
                        case '&':
                        case '=':
                            _state = State.FINISH;
                            break;
                        default:
                            _state = State.END;
                            break;
                    }
                    break;
                case State.MULT:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.LT:
                    switch (ch) {
                        case '=':
                            _state = State.FINISH;
                            break;
                        case '<':
                            _state = State.LTLT;
                            break;
                        default:
                            _state = State.END;
                            break;
                    }
                    break;
                case State.GT:
                    switch (ch) {
                        case '=':
                            _state = State.FINISH;
                            break;
                        case '>':
                            _state = State.GTGT;
                            break;
                        default:
                            _state = State.END;
                            break;
                    }
                    break;
                case State.EQ:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.OR:
                    switch (ch) {
                        case '|':
                        case '=':
                            _state = State.FINISH;
                            break;
                        default:
                            _state = State.END;
                            break;
                    }
                    break;
                case State.NOT:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.DIV:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.MOD:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.XOR:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.LTLT:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                case State.GTGT:
                    if (ch == '=') {
                        _state = State.FINISH;
                    } else {
                        _state = State.END;
                    }
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

        public override sealed void ReadEOF() {
            _scanned = _scanned + '0';
            switch (_state) {
                case State.FINISH:
                case State.SUB:
                case State.ADD:
                case State.AMP:
                case State.MULT:
                case State.LT:
                case State.LTLT:
                case State.GT:
                case State.GTGT:
                case State.EQ:
                case State.OR:
                case State.NOT:
                case State.DIV:
                case State.MOD:
                case State.XOR:
                    _state = State.END;
                    break;
                default:
                    _state = State.ERROR;
                    break;
            }
        }

    }
}