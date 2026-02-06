namespace TangoFlexLSP
{
    public class LspPosition
    {
        public int Start;
        public int End;

        public LspPosition(int start, int end)
        {
            Start = start;
            End = end;
        }
    }
}