// api-inventory: dump the public/protected surface of .NET assemblies as markdown + json.
// Modes:
//   runtime  - loads assemblies with AssemblyLoadContext and reports nullability via NullabilityInfoContext.
//   metadata - uses MetadataLoadContext; no nullability; works for reference assemblies.
// Usage:
//   dotnet run -- --assembly <path> [--assembly ...] [--resolve-dir <dir>]...
//                 [--include <regex>]... [--exclude <regex>]...
//                 [--mode runtime|metadata] [--core-dir <ref-pack-dir>]
//                 [--out-md <path>] [--out-json <path>] [--title <text>]

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var argsList = Environment.GetCommandLineArgs().Skip(1).ToArray();

string[] Multi(string name)
{
    var values = new List<string>();
    for (var i = 0; i < argsList.Length - 1; i++)
    {
        if (argsList[i] == name) values.Add(argsList[i + 1]);
    }
    return values.ToArray();
}

string? Single(string name)
{
    for (var i = 0; i < argsList.Length - 1; i++)
    {
        if (argsList[i] == name) return argsList[i + 1];
    }
    return null;
}

var assemblies = Multi("--assembly");
var resolveDirs = Multi("--resolve-dir");
var includes = Multi("--include").Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();
var excludes = Multi("--exclude").Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();
var mode = Single("--mode") ?? "runtime";
var coreDir = Single("--core-dir");
var outMd = Single("--out-md");
var outJson = Single("--out-json");
var title = Single("--title") ?? "Public API inventory";
var listTypes = argsList.Contains("--list-types");

if (assemblies.Length == 0)
{
    Console.Error.WriteLine("no --assembly given");
    return 2;
}

var searchDirs = new List<string>();
foreach (var a in assemblies)
{
    if (Path.GetDirectoryName(Path.GetFullPath(a)) is { Length: > 0 } d) searchDirs.Add(d);
}
searchDirs.AddRange(resolveDirs);

return mode == "metadata"
    ? MetadataRunner.Run(assemblies, searchDirs, coreDir, includes, excludes, title, outMd, outJson, listTypes)
    : RuntimeRunner.Run(assemblies, searchDirs, includes, excludes, title, outMd, outJson, listTypes);

internal static class MetadataRunner
{
    public static int Run(string[] assemblies, List<string> searchDirs, string? coreDir,
        Regex[] includes, Regex[] excludes, string title, string? outMd, string? outJson, bool listTypes)
    {
        var all = searchDirs
            .Where(Directory.Exists)
            .SelectMany(d => Directory.GetFiles(d, "*.dll"))
            .Concat(coreDir is not null && Directory.Exists(coreDir) ? Directory.GetFiles(coreDir, "*.dll") : [])
            .GroupBy(Path.GetFileName)
            .Select(g => g.First())
            .ToArray();
        using var mlc = new MetadataLoadContext(new PathAssemblyResolver(all));
        var loaded = assemblies.Select(a => mlc.LoadFromAssemblyPath(Path.GetFullPath(a))).ToArray();
        return Runner.Run(loaded, null, includes, excludes, title, outMd, outJson, listTypes);
    }
}

internal static class RuntimeRunner
{
    public static int Run(string[] assemblies, List<string> searchDirs,
        Regex[] includes, Regex[] excludes, string title, string? outMd, string? outJson, bool listTypes)
    {
        if (searchDirs.Count > 0)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                var simple = new AssemblyName(e.Name).Name + ".dll";
                foreach (var dir in searchDirs)
                {
                    var candidate = Path.Combine(dir, simple);
                    if (File.Exists(candidate)) return Assembly.LoadFrom(candidate);
                }
                return null;
            };
        }

        var loaded = assemblies.Select(Assembly.LoadFrom).ToArray();
        return Runner.Run(loaded, DescribeNullability, includes, excludes, title, outMd, outJson, listTypes);
    }

    private static readonly NullabilityInfoContext Ctx = new();

    private static string? DescribeNullability(object member)
    {
        try
        {
            return member switch
            {
                ParameterInfo p => Ctx.Create(p).ReadState.ToString(),
                PropertyInfo p => Ctx.Create(p).ReadState.ToString(),
                FieldInfo f => Ctx.Create(f).ReadState.ToString(),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }
}

internal static class Runner
{
    public static int Run(Assembly[] assemblies, Func<object, string?>? nullability,
        Regex[] includes, Regex[] excludes, string title, string? outMd, string? outJson, bool listTypes)
    {
        var types = new List<Type>();
        foreach (var asm in assemblies)
        {
            Type?[] exported;
            try { exported = asm.GetExportedTypes(); }
            catch (ReflectionTypeLoadException ex) { exported = ex.Types; }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"WARN: cannot enumerate {asm.FullName}: {ex.Message}");
                continue;
            }
            types.AddRange(exported.Where(t => t is not null).Cast<Type>());
        }

        types = types
            .Where(t => IsIncluded(t, includes, excludes))
            .Distinct()
            .OrderBy(t => t.Namespace ?? "", StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        if (listTypes)
        {
            foreach (var t in types)
            {
                Console.WriteLine(t.FullName);
            }
            return 0;
        }

        var md = new StringBuilder();
        md.AppendLine($"# {title}");
        md.AppendLine();
        md.AppendLine($"Assemblies: {string.Join(", ", assemblies.Select(a => $"{a.GetName().Name} {a.GetName().Version}"))}");
        md.AppendLine();
        md.AppendLine($"Type count: {types.Count}");
        md.AppendLine();

        var jsonTypes = new List<object>();
        string? currentNs = null;
        foreach (var t in types)
        {
            if (t.Namespace != currentNs)
            {
                currentNs = t.Namespace;
                md.AppendLine($"## {currentNs ?? "<global>"}");
                md.AppendLine();
            }

            var kind = DescribeKind(t);
            var bases = new List<string>();
            if (t.BaseType is not null && t.BaseType.FullName is not ("System.Object" or "System.ValueType" or "System.Enum" or "System.MulticastDelegate"))
                bases.Add(FormatType(t.BaseType));
            bases.AddRange(t.GetInterfaces().Select(FormatType));
            var baseText = bases.Count > 0 ? " : " + string.Join(", ", bases) : "";

            var flags = new List<string>();
            if (t.IsAbstract && t.IsSealed) flags.Add("static");
            else if (t.IsAbstract && !t.IsInterface) flags.Add("abstract");
            else if (t.IsSealed && !t.IsValueType) flags.Add("sealed");
            if (t.IsValueType && HasAttr(t, "System.Runtime.CompilerServices.IsReadOnlyAttribute")) flags.Add("readonly struct");
            if (HasAttr(t, "System.Runtime.CompilerServices.IsByRefLikeAttribute")) flags.Add("ref struct");
            var flagText = flags.Count > 0 ? " [" + string.Join(", ", flags) + "]" : "";

            md.AppendLine($"### {t.Name}{GenericSuffix(t)} ({kind}{flagText}){baseText}");
            md.AppendLine();
            var constraints = ConstraintText(t.GetGenericArguments());
            if (constraints.Length > 0) md.AppendLine(constraints);

            var members = CollectMembers(t, nullability, md);
            md.AppendLine();

            jsonTypes.Add(new
            {
                fullName = t.FullName,
                kind,
                name = t.Name,
                @namespace = t.Namespace,
                bases,
                members,
            });
        }

        if (outJson is not null)
        {
            File.WriteAllText(outJson, JsonSerializer.Serialize(new { title, types = jsonTypes }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"wrote {outJson} ({types.Count} types)");
        }
        if (outMd is not null)
        {
            File.WriteAllText(outMd, md.ToString());
            Console.WriteLine($"wrote {outMd} ({types.Count} types)");
        }
        else
        {
            Console.Write(md.ToString());
        }
        return 0;
    }

    private static List<object> CollectMembers(Type t, Func<object, string?>? nullability, StringBuilder md)
    {
        var members = new List<object>();
        const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        if (t.IsEnum)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(f => f.GetRawConstantValue()))
            {
                var value = f.GetRawConstantValue();
                md.AppendLine($"- `{f.Name} = {value}`");
                members.Add(new { kind = "enum-value", name = f.Name, value = value?.ToString() });
            }
            return members;
        }

        foreach (var c in t.GetConstructors(Public).OrderBy(c => c.GetParameters().Length))
        {
            var sig = $"public {t.Name}{RenderParameters(c.GetParameters(), nullability)}";
            md.AppendLine($"- `{sig}`");
            members.Add(new { kind = "ctor", signature = sig });
        }

        if (t.BaseType?.FullName == "System.MulticastDelegate")
        {
            var invoke = t.GetMethod("Invoke");
            if (invoke is not null)
            {
                var sig = $"delegate {FormatReturn(invoke.ReturnType, invoke.ReturnParameter, nullability)} Invoke{RenderParameters(invoke.GetParameters(), nullability)}";
                md.AppendLine($"- `{sig}`");
                members.Add(new { kind = "delegate", signature = sig });
            }
        }

        foreach (var f in t.GetFields(Public).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            if (f.IsSpecialName) continue;
            var mod = f.IsLiteral ? "const " : f.IsStatic ? "static " : f.IsInitOnly ? "readonly " : "";
            var sig = $"public {mod}{FormatType(f.FieldType)} {f.Name}";
            md.AppendLine($"- `{sig}`");
            members.Add(new { kind = "field", signature = sig });
        }

        foreach (var p in t.GetProperties(Public).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var accessors = new List<string>();
            if (p.GetMethod is { IsPublic: true }) accessors.Add("get;");
            if (p.SetMethod is { IsPublic: true }) accessors.Add(IsInitOnly(p.SetMethod) ? "init;" : "set;");
            if (accessors.Count == 0) continue;
            var stat = (p.GetMethod ?? p.SetMethod)!.IsStatic ? "static " : "";
            var nullable = nullability?.Invoke(p);
            var nullableText = nullable is null or "NotNull" ? "" : $" (nullability: {nullable})";
            var index = p.GetIndexParameters();
            var indexText = index.Length > 0 ? RenderParameters(index, nullability) : "";
            var sig = $"public {stat}{FormatType(p.PropertyType)} {p.Name}{indexText} {{ {string.Join(" ", accessors)} }}{nullableText}";
            md.AppendLine($"- `{sig}`");
            members.Add(new { kind = "property", signature = sig });
        }

        foreach (var m in t.GetMethods(Public)
                     .Where(m => !m.IsSpecialName && !(t.BaseType?.FullName == "System.MulticastDelegate" && m.Name == "Invoke"))
                     .OrderBy(m => m.Name, StringComparer.Ordinal)
                     .ThenBy(m => m.GetParameters().Length))
        {
            var isExtension = HasAttr(m, "System.Runtime.CompilerServices.ExtensionAttribute");
            var returnText = FormatReturn(m.ReturnType, m.ReturnParameter, nullability);
            var generic = GenericSuffix(m);
            var parameters = RenderParameters(m.GetParameters(), nullability, extension: isExtension);
            var constraints = ConstraintText(m.GetGenericArguments());
            var sig = $"public {(m.IsStatic ? "static " : "")}{returnText} {m.Name}{generic}{parameters}";
            if (isExtension) sig = "[ext] " + sig;
            md.AppendLine($"- `{sig}`");
            if (constraints.Length > 0) md.Append(constraints);
            members.Add(new { kind = "method", signature = sig, extension = isExtension });
        }

        foreach (var m in t.GetMethods(Public)
                     .Where(m => m.IsSpecialName && m.Name.StartsWith("op_", StringComparison.Ordinal))
                     .OrderBy(m => m.Name, StringComparer.Ordinal)
                     .ThenBy(m => m.GetParameters().Length))
        {
            var sig = $"public static {FormatType(m.ReturnType)} {m.Name}{RenderParameters(m.GetParameters(), nullability)}";
            md.AppendLine($"- `{sig}`");
            members.Add(new { kind = "operator", signature = sig });
        }

        foreach (var e in t.GetEvents(Public).OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            var sig = $"public event {FormatType(e.EventHandlerType!)} {e.Name}";
            md.AppendLine($"- `{sig}`");
            members.Add(new { kind = "event", signature = sig });
        }

        return members;
    }

    private static bool IsInitOnly(MethodInfo setter)
    {
        try
        {
            return setter.ReturnParameter.GetRequiredCustomModifiers()
                .Any(t => t.FullName == "System.Runtime.CompilerServices.IsExternalInit");
        }
        catch
        {
            return false;
        }
    }

    private static bool HasAttr(object provider, string attributeFullName)
    {
        try
        {
            var data = provider switch
            {
                MemberInfo m => m.GetCustomAttributesData(),
                ParameterInfo p => p.GetCustomAttributesData(),
                _ => null,
            };
            return data is not null && data.Any(a => a.AttributeType.FullName == attributeFullName);
        }
        catch
        {
            return false;
        }
    }

    private static string RenderParameters(ParameterInfo[] parameters, Func<object, string?>? nullability, bool extension = false)
    {
        var parts = parameters.Select((p, i) =>
        {
            var mods = new List<string>();
            if (extension && i == 0) mods.Add("this");
            if (HasAttr(p, "System.ParamArrayAttribute")) mods.Add("params");
            if (p.IsOut) mods.Add("out");
            else if (p.ParameterType.IsByRef) mods.Add(p.IsIn ? "in" : "ref");
            var nullable = nullability?.Invoke(p);
            var isNullableValue = p.ParameterType.FullName?.StartsWith("System.Nullable`1", StringComparison.Ordinal) == true;
            var nullableText = nullable is null or "NotNull" or "Unknown" || (nullable == "Nullable" && isNullableValue) ? "" : "?";
            var prefix = mods.Count > 0 ? string.Join(" ", mods) + " " : "";
            return $"{prefix}{FormatType(p.ParameterType)}{nullableText} {p.Name}";
        });
        return "(" + string.Join(", ", parts) + ")";
    }

    // Return types follow the same rendering rule as parameters: a Nullable state appends "?"
    // unless the type already is System.Nullable<T>; NotNull and Unknown add nothing.
    private static string FormatReturn(Type returnType, ParameterInfo returnParameter, Func<object, string?>? nullability)
    {
        var text = FormatType(returnType);
        if (nullability?.Invoke(returnParameter) != "Nullable") return text;
        // FullName is null on open generics such as Nullable<T>, so compare the definition.
        var isNullableValue = returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition().FullName == "System.Nullable`1";
        return isNullableValue ? text : text + "?";
    }

    private static string GenericSuffix(Type t) => GenericSuffix(t.GetGenericArguments());
    private static string GenericSuffix(MethodInfo m) => GenericSuffix(m.GetGenericArguments());

    private static string GenericSuffix(Type[] args)
    {
        if (args.Length == 0) return "";
        var all = args.Select(a => a.IsGenericParameter ? a.Name : FormatType(a));
        return "<" + string.Join(", ", all) + ">";
    }

    private static string ConstraintText(Type[] args)
    {
        var sb = new StringBuilder();
        foreach (var a in args.Where(a => a.IsGenericParameter))
        {
            var parts = new List<string>();
            var attrs = a.GenericParameterAttributes;
            if ((attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0) parts.Add("class");
            if ((attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) parts.Add("struct");
            foreach (var c in a.GetGenericParameterConstraints().Where(c => c.FullName != "System.ValueType")) parts.Add(FormatType(c));
            if ((attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && (attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0) parts.Add("new()");
            if (parts.Count > 0) sb.AppendLine($"- `where {a.Name} : {string.Join(", ", parts)}`");
        }
        return sb.ToString();
    }

    private static bool IsIncluded(Type t, Regex[] includes, Regex[] excludes)
    {
        var name = t.FullName ?? t.Name;
        var included = includes.Length == 0 || includes.Any(r => r.IsMatch(name));
        if (!included) return false;
        return !excludes.Any(r => r.IsMatch(name));
    }

    private static string DescribeKind(Type t) =>
        t.IsInterface ? "interface" :
        t.IsEnum ? "enum" :
        t.IsValueType ? "struct" :
        t.BaseType?.FullName == "System.MulticastDelegate" ? "delegate" :
        "class";

    public static string FormatType(Type t)
    {
        if (t.IsByRef) return FormatType(t.GetElementType()!) + "&";
        if (t.IsArray) return FormatType(t.GetElementType()!) + "[]";
        if (t.IsGenericParameter) return t.Name;
        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            var name = (def.FullName ?? def.Name).Split('`')[0];
            var args = t.GetGenericArguments();
            if (def.FullName == "System.Nullable`1") return FormatType(args[0]) + "?";
            return $"{name}<{string.Join(", ", args.Select(FormatType))}>";
        }
        return t.FullName ?? t.Name;
    }
}
