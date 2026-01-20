using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BjornBEs.Libs.EasyArgs
{
    #region Attributes

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ArgAttributeHelp : Attribute
    {
        public string ValueName { get; set; } = "";
        public string Help { get; } = "";
        public string[] AllowedValues { get; set; } = [];
        public string[]? ValueDescriptions { get; set; } = [];
        public string? HelpPlaceholder { get; set; } = "";
        public string Category { get; set; } = "";
        public bool ShowByDefault { get; set; } = true;
        public bool ShowList { get; set; } = true;

        public ArgAttributeHelp(string help, string category)
        {
            Help = help;
            Category = category;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ArgAttribute : Attribute
    {
        public string Short { get; }
        public string Long { get; }
        public bool Required { get; set; } = false;
        public string[] AllowedValues { get; set; } = [];
        public bool HasValue { get; set; } = true;

        public ArgAttribute(string shortName, string longName)
        {
            Short = shortName;
            Long = longName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PositionalAttribute : Attribute
    {
        public int Index { get; }
        public string Help { get; set; }
        public bool Required { get; set; }
        public PositionalAttribute(int index) { Index = index; }
    }

    #endregion

    #region Internal Tokens

    internal sealed class OptionToken
    {
        public string Name;
        public string Value;
    }

    #endregion

    public static class EasyArgs
    {
        #region Public API

        public static T Parse<T>(string[] rawArgs) where T : new()
        {
            return (T)Parse(typeof(T), rawArgs);
        }

        public static object Parse(Type type, string[] rawArgs)
        {
            var args = NormalizeRawArgs(rawArgs);

            if (args.Any(a => a == "--help" || a == "-h"))
            {
                Console.WriteLine(GenerateHelp(type, "-h"));
                Environment.Exit(0);
            }

            var result = Activator.CreateInstance(type);

            // metadata
            var optionMap = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            var shortMap = new Dictionary<char, PropertyInfo>();
            var argMeta = new Dictionary<PropertyInfo, ArgAttribute>();
            var positionalProps = new SortedDictionary<int, PropertyInfo>();

            // defaults + maps
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var arg = p.GetCustomAttribute<ArgAttribute>();
                if (arg != null)
                {
                    if (!string.IsNullOrEmpty(arg.Short)) optionMap[arg.Short] = p;
                    if (!string.IsNullOrEmpty(arg.Long)) optionMap[arg.Long] = p;

                    if (!string.IsNullOrEmpty(arg.Short) &&
                        arg.Short.StartsWith("-") &&
                        arg.Short.Length == 2)
                        shortMap[arg.Short[1]] = p;

                    argMeta[p] = arg;
                }

                var pos = p.GetCustomAttribute<PositionalAttribute>();
                if (pos != null)
                    positionalProps[pos.Index] = p;

                var dv = p.GetCustomAttribute<DefaultValueAttribute>();
                if (dv != null)
                    p.SetValue(result, dv.Value);
            }

            // -------- PASS 1: TOKENIZE --------
            Tokenize(args, shortMap, optionMap,
                out var optionTokens,
                out var positionalTokens);

            // -------- PASS 2: BIND OPTIONS --------
            foreach (var tok in optionTokens)
            {
                if (!optionMap.TryGetValue(tok.Name, out var prop))
                    throw new ArgumentException($"Unknown option {tok.Name}");

                var meta = argMeta[prop];

                if (!meta.HasValue || IsBoolean(prop.PropertyType))
                {
                    bool val = tok.Value == null ? true : ParseBoolString(tok.Value);
                    SetOrAppend(result, prop, val);
                }
                else
                {
                    if (tok.Value == null)
                        throw new ArgumentException($"Missing value for option {tok.Name}");

                    SetOrAppend(result, prop, tok.Value);
                }
            }

            // -------- POSITIONALS --------
            foreach (var kv in positionalProps)
            {
                int idx = kv.Key;
                var prop = kv.Value;
                var pa = prop.GetCustomAttribute<PositionalAttribute>();

                if (idx < positionalTokens.Count)
                {
                    SetOrAppend(result, prop, positionalTokens[idx]);
                }
                else if (pa.Required)
                {
                    throw new ArgumentException(
                        $"Missing required positional argument {prop.Name}");
                }
            }

            // -------- REQUIRED VALIDATION --------
            foreach (var kv in argMeta)
            {
                var prop = kv.Key;
                var meta = kv.Value;
                if (!meta.Required) continue;

                var val = prop.GetValue(result);
                if (val == null)
                    throw new ArgumentException(
                        $"Missing required option {meta.Long} / {meta.Short}");
            }

            return result;
        }

        #endregion

        #region Pass 1 – Tokenizer

        static void Tokenize(string[] args, Dictionary<char, PropertyInfo> shortMap, Dictionary<string, PropertyInfo> optionMap, out List<OptionToken> options, out List<string> positionals)
        {
            options = new List<OptionToken>();
            positionals = new List<string>();

            int i = 0;
            while (i < args.Length)
            {
                string tok = args[i];

                if (tok.StartsWith("--"))
                {
                    string name;
                    string value = null;

                    int eq = tok.IndexOf('=');
                    if (eq >= 0)
                    {
                        name = tok.Substring(0, eq);
                        value = tok.Substring(eq + 1);
                    }
                    else
                    {
                        name = tok;
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            value = args[++i];
                        }
                    }

                    options.Add(new OptionToken { Name = name, Value = value });
                }
                else if (tok.StartsWith("-") && tok.Length > 1)
                {
                    if (tok.Length == 2)
                    {
                        char c = tok[1];
                        var prop = shortMap[c];
                        var meta = prop.GetCustomAttribute<ArgAttribute>();

                        string value = null;
                        if (meta.HasValue && i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                            value = args[++i];

                        options.Add(new OptionToken { Name = meta.Short, Value = value });
                    }
                    else
                    {
                        char first = tok[1];
                        var prop = shortMap[first];
                        var meta = prop.GetCustomAttribute<ArgAttribute>();

                        if (meta.HasValue)
                        {
                            options.Add(new OptionToken
                            {
                                Name = meta.Short,
                                Value = tok.Substring(2)
                            });
                        }
                        else
                        {
                            for (int j = 1; j < tok.Length; j++)
                            {
                                char c = tok[j];
                                var p = shortMap[c];
                                options.Add(new OptionToken
                                {
                                    Name = p.GetCustomAttribute<ArgAttribute>().Short,
                                    Value = null
                                });
                            }
                        }
                    }
                }
                else
                {
                    positionals.Add(tok);
                }

                i++;
            }
        }

        #endregion

        #region Property Binding

        static void SetOrAppend(object target, PropertyInfo prop, object raw)
        {
            var t = prop.PropertyType;

            // Array
            if (t.IsArray)
            {
                var elem = t.GetElementType();
                var current = (Array)prop.GetValue(target);
                var list = new List<object>();

                if (current != null)
                    foreach (var x in current) list.Add(x);

                list.Add(ConvertTo(raw.ToString(), elem));

                var arr = Array.CreateInstance(elem, list.Count);
                for (int i = 0; i < list.Count; i++)
                    arr.SetValue(list[i], i);

                prop.SetValue(target, arr);
                return;
            }

            // IList<T>
            if (typeof(IList).IsAssignableFrom(t))
            {
                var list = (IList)prop.GetValue(target);
                if (list == null)
                {
                    list = (IList)Activator.CreateInstance(t);
                    prop.SetValue(target, list);
                }

                var elem = t.IsGenericType ? t.GetGenericArguments()[0] : typeof(object);
                if (raw.ToString().Contains(','))
                {
                    string[] split = raw.ToString().Split(',');
                    foreach (string s in split)
                    {
                        list.Add(ConvertTo(s, elem));
                    }
                }
                else
                {
                    list.Add(ConvertTo(raw.ToString(), elem));
                }
                return;
            }

            // scalar
            SetProperty(target, prop, raw.ToString());
        }

        static void SetProperty(object target, PropertyInfo prop, string raw)
        {
            var t = prop.PropertyType;

            if (t.IsEnum)
            {
                prop.SetValue(target, Enum.Parse(t, raw, true));
                return;
            }

            var nullable = Nullable.GetUnderlyingType(t);
            if (nullable != null)
            {
                prop.SetValue(target, ConvertTo(raw, nullable));
                return;
            }

            if (IsBoolean(t))
            {
                prop.SetValue(target, ParseBoolString(raw));
                return;
            }

            if (t == typeof(string))
            {
                prop.SetValue(target, raw);
                return;
            }

            prop.SetValue(target, ConvertTo(raw, t));
        }

        #endregion

        #region Utilities

        static bool IsBoolean(Type t) =>
            t == typeof(bool) || t == typeof(bool?);

        static object ConvertTo(string raw, Type t)
        {
            var conv = TypeDescriptor.GetConverter(t);
            if (conv != null && conv.CanConvertFrom(typeof(string)))
                return conv.ConvertFromInvariantString(raw);

            return Convert.ChangeType(raw, t);
        }

        static bool ParseBoolString(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            s = s.ToLowerInvariant();
            if (s == "1" || s == "true" || s == "yes" || s == "on") return true;
            if (s == "0" || s == "false" || s == "no" || s == "off") return false;
            throw new ArgumentException($"Invalid boolean: {s}");
        }

        static string[] NormalizeRawArgs(string[] raw)
        {
            if (raw == null) return Array.Empty<string>();
            if (raw.Length > 0 && raw[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return raw.Skip(1).ToArray();
            return raw;
        }

        #endregion

        #region Help
        static int longestLine = 0;
        static string GenerateHelp(Type t, string from)
        {
            var lines = new List<string>
            {
                $"Usage: {AppDomain.CurrentDomain.FriendlyName} [options] {GeneratePositionalUsage(t)}",
                "",
                "Options:"
            };
            PropertyInfo[] ps = t.GetProperties();
            ps = ps.OrderBy(p =>
            {
                if (p == null)
                {
                    return "NULL CATEGORY";
                }
                ArgAttributeHelp arg = p.GetCustomAttribute<ArgAttributeHelp>();
                if (arg != null)
                {
                    return arg.Category;
                }
                return "NULL CATEGORY";
            }).ToArray();
            foreach (PropertyInfo p in ps)
            {
                var a = p.GetCustomAttribute<ArgAttribute>();
                ArgAttributeHelp h = p.GetCustomAttribute<ArgAttributeHelp>();
                if (h == null)
                {
                    continue;
                }
                if (a != null)
                {
                    string names = $"{a.Short}";
                    if (!string.IsNullOrEmpty(a.Short) && !string.IsNullOrEmpty(a.Long))
                    {
                        names = $"{a.Short,-6} {a.Long}";
                    }
                    else if (!string.IsNullOrEmpty(a.Long))
                    {
                        names = $"{a.Long}";
                    }

                    if (a.HasValue && !string.IsNullOrEmpty(h.HelpPlaceholder))
                    {
                        names += $"={h.HelpPlaceholder}";
                    }
                    names = names.PadRight(20, ' ');
                    int length = $"  {names}".Length;
                    if (length > longestLine)
                    {
                        longestLine = length;
                    }
                }
            }

            string lastCategory = "";

            foreach (var p in ps)
            {
                var a = p.GetCustomAttribute<ArgAttribute>();
                ArgAttributeHelp h = p.GetCustomAttribute<ArgAttributeHelp>();
                if (h == null)
                {
                    continue;
                }
                if (h.ShowByDefault == false)
                {
                    continue;
                }
                if (a != null)
                {
                    if (lastCategory != h.Category)
                    {
                        lines.Add("");
                        lines.Add($"{h.Category} options:");
                        lastCategory = h.Category;
                    }

                    string name = $"{a.Short}";
                    if (!string.IsNullOrEmpty(a.Short) && !string.IsNullOrEmpty(a.Long))
                    {
                        name = $"{a.Short,-6} {a.Long}";
                    }
                    else if (!string.IsNullOrEmpty(a.Long))
                    {
                        name = $"{a.Long}";
                    }

                    string names = name;
                    if (a.HasValue && !string.IsNullOrEmpty(h.HelpPlaceholder))
                    {
                        names += $"={h.HelpPlaceholder}";
                    }
                    names = names.PadRight(longestLine, ' ');
                    string help = h.Help ?? "";
                    var dv = p.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                    if (dv != null) help += $" (default: {dv})";
                    if (a.Required) help += " (required)";
                    if (a.AllowedValues.Length > 0 && h.ShowList)
                    {
                        if (h.ValueDescriptions.Length > 0 && h.ValueDescriptions.Length != a.AllowedValues.Length)
                        {
                            throw new ArgumentException("The Length of ValueDescriptions and AllowedValues need to be the same");
                        }
                        help += Environment.NewLine;
                        for (int i = 0; i < a.AllowedValues.Length; i++)
                        {
                            int nameLength = name.Length + (names.Contains('=') ? 1 : 0) + 2;
                            help += "".PadLeft(nameLength, ' ');
                            help += $"{a.AllowedValues[i]}".PadRight(longestLine, ' ');
                            if (h.ValueDescriptions.Length > 0)
                            {
                                help += h.ValueDescriptions[i];
                            }
                            if (dv != null)
                            {
                                if (dv.ToString() == a.AllowedValues[i])
                                {
                                    help += "[default]";
                                }
                            }
                            help += Environment.NewLine;
                        }
                    }
                    lines.Add($"  {names} {help}");
                }
            }

            return string.Join("\n", lines);
        }

        static string GeneratePositionalUsage(Type t)
        {
            var positional = new SortedDictionary<int, PropertyInfo>();
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var pa = p.GetCustomAttribute<PositionalAttribute>();
                if (pa != null) positional[pa.Index] = p;
            }

            if (positional.Count == 0) return "";

            // Use the property name in uppercase, or fallback to the long option if it exists
            return string.Join(" ", positional.Select(kv =>
            {
                var p = kv.Value;
                var argAttr = p.GetCustomAttribute<ArgAttribute>();
                var argPos = p.GetCustomAttribute<PositionalAttribute>();
                if (argAttr != null)
                {
                    return argAttr.Long.TrimStart('-').ToUpperInvariant();
                }
                else
                {
                    return p.Name.ToUpperInvariant();
                }
            }));
        }


        #endregion
    }
}
