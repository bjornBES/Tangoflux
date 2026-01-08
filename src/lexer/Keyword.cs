using System;
using System.Collections.Generic;

namespace CompilerTangoFlex.lexer {
    public enum KeywordVal {
        NONE = 0,
        FUNC, VAR, RETURN,
        IF, ELSE, WHILE,
        TRUE, FALSE,
        INT8, UINT8,
        INT16, UINT16,
        INT, INT32 = INT, UINT32,
        INT64, UINT64,
        FLOAT, BOOL, STRING, VOID,
        PTR,
        
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
            { "INT",        KeywordVal.INT      },
            { "ELSE",       KeywordVal.ELSE     },
            { "RETURN",     KeywordVal.RETURN   },
            { "FLOAT",      KeywordVal.FLOAT    },
            { "IF",         KeywordVal.IF       },
            { "WHILE",      KeywordVal.WHILE    },
            { "FUNC",       KeywordVal.FUNC     },
            { "VAR",        KeywordVal.VAR      },
            { "TRUE",       KeywordVal.TRUE     },
            { "FALSE",      KeywordVal.FALSE    },
            { "STRING",     KeywordVal.STRING   },
            { "BOOL",       KeywordVal.BOOL     },
            { "PTR",        KeywordVal.PTR      },
            { "INT8",       KeywordVal.INT8     },
            { "UINT8",      KeywordVal.UINT8    },
            { "INT16",      KeywordVal.INT16    },
            { "UINT16",     KeywordVal.UINT16   },
            { "INT32",      KeywordVal.INT32    },
            { "UINT32",     KeywordVal.UINT32   },
            { "INT64",      KeywordVal.INT64    },
            { "UINT64",     KeywordVal.UINT64   },
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