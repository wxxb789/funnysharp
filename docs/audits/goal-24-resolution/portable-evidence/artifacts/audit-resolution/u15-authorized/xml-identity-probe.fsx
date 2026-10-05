open System
open System.Reflection
open System.Security.Cryptography
open System.IO
for path in fsi.CommandLineArgs |> Array.skip 1 do
    let assembly = Assembly.LoadFile(Path.GetFullPath path)
    let version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    printfn "FILE=%s" path
    printfn "SHA256=%s" (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes path)).ToLowerInvariant())
    printfn "MVID=%O" assembly.ManifestModule.ModuleVersionId
    printfn "INFORMATIONAL_VERSION=%s" version.InformationalVersion
