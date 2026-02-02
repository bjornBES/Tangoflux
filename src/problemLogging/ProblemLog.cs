
using CompilerTangoFlex.lexer;

public enum State
{
    Info,
    Warning,
    Error,
}

public static class ProblemLog
{
    public static void LogProblem(State state, SourceSpan span, string message)
    {
        Console.WriteLine(span.ToString());
        Console.WriteLine(message);
    }
    public static void LogProblem(State state, string file, string message)
    {
        Console.WriteLine($"{file}");
        Console.WriteLine(message);
    }
    public static void LogProblem(State state, string message)
    {
        Console.WriteLine(message);
    }
}