using System.Reflection;

static void Dump(Type type, string[] interest)
{
    Console.WriteLine($"=== {type.FullName} ===");
    foreach (var member in type
                 .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                 .Where(member => member is MethodInfo { IsSpecialName: false })
                 .Where(member => interest.Length == 0 || interest.Any(token => member.Name.Contains(token, StringComparison.Ordinal)))
                 .OrderBy(member => member.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"  {member}");
    }
}

Dump(typeof(LanguageExt.Option<int>), ["None"]);
Console.WriteLine();
Dump(typeof(LanguageExt.Prelude), ["None", "Some"]);
Console.WriteLine();
Dump(typeof(Funcky.Monads.Option<int>), ["None", "Some", "Return"]);
Console.WriteLine();
var cfeAssembly = typeof(CSharpFunctionalExtensions.Result<int, string>).Assembly;
foreach (var type in cfeAssembly.GetTypes().Where(t => t.Name.Contains("ResultExtensions", StringComparison.Ordinal)).OrderBy(t => t.FullName, StringComparer.Ordinal))
{
    if (type.IsAbstract && type.IsSealed)
    {
        Dump(type, ["Map", "Bind", "Ensure", "Match", "GetValueOr", "Default", "OnSuccess", "With"]);
        Console.WriteLine();
    }
}
