namespace BluelBerry;

/// <summary>Lightweight command-line options parser using reflection with strict validation.</summary>
public static class AppOptionsParser
{
    /// <summary>Parses args into strongly-typed options. Supports --option value, --option (bool), --no-option (false). Fails on unrecognized arguments.</summary>
    public static T Parse<T>(string[] args, string exeName = "app") where T : class
    {
        var type = typeof(T);
        var ctor = type.GetConstructors().Single();
        var parameters = ctor.GetParameters();

        // Handle --help early
        if (args.Any(a => a is "--help" or "-h" or "/?"))
        {
            Console.WriteLine(Usage<T>(exeName));
            Environment.Exit(0);
        }

        var values = new object?[parameters.Length];

        // Build set of valid argument names
        var validArgNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var param in parameters)
        {
            var kebabName = ToKebab(param.Name ?? "");
            validArgNames.Add("--" + kebabName);
            if (param.ParameterType == typeof(bool))
            {
                validArgNames.Add("--no-" + kebabName);
            }
        }
        
        // Add help variants
        validArgNames.Add("--help");
        validArgNames.Add("-h");
        validArgNames.Add("/?");

        // Initialize with parameter defaults
        for (var i = 0; i < parameters.Length; i++)
            values[i] = parameters[i].HasDefaultValue
                ? parameters[i].DefaultValue
                : GetDefault(parameters[i].ParameterType);

        // First pass: identify all arguments and validate them
        var unrecognizedArgs = new List<string>();
        
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--") && !arg.StartsWith("-")) continue;

            // Handle single dash arguments (convert to double dash for validation)
            var normalizedArg = arg.StartsWith("-") && !arg.StartsWith("--") ? "--" + arg[1..] : arg;
            
            if (!validArgNames.Contains(normalizedArg))
            {
                unrecognizedArgs.Add(arg);
                continue;
            }
            
            // Skip the value if this argument takes one
            if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
            {
                var name = normalizedArg[2..];
                if (!name.StartsWith("no-"))
                {
                    var pIndex = Array.FindIndex(parameters,
                        p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (pIndex >= 0 && parameters[pIndex].ParameterType != typeof(bool))
                    {
                        i++; // Skip the value
                    }
                }
            }
        }

        // Report unrecognized arguments and exit
        if (unrecognizedArgs.Any())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: Unrecognized argument(s): {string.Join(", ", unrecognizedArgs)}");
            Console.ResetColor();
            Console.WriteLine("Use --help to see all valid options.");
            Environment.Exit(1);
        }

        // Second pass: parse the arguments
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--") && !arg.StartsWith("-")) continue;

            // Normalize single dash to double dash
            var normalizedArg = arg.StartsWith("-") && !arg.StartsWith("--") ? "--" + arg[1..] : arg;
            var name = normalizedArg[2..];
            string? value;

            if (name.StartsWith("no-"))
            {
                name = name[3..];
                value = "false";
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
            {
                value = args[++i];
            }
            else
            {
                value = "true";
            }

            var pIndex = Array.FindIndex(parameters,
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            if (pIndex >= 0)
            {
                try
                {
                    values[pIndex] = ConvertTo(value, parameters[pIndex].ParameterType);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Error: Invalid value '{value}' for argument {normalizedArg}: {ex.Message}");
                    Console.ResetColor();
                    Environment.Exit(1);
                }
            }
        }

        try
        {
            var result = (T)ctor.Invoke(values);
            
            // Print parsed configuration for confirmation
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ Configuration:");
            foreach (var param in parameters)
            {
                var paramIndex = Array.IndexOf(parameters, param);
                var value = values[paramIndex];
                var displayValue = param.Name?.ToLower() == "key" && value?.ToString()?.Length > 8 
                    ? value.ToString()?[..4] + "..." + value.ToString()?[^4..] 
                    : value?.ToString();
                Console.WriteLine($"   --{ToKebab(param.Name ?? "")}: {displayValue}");
            }
            Console.ResetColor();
            Console.WriteLine();
            
            return result;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error creating configuration: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
            return default!; // Never reached
        }
    }

    /// <summary>Generates usage help message for the options type.</summary>
    public static string Usage<T>(string exeName = "app")
    {
        var ctor = typeof(T).GetConstructors().Single();
        var parameters = ctor.GetParameters();

        var lines = new List<string> { $"Usage: {exeName} [options]" };
        lines.Add("");
        lines.Add("Options:");
        
        foreach (var p in parameters)
        {
            var name = "--" + ToKebab(p.Name ?? "");
            var desc = p.ParameterType == typeof(bool)
                ? $"{name} / --no-{ToKebab(p.Name ?? "")}"
                : $"{name} <{p.ParameterType.Name.ToLower()}>";

            // Show default value if it exists
            if (p.HasDefaultValue && p.DefaultValue != null) 
            {
                var defaultDisplay = p.Name?.ToLower() == "key" && p.DefaultValue.ToString()?.Length > 8
                    ? p.DefaultValue.ToString()?[..4] + "..." + p.DefaultValue.ToString()?[^4..]
                    : p.DefaultValue.ToString();
                desc += $" (default: {defaultDisplay})";
            }

            lines.Add($"  {desc}");
        }

        lines.Add("  --help, -h       Show this message");
        lines.Add("");
        lines.Add("Examples:");
        lines.Add($"  {exeName} --model gpt-4o --endpoint https://api.openai.com/v1 --key sk-...");
        lines.Add($"  {exeName} --help");
        
        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Converts string to specified type (string, int, bool).</summary>
    private static object ConvertTo(string s, Type t)
    {
        if (t == typeof(string)) return s;
        if (t == typeof(int)) return int.Parse(s);
        if (t == typeof(bool)) return bool.Parse(s);
        throw new NotSupportedException($"Unsupported type {t}");
    }

    /// <summary>Gets default value for type (0, false, null, etc.).</summary>
    private static object? GetDefault(Type t)
    {
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }

    /// <summary>Converts PascalCase/camelCase to kebab-case (MyOption → my-option).</summary>
    private static string ToKebab(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}