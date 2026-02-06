using System;

namespace TangoFlexCompiler.Lexer {
    public static class Utils {

        // IsEscapeChar : Char -> Boolean
        // ==============================
        // 
        public static bool IsEscapeChar(char ch) {
            switch (ch) {
                case 'a':
                case 'b':
                case 'f':
                case 'n':
                case 'r':
                case 't':
                case 'v':
                case '\'':
                case '\"':
                case '\\':
                case '?':
                    return true;
                default:
                    return false;
            }
        }

        // IsHexDigit : Char -> Boolean
        // ============================
        // 
        public static bool IsHexDigit(char ch) {
            return (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
        }

        // IsOctDigit : Char -> Boolean
        // ============================
        // 
        public static bool IsOctDigit(char ch) {
            return ch >= '0' && ch <= '7';
        }

        // GetHexDigit : Char -> Int64
        // ===========================
        // 
        public static long GetHexDigit(char ch) {
            if (ch >= '0' && ch <= '9') {
                return ch - '0';
            }
            if (ch >= 'a' && ch <= 'f') {
                return ch - 'a' + 0xA;
            }
            if (ch >= 'A' && ch <= 'F') {
                return ch - 'A' + 0xA;
            }
            throw new Exception("GetHexDigit: Character is not a hex digit. You should first call IsHexDigit(ch) for a check.");
        }

        // IsSpace : Char -> Boolean
        // =========================
        // 
        public static bool IsSpace(char ch) {
            return (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\f' || ch == '\v');
        }
    }
}