namespace TangoFlexLSP
{
    public class LspRange
    {
        public LspPosition StartPosition;
        public LspPosition EndPosition;

        public LspRange(LspPosition start, LspPosition end)
        {
            StartPosition = start;
            EndPosition = end;
        }
    }
}