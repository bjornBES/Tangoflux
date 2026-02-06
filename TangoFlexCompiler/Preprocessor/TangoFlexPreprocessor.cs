
using System.ComponentModel;
using System.Text;
using Common;

namespace TangoFlexCompiler.Preprocessor
{
    public class TangoFlexPreprocessor
    {
        public bool debug = true;
        public string debugFile = Path.GetFullPath(Path.Combine("debug", "preprocessor.tf"));
        public string Source;
        private string[] lines;
        Arguments args;
        List<string> includeOnce = new List<string>();
        string fileName;
        public TangoFlexPreprocessor(Arguments arguments)
        {
            args = arguments;
            fileName = arguments.InputFile;
        }

        public string Process(string sourceCode)
        {
            lines = sourceCode.Split('\n');
            string newSrc = Process(args, lines);
            if (debug && args.debug == true && args.WriteDebugFiles)
            {
                File.WriteAllText(debugFile, newSrc);
            }
            return newSrc;
        }
        public string Process(string sourceCode, string file)
        {
            fileName = file;
            lines = sourceCode.Split('\n');
            return Process(args, lines);
        }
        public string Process(Arguments arguments, string[] _lines)
        {
            StringBuilder output = new();
            bool currentEnabled = true;
            // bool HasFile = lines.Contains($"#file {arguments.InputFile}", StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _lines.Length; i++)
            {
                string line = _lines[i].TrimStart();

                if (line.StartsWith("#"))
                {
                    line = HandleLineContinuation(_lines, i, ref i);

                    if (line.StartsWith("#extension "))
                    {
                        string libName = line[11..].Trim();
                        output.AppendLine($"#include <{libName}.tf>");
                    }
                    else if (line.StartsWith("#included once"))
                    {
                        includeOnce.Add(fileName);
                    }
                    else
                    {
                        output.AppendLine(_lines[i]);
                    }

                    continue;
                }

                if (currentEnabled)
                {
                    output.AppendLine(_lines[i]);
                }
            }

            return output.ToString();
        }
        private string HandleLineContinuation(string[] _lines, int index, ref int i)
        {
            StringBuilder builder = new();
            string line = _lines[i].TrimEnd();
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
}