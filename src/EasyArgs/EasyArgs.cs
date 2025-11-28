using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BjornBEs.Libs.EasyArgs
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ArgAttribute : Attribute
    {
        public string Short { get; }
        public string Long { get; }
        public bool Required { get; set; }
        public string Help { get; set; }

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

    public static class EasyArgs
    {
        public static T Parse<T>(string[] rawArgs) where T : new()
        {
            return (T)Parse(typeof(T), rawArgs);
        }

        public static object Parse(Type type, string[] rawArgs)
        {
            // Normalize args: skip the first program path if present
            var args = NormalizeRawArgs(rawArgs);

            // check for help first
            if (args.Any(a => a == "--help" || a == "-h"))
            {
                Console.WriteLine(GenerateHelp(type));
                Environment.Exit(0);
            }

            var result = Activator.CreateInstance(type);

            // Map long and short forms
            var optionMap = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            var shortMap = new Dictionary<char, PropertyInfo>();
            var propertyMeta = new Dictionary<PropertyInfo, ArgAttribute>();
            var positionalProps = new SortedDictionary<int, PropertyInfo>();

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = p.GetCustomAttribute<ArgAttribute>();
                if (attr != null)
                {
                    if (!string.IsNullOrEmpty(attr.Short)) optionMap[attr.Short] = p;
                    if (!string.IsNullOrEmpty(attr.Long)) optionMap[attr.Long] = p;
                    if (!string.IsNullOrEmpty(attr.Short) && attr.Short.StartsWith("-") && attr.Short.Length == 2)
                    {
                        shortMap[attr.Short[1]] = p;
                    }
                    propertyMeta[p] = attr;
                }

                var pos = p.GetCustomAttribute<PositionalAttribute>();
                if (pos != null)
                {
                    positionalProps[pos.Index] = p;
                }

                // apply default values if present
                var dv = p.GetCustomAttribute<DefaultValueAttribute>();
                if (dv != null)
                {
                    p.SetValue(result, dv.Value);
                }
            }

            // parse loop
            var tokens = new List<string>(args);
            var consumedPositional = new List<string>();
            int i = 0;
            while (i < tokens.Count)
            {
                string tok = tokens[i];
                if (tok.StartsWith("--"))
                {
                    // long option
                    if (!optionMap.TryGetValue(tok, out var prop))
                    {
                        throw new ArgumentException($"Unknown option: {tok}");
                    }

                    if (IsBoolean(prop.PropertyType))
                    {
                        prop.SetValue(result, true);
                        i++;
                    }
                    else
                    {
                        // needs a value
                        i++;
                        if (i >= tokens.Count) throw new ArgumentException($"Missing value for option {tok}");
                        string val = tokens[i];
                        SetProperty(result, prop, val);
                        i++;
                    }
                }
                else if (tok.StartsWith("-") && tok.Length > 1)
                {
                    // could be combined short flags or a short option with value
                    // e.g. -abc  => -a -b -c  (if a,b,c are booleans)
                    // or -n 10 / -n10
                    if (tok.Length == 2)
                    {
                        char k = tok[1];
                        if (!shortMap.TryGetValue(k, out var prop))
                            throw new ArgumentException($"Unknown option: {tok}");

                        if (IsBoolean(prop.PropertyType))
                        {
                            prop.SetValue(result, true);
                            i++;
                        }
                        else
                        {
                            // check if the rest of token contains the value: -n10 (not in this branch), but since length==2, look at next token
                            i++;
                            if (i >= tokens.Count) throw new ArgumentException($"Missing value for option {tok}");
                            string val = tokens[i];
                            SetProperty(result, prop, val);
                            i++;
                        }
                    }
                    else
                    {
                        // length > 2: either combined booleans -abc or short+value -n10
                        // if the first short corresponds to non-boolean, treat remainder as its value
                        char first = tok[1];
                        if (!shortMap.TryGetValue(first, out var firstProp))
                            throw new ArgumentException($"Unknown option: -{first}");

                        if (!IsBoolean(firstProp.PropertyType))
                        {
                            string remainder = tok.Substring(2);
                            if (string.IsNullOrEmpty(remainder))
                                throw new ArgumentException($"Missing value for option -{first}");
                            SetProperty(result, firstProp, remainder);
                            i++;
                        }
                        else
                        {
                            // treat each char as boolean flag
                            for (int j = 1; j < tok.Length; j++)
                            {
                                char c = tok[j];
                                if (!shortMap.TryGetValue(c, out var pflag))
                                    throw new ArgumentException($"Unknown short flag: -{c}");
                                if (!IsBoolean(pflag.PropertyType))
                                    throw new ArgumentException($"Short combined flag -{c} expects a value, can't be combined");
                                pflag.SetValue(result, true);
                            }
                            i++;
                        }
                    }
                }
                else
                {
                    // positional or unknown - collect as positional
                    consumedPositional.Add(tok);
                    i++;
                }
            }

            // assign positional properties
            foreach (var kv in positionalProps)
            {
                int idx = kv.Key;
                var prop = kv.Value;
                if (idx < consumedPositional.Count)
                {
                    SetProperty(result, prop, consumedPositional[idx]);
                }
                else if (prop.GetCustomAttribute<PositionalAttribute>().Required)
                {
                    throw new ArgumentException($"Missing required positional argument at index {idx} (property {prop.Name})");
                }
            }

            // validate required options
            foreach (var kv in propertyMeta)
            {
                var p = kv.Key;
                var meta = kv.Value;
                if (!meta.Required) continue;
                var val = p.GetValue(result);
                if (val == null || (p.PropertyType == typeof(string) && string.IsNullOrEmpty((string)val)))
                    throw new ArgumentException($"Missing required option {meta.Long} / {meta.Short}");
            }

            return result;
        }

        static string[] NormalizeRawArgs(string[] rawArgs)
        {
            if (rawArgs == null) return Array.Empty<string>();
            // Some environments include the program path as argv[0] — allow both forms
            if (rawArgs.Length > 0 && rawArgs[0].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return rawArgs.Skip(1).ToArray();
            }
            return rawArgs;
        }

        static bool IsBoolean(Type t)
        {
            return t == typeof(bool) || t == typeof(bool);
        }

        static void SetProperty(object target, PropertyInfo prop, string rawValue)
        {
            var propType = prop.PropertyType;

            // handle arrays
            if (propType.IsArray)
            {
                var elemType = propType.GetElementType();
                // split by comma or semicolon or space
                var parts = SplitArrayValues(rawValue);
                var arr = Array.CreateInstance(elemType, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                {
                    arr.SetValue(ConvertTo(parts[i], elemType), i);
                }
                prop.SetValue(target, arr);
                return;
            }

            // special case: IEnumerable<T> that is IList-like could be supported in future

            // enums
            if (propType.IsEnum)
            {
                var val = Enum.Parse(propType, rawValue, ignoreCase: true);
                prop.SetValue(target, val);
                return;
            }

            // nullable
            var underlying = Nullable.GetUnderlyingType(propType);
            if (underlying != null)
            {
                if (string.IsNullOrEmpty(rawValue)) { prop.SetValue(target, null); return; }
                var converted = ConvertTo(rawValue, underlying);
                prop.SetValue(target, converted);
                return;
            }

            if (propType == typeof(string))
            {
                prop.SetValue(target, rawValue);
                return;
            }

            if (IsBoolean(propType))
            {
                // interpret common truthy/falsey
                var v = ParseBoolString(rawValue);
                prop.SetValue(target, v);
                return;
            }

            // numeric and other
            var convertedVal = ConvertTo(rawValue, propType);
            prop.SetValue(target, convertedVal);
        }

        static object ConvertTo(string raw, Type targetType)
        {
            try
            {
                // allow enums handled earlier
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter != null && converter.CanConvertFrom(typeof(string)))
                {
                    return converter.ConvertFromInvariantString(raw);
                }

                // fallback
                return Convert.ChangeType(raw, targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert '{raw}' to {targetType.Name}: {ex.Message}");
            }
        }

        static string[] SplitArrayValues(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            // support comma, semicolon, or whitespace separated lists
            var parts = raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                           .SelectMany(s => s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                           .ToArray();
            return parts;
        }

        static bool ParseBoolString(string s)
        {
            if (string.IsNullOrEmpty(s)) return true;
            s = s.Trim().ToLowerInvariant();
            if (s == "true" || s == "1" || s == "yes" || s == "on") return true;
            if (s == "false" || s == "0" || s == "no" || s == "off") return false;
            throw new ArgumentException($"Invalid boolean value: {s}");
        }

        static string GenerateHelp(Type t)
        {
            var lines = new List<string>();
            lines.Add($"Usage: {AppDomain.CurrentDomain.FriendlyName} [options] {GeneratePositionalUsage(t)}\n");
            lines.Add("Options:");

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var a = p.GetCustomAttribute<ArgAttribute>();
                if (a != null)
                {
                    string names = $"{a.Short,-6} {a.Long,-20}";
                    string help = a.Help ?? "";
                    var dv = p.GetCustomAttribute<DefaultValueAttribute>()?.Value;
                    if (dv != null) help += $" (default: {dv})";
                    if (a.Required) help += " (required)";
                    lines.Add($"  {names} {help}");
                }
            }

            // positional
            var positional = new List<(int, PropertyInfo, PositionalAttribute)>();
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var pa = p.GetCustomAttribute<PositionalAttribute>();
                if (pa != null) positional.Add((pa.Index, p, pa));
            }
            if (positional.Count > 0)
            {
                lines.Add("\nPositional arguments:");
                foreach (var item in positional.OrderBy(x => x.Item1))
                {
                    var p = item.Item2; var pa = item.Item3;
                    string help = pa.Help ?? "";
                    if (pa.Required) help += " (required)";
                    lines.Add($"  [{item.Item1}] {p.Name} {help}");
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
            return string.Join(" ", positional.Select(kv => kv.Value.Name.ToUpperInvariant()));
        }
    }
}
