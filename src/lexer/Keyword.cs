using System;
using System.Collections.Generic;

namespace CompilerTangoFlex.lexer {
    public enum KeywordVal {
        FUNC, VAR, RETURN,
IF, ELSE, WHILE,
TRUE, FALSE,
INT, FLOAT, BOOL, STRING, VOID,
        
/*
        AUTO,
        DOUBLE,
        STRUCT,
        BREAK,
        LONG,
        SWITCH,
        CASE,
        ENUM,
        REGISTER,
        TYPEDEF,
        CHAR,
        EXTERN,
        UNION,
        CONST,
        SHORT,
        UNSIGNED,
        CONTINUE,
        FOR,
        SIGNED,
        VOID,
        DEFAULT,
        GOTO,
        SIZEOF,
        VOLATILE,
        DO,
        STATIC,
 */
    }

    public class TokenKeyword : Token {
        public TokenKeyword(KeywordVal val) {
            Val = val;
        }

        public override TokenKind Kind { get; } = TokenKind.KEYWORD;
        public KeywordVal Val { get; }
        public static Dictionary<string, KeywordVal> Keywords { get; } = new Dictionary<string, KeywordVal>(StringComparer.InvariantCultureIgnoreCase) {
            { "INT",         KeywordVal.INT       },
            { "ELSE",        KeywordVal.ELSE      },
            { "RETURN",      KeywordVal.RETURN    },
            { "FLOAT",       KeywordVal.FLOAT     },
            { "IF",          KeywordVal.IF        },
            { "WHILE",       KeywordVal.WHILE     },
            { "FUNC",        KeywordVal.FUNC      },
            { "VAR",         KeywordVal.VAR       },
            { "TRUE",        KeywordVal.TRUE      },
            { "FALSE",       KeywordVal.FALSE     },
            { "STRING",      KeywordVal.STRING    },
            { "BOOL",        KeywordVal.BOOL      },
            /*
            { "AUTO",        KeywordVal.AUTO      },
            { "DOUBLE",      KeywordVal.DOUBLE    },
            { "STRUCT",      KeywordVal.STRUCT    },
            { "BREAK",       KeywordVal.BREAK     },
            { "LONG",        KeywordVal.LONG      },
            { "SWITCH",      KeywordVal.SWITCH    },
            { "CASE",        KeywordVal.CASE      },
            { "ENUM",        KeywordVal.ENUM      },
            { "REGISTER",    KeywordVal.REGISTER  },
            { "TYPEDEF",     KeywordVal.TYPEDEF   },
            { "CHAR",        KeywordVal.CHAR      },
            { "EXTERN",      KeywordVal.EXTERN    },
            { "UNION",       KeywordVal.UNION     },
            { "CONST",       KeywordVal.CONST     },
            { "SHORT",       KeywordVal.SHORT     },
            { "UNSIGNED",    KeywordVal.UNSIGNED  },
            { "CONTINUE",    KeywordVal.CONTINUE  },
            { "FOR",         KeywordVal.FOR       },
            { "SIGNED",      KeywordVal.SIGNED    },
            { "VOID",        KeywordVal.VOID      },
            { "DEFAULT",     KeywordVal.DEFAULT   },
            { "GOTO",        KeywordVal.GOTO      },
            { "SIZEOF",      KeywordVal.SIZEOF    },
            { "VOLATILE",    KeywordVal.VOLATILE  },
            { "DO",          KeywordVal.DO        },
            { "STATIC",      KeywordVal.STATIC    },
             */
        };

        public override string ToString() {
            return $"({Line}:{Column}): " + Kind + ": " + Val;
        }

    }
}