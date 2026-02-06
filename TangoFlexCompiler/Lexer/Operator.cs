using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TangoFlexCompiler.Lexer
{
    /// <summary>
    /// Note that '...' is recognized as three '.'s
    /// </summary>
    public enum OperatorVal
    {
        LBRACKET,
        RBRACKET,
        LEFTPAREN,
        RIGHTPAREN,
        PERIOD,
        COMMA,
        QUESTION,
        COLON,
        TILDE,
        SUB,
        // >
        RIGHTARROW,
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
        LEFTSHIFT,
        LEFTSHIFTASSIGN,
        GT,
        GEQ,
        RIGHTSHIFT,
        RIGHTSHIFTASSIGN,
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
        LEFTCURL,
        RIGHTCURL,
        HASH,
        INTRINSICS
    }

    public sealed class TokenOperator : Token
    {
        public TokenOperator(OperatorVal val)
        {
            Val = val;
        }

        public override TokenKind Kind { get; } = TokenKind.OPERATOR;
        public OperatorVal Val { get; }

        public static Dictionary<string, OperatorVal> Operators { get; } = new Dictionary<string, OperatorVal> {
            { "[",    OperatorVal.LBRACKET     },
            { "]",    OperatorVal.RBRACKET     },
            { "(",    OperatorVal.LEFTPAREN    },
            { ")",    OperatorVal.RIGHTPAREN   },
            { ".",    OperatorVal.PERIOD       },
            { ",",    OperatorVal.COMMA        },
            { "?",    OperatorVal.QUESTION     },
            { ":",    OperatorVal.COLON        },
            { "~",    OperatorVal.TILDE        },
            { "-",    OperatorVal.SUB          },
            { "->",   OperatorVal.RIGHTARROW   },
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
            { "<<",   OperatorVal.LEFTSHIFT       },
            { "<<=",  OperatorVal.LEFTSHIFTASSIGN },
            { ">",    OperatorVal.GT           },
            { ">=",   OperatorVal.GEQ          },
            { ">>",   OperatorVal.RIGHTSHIFT       },
            { ">>=",  OperatorVal.RIGHTSHIFTASSIGN },
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
            { "{",    OperatorVal.LEFTCURL        },
            { "}",    OperatorVal.RIGHTCURL        },
            { "#",    OperatorVal.HASH         },
            { "@",    OperatorVal.INTRINSICS   },
        };

        public override string ToString()
        {
            return $"({File}:{Line}:{Column}): " + Kind + " [" + Val + "]: " + Operators.First(pair => pair.Value == Val).Key;
        }

        public override Token Clone()
        {
            return new TokenOperator(Val);
        }

        public override object GetData()
        {
            int valueIndex = Operators.Values.ToList().IndexOf(Val);
            return Operators.Keys.ElementAt(valueIndex).ToString();
        }

        public int binPrecedence()
        {
            switch (Val)
            {
                case OperatorVal.MULT:
                case OperatorVal.DIV:
                case OperatorVal.MOD:
                    return 7;
                case OperatorVal.ADD:
                case OperatorVal.SUB:
                    return 6;
                case OperatorVal.LT:
                case OperatorVal.LEQ:
                case OperatorVal.GT:
                case OperatorVal.GEQ:
                    return 5;
                case OperatorVal.EQ:
                case OperatorVal.NEQ:
                    return 4;
                case OperatorVal.BITAND:
                    return 3;
                case OperatorVal.XOR:
                    return 2;
                case OperatorVal.BITOR:
                    return 1;
                case OperatorVal.AND:
                    return 0;
                case OperatorVal.OR:
                    return -1;
                default:
                    return -2;
            }
        }
        public bool IsCompound()
        {
            switch (Val)
            {
                case OperatorVal.ADDASSIGN:
                case OperatorVal.SUBASSIGN:
                case OperatorVal.MULTASSIGN:
                case OperatorVal.DIVASSIGN:
                case OperatorVal.MODASSIGN:
                case OperatorVal.ANDASSIGN:
                case OperatorVal.ORASSIGN:
                case OperatorVal.XORASSIGN:
                case OperatorVal.LEFTSHIFTASSIGN:
                case OperatorVal.RIGHTSHIFTASSIGN:
                    return true;
                default:
                    return false;
            }
        }

        internal OperatorVal FromAssign()
        {
            switch (Val)
            {
                case OperatorVal.ADDASSIGN:
                    return OperatorVal.ADD;
                case OperatorVal.SUBASSIGN:
                    return OperatorVal.SUB;
                case OperatorVal.MULTASSIGN:
                    return OperatorVal.MULT;
                case OperatorVal.DIVASSIGN:
                    return OperatorVal.DIV;
                case OperatorVal.MODASSIGN:
                    return OperatorVal.MOD;
                case OperatorVal.ANDASSIGN:
                    return OperatorVal.BITAND;
                case OperatorVal.ORASSIGN:
                    return OperatorVal.BITOR;
                case OperatorVal.XORASSIGN:
                    return OperatorVal.XOR;
                case OperatorVal.LEFTSHIFTASSIGN:
                    return OperatorVal.LEFTSHIFT;
                case OperatorVal.RIGHTSHIFTASSIGN:
                    return OperatorVal.RIGHTSHIFT;
                default:
                    throw new Exception("Not a compound assignment operator");
            }
        }

        internal bool IsUnary()
        {
            switch (Val)
            {
                case OperatorVal.ADD:
                case OperatorVal.SUB:
                case OperatorVal.BITAND:
                    return true;
            }
            return false;
        }
    }

    public class FSAOperator : FSA
    {
        private enum State
        {
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
            '}',
            '#',
            '@'
            );

        private State _state;
        public string _scanned;

        public FSAOperator()
        {
            _state = State.START;
            _scanned = "";
        }

        public override sealed void Reset()
        {
            _state = State.START;
            _scanned = "";
        }

        public override sealed FSAStatus GetStatus()
        {
            switch (_state)
            {
                case State.START:
                    return FSAStatus.NONE;
                case State.END:
                    return FSAStatus.END;
                case State.ERROR:
                    return FSAStatus.ERROR;
                case State.FINISH:
                    return FSAStatus.FINISH;
                default:
                    return FSAStatus.RUNNING;
            }
        }

        public override sealed Token RetrieveToken()
        {
            if (TokenOperator.Operators.ContainsKey(_scanned))
            {
                return new TokenOperator(TokenOperator.Operators[_scanned]);
            }
            return new TokenOperator(TokenOperator.Operators[_scanned.Substring(0, _scanned.Length - 1)]);
        }

        public override sealed void ReadChar(char ch)
        {
            _scanned = _scanned + ch;
            switch (_state)
            {
                case State.END:
                case State.ERROR:
                    _state = State.ERROR;
                    break;
                case State.START:
                    if (OperatorChars.Contains(ch))
                    {
                        switch (ch)
                        {
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
                    }
                    else
                    {
                        _state = State.ERROR;
                    }
                    break;
                case State.FINISH:
                    _state = State.END;
                    break;
                case State.SUB:
                    switch (ch)
                    {
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
                    switch (ch)
                    {
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
                    switch (ch)
                    {
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
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.LT:
                    switch (ch)
                    {
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
                    switch (ch)
                    {
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
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.OR:
                    switch (ch)
                    {
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
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.DIV:
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.MOD:
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.XOR:
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.LTLT:
                    if (ch == '=')
                    {
                        _state = State.FINISH;
                    }
                    else
                    {
                        _state = State.END;
                    }
                    break;
                case State.GTGT:
                    if (ch == '=')
                    {
                        _state = State.FINISH;
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

        public override sealed void ReadEOF()
        {
            _scanned = _scanned + '0';
            switch (_state)
            {
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