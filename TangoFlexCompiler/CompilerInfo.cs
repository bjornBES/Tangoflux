namespace TangoFlexCompiler
{
    public record FileId(int id);

    public class SourceSpan
    {
        public FileId Id { get; set; }
        public int StartLine { get; set; }
        public int StartColumn { get; set; }
        public int EndLine { get; set; }
        public int EndColumn { get; set; }
        public SourceSpan(FileId id, int startLine, int startColumn, int endLine, int endColumn)
        {
            Id = id;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public override string ToString()
        {
            string sourceFile = CompilerInfo.Files[Id];
            return $"{sourceFile}:{StartLine}:{StartColumn}";
        }
    }


    public static class CompilerInfo
    {
        public static Dictionary<FileId, string> Files = new Dictionary<FileId, string>();
    }
}