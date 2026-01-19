
public class TangoFlexPreprocessor
{
    public string SourceCode { get; private set; }

    public TangoFlexPreprocessor(string sourceCode, Arguments arguments)
    {
        SourceCode = sourceCode;
    }
}