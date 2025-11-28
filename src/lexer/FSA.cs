using System;

namespace CompilerTangoFlex.lexer {
    public enum FSAStatus {
        NONE,
        END,
        RUNNING,
        ERROR
    }

    public abstract class FSA {
        public abstract FSAStatus GetStatus();
        public abstract void ReadChar(char ch);
        public abstract void Reset();
        public abstract void ReadEOF();
        public abstract Token RetrieveToken();
    }
}