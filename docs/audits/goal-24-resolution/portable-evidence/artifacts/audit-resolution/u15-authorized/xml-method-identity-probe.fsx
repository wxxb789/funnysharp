open System
open System.IO
open System.Reflection.Metadata
open System.Reflection.PortableExecutable
open System.Security.Cryptography
let hash (bytes: byte array) = Convert.ToHexString(SHA256.HashData bytes)
let readMethods path =
    use stream = File.OpenRead path
    use pe = new PEReader(stream)
    let metadata = pe.GetMetadataReader()
    [| for handle in metadata.MethodDefinitions do
           let method = metadata.GetMethodDefinition handle
           let signature = hash (metadata.GetBlobBytes method.Signature)
           if method.RelativeVirtualAddress = 0 then
               yield metadata.GetString method.Name, signature, "NO_IL", int method.Attributes, int method.ImplAttributes
           else
               let body = pe.GetMethodBody method.RelativeVirtualAddress
               yield metadata.GetString method.Name, signature, hash (body.GetILBytes()), int method.Attributes, int method.ImplAttributes |]
let paths = fsi.CommandLineArgs |> Array.skip 1
for index in [0; 2] do
    let oldMethods = readMethods paths.[index]
    let newMethods = readMethods paths.[index+1]
    printfn "PAIR=%s,%s" paths.[index] paths.[index+1]
    printfn "METHOD_COUNTS=%d,%d" oldMethods.Length newMethods.Length
    let differences = Array.zip oldMethods newMethods |> Array.indexed |> Array.filter(fun (_, (oldValue,newValue)) -> oldValue <> newValue)
    printfn "METHOD_SIGNATURE_IL_ATTRIBUTE_DIFFERENCES=%d" differences.Length
    if differences.Length <> 0 then printfn "%A" differences
