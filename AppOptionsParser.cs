namespace BluelBerry;

/// <summary>Lightweight command-line options parser using reflection.</summary>
public static class AppOptionsParser
{
    /// <summary>Parses args into strongly-typed options. Supports --option value, --option (bool), --no-option (false).</summary>
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

        // Initialize with parameter defaults
        for (var i = 0; i < parameters.Length; i++)
            values[i] = parameters[i].HasDefaultValue
                ? parameters[i].DefaultValue
                : GetDefault(parameters[i].ParameterType);

        // Parse args
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--")) continue;

            var name = arg[2..];
            string? value;

            if (name.StartsWith("no-"))
            {
                name = name[3..];
                value = "false";
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
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
                values[pIndex] = ConvertTo(value, parameters[pIndex].ParameterType);
        }

        return (T)ctor.Invoke(values);
    }

    /// <summary>Generates usage help message for the options type.</summary>
    public static string Usage<T>(string exeName = "app")
    {
        var ctor = typeof(T).GetConstructors().Single();
        var parameters = ctor.GetParameters();

        var lines = new List<string> { $"Usage: {exeName} [options]" };
        foreach (var p in parameters)
        {
            var name = "--" + ToKebab(p.Name ?? "");
            var desc = p.ParameterType == typeof(bool)
                ? $"{name} / --no-{ToKebab(p.Name ?? "")}"
                : $"{name} <{p.ParameterType.Name.ToLower()}>";

            // Show default value if it exists
            if (p.HasDefaultValue && p.DefaultValue != null) desc += $" (default: {p.DefaultValue})";

            lines.Add($"  {desc}");
        }

        lines.Add("  --help           Show this message");
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

    /// <summary>Converts PascalCase/camelCase to kebab-case (MyOption -> my-option).</summary>
    private static string ToKebab(string name)
    {
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}