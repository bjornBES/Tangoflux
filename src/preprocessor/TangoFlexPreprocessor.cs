
using System.Text;



public class TangoFlexPreprocessor
{
    public bool debug = true;
    public string debugFile = Path.GetFullPath(Path.Combine("debug", "preprocessor.tf"));
    public string Source;
    private readonly string[] lines;

    private List<string> includedFiles = new List<string>();
    public TangoFlexPreprocessor(string sourceCode, Arguments arguments)
    {
        lines = sourceCode.Split('\n');
        Source = Process(arguments);
        lines = Source.Split('\n');
        Source = ProcessIncludes(arguments);
        lines = Source.Split('\n');
        Source = Process(arguments);

        if (debug)
        {
            File.WriteAllText(debugFile, Source);
        }
    }

    private string ProcessIncludes(Arguments arguments)
    {
        StringBuilder output = new();
        bool currentEnabled = true;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            if (line.StartsWith("#"))
            {
                line = HandleLineContinuation(i, ref i);

                if (line.StartsWith("#namespace "))
                {
                    string ns = line[11..].Trim();
                    // handle namespace logic later
                }
                else if (line.StartsWith("#import "))
                {
                    string importName = line[7..].Trim();
                    // handle import logic later
                }
                else if (line.StartsWith("#include "))
                {
                    string libName = line[9..].Trim();
                    string filePath = "";
                    if (libName.StartsWith('<') && libName.EndsWith('>'))
                    {
                        string defaultPath = "lib"; // temp
                        filePath = Path.Combine(defaultPath, libName.Substring(1, libName.Length - 2));
                    }
                    if (includedFiles.Contains(filePath))
                    {
                        continue;
                    }
                    string src = File.ReadAllText(filePath);
                    if (src.Contains("#included once"))
                    {
                        includedFiles.Add(libName);
                        src = src.Replace("#included once", "");
                    }
                    output.AppendLine(src);
                }
                else if (line.StartsWith("#included "))
                {
                    string libName = line[10..].Trim();
                }
                else
                {
                    output.AppendLine(lines[i]);
                }

                continue;
            }

            if (currentEnabled)
                output.AppendLine(lines[i]);
        }

        return output.ToString();
    }

    public string Process(Arguments arguments)
    {
        StringBuilder output = new();
        bool currentEnabled = true;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimStart();

            if (line.StartsWith("#"))
            {
                line = HandleLineContinuation(i, ref i);

                if (line.StartsWith("#namespace "))
                {
                    string ns = line[11..].Trim();
                    // handle namespace logic later
                }
                else if (line.StartsWith("#import "))
                {
                    string importName = line[7..].Trim();
                    // handle import logic later
                }
                else if (line.StartsWith("#extension "))
                {
                    string libName = line[11..].Trim();
                    output.AppendLine($"#include <{libName}.tf>");
                }
                else
                {
                    output.AppendLine(lines[i]);
                }

                continue;
            }

            if (currentEnabled)
                output.AppendLine(lines[i]);
        }

        return output.ToString();
    }
    private string HandleLineContinuation(int index, ref int i)
    {
        StringBuilder builder = new();
        string line = lines[i].TrimEnd();
        builder.Append(line);

        while (line.EndsWith("\\"))
        {
            builder.Length--; // remove backslash
            i++;
            if (i >= lines.Length) break;
            line = lines[i].TrimEnd();
            builder.Append(' ').Append(line);
        }

        return builder.ToString();
    }

    private void HandleDirective(string line, ref bool currentEnabled)
    {
        if (line.StartsWith("#namespace "))
        {
            string ns = line[11..].Trim();
            // handle namespace logic later
            return;
        }

        if (line.StartsWith("#import "))
        {
            string importName = line[7..].Trim();
            // handle import logic later
            return;
        }

    }
}