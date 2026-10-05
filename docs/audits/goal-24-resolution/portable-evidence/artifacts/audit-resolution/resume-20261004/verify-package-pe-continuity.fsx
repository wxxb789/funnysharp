open System
open System.IO
open System.IO.Compression
open System.Reflection.Metadata
open System.Reflection.Metadata.Ecma335
open System.Reflection.PortableExecutable
open System.Security.Cryptography

let sha (bytes: byte array) = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()
let token (handle: EntityHandle) = MetadataTokens.GetToken handle
let packageEntry path name expected =
    use archive = ZipFile.OpenRead path
    let entries = archive.Entries |> Seq.filter (fun e -> e.FullName = name) |> Seq.toArray
    if entries.Length <> 1 then invalidOp ("Package entry missing/duplicate: " + name)
    use input = entries.[0].Open()
    use output = new MemoryStream()
    input.CopyTo output
    let bytes = output.ToArray()
    if sha bytes <> expected then invalidOp ("PE input hash mismatch: " + path)
    bytes
let typeName (reader: MetadataReader) (handle: EntityHandle) =
    match handle.Kind with
    | HandleKind.TypeReference ->
        let t = reader.GetTypeReference(TypeReferenceHandle.op_Explicit handle)
        reader.GetString t.Namespace + "." + reader.GetString t.Name
    | HandleKind.TypeDefinition ->
        let t = reader.GetTypeDefinition(TypeDefinitionHandle.op_Explicit handle)
        reader.GetString t.Namespace + "." + reader.GetString t.Name
    | _ -> string handle.Kind + ":" + string (token handle)
let attributes (reader: MetadataReader) =
    [| for handle in reader.CustomAttributes do
        let a = reader.GetCustomAttribute handle
        let owner =
            match a.Constructor.Kind with
            | HandleKind.MemberReference -> reader.GetMemberReference(MemberReferenceHandle.op_Explicit a.Constructor).Parent
            | HandleKind.MethodDefinition -> reader.GetMethodDefinition(MethodDefinitionHandle.op_Explicit a.Constructor).GetDeclaringType() |> TypeDefinitionHandle.op_Implicit
            | _ -> invalidOp "Unsupported attribute constructor"
        let bytes = reader.GetBlobBytes a.Value
        let decoded =
            if typeName reader owner = "System.Reflection.AssemblyInformationalVersionAttribute" then
                let mutable blob = reader.GetBlobReader a.Value
                if blob.ReadUInt16() <> 1us then invalidOp "Invalid custom attribute prolog"
                blob.ReadSerializedString()
            else ""
        yield token a.Parent, token a.Constructor, typeName reader owner, sha bytes, decoded |]
let methods (pe: PEReader) (reader: MetadataReader) =
    [| for handle in reader.MethodDefinitions do
        let m = reader.GetMethodDefinition handle
        let body =
            if m.RelativeVirtualAddress = 0 then "NO_BODY" else
                let b = pe.GetMethodBody m.RelativeVirtualAddress
                let regions = [| for r in b.ExceptionRegions -> int r.Kind, r.TryOffset, r.TryLength, r.HandlerOffset, r.HandlerLength, r.FilterOffset, token r.CatchType |]
                sprintf "%s|%d|%b|%d|%A" (sha (b.GetILBytes())) b.MaxStack b.LocalVariablesInitialized (token (StandaloneSignatureHandle.op_Implicit b.LocalSignature)) regions
        yield token (MethodDefinitionHandle.op_Implicit handle), reader.GetString m.Name, sha (reader.GetBlobBytes m.Signature), int m.Attributes, int m.ImplAttributes, body |]
let root = "Q:/repos/funnysharp-goal24-resolution/"
let oldFeed = root + "artifacts/audit-resolution/u14-post-xml/attempt-03/feed/"
let currentFeed = root + "artifacts/audit-resolution/u15-authorized/ci-d8744-final-run-retained/canonical-candidate-37135069130-1/packages/"
let pairs = [ "FunnySharp", "6a903f737356fdf67845c9cd39385e4a5e2b39bcae0ee92ff2882480ecb7e49d", "b3f2d4dd99e09edf4ebaa5d68161aff0d7763f2ba174fc6c784614ef6046d893";
              "FunnySharp.AspNetCore", "2daaff9c72db5b730c06d3270c00dfd7046fa27e26d59374e31cf270b8dc30c2", "4926aef5f311f80ee93e07243acb98147cf55fcf3080bc3e96152f3bf90a1b29" ]
for name, oldHash, currentHash in pairs do
    let entry = "lib/net10.0/" + name + ".dll"
    let oldBytes = packageEntry (oldFeed + name + ".0.2.0.nupkg") entry oldHash
    let newBytes = packageEntry (currentFeed + name + ".0.2.0.nupkg") entry currentHash
    use oldStream = new MemoryStream(oldBytes, false)
    use newStream = new MemoryStream(newBytes, false)
    use oldPe = new PEReader(oldStream)
    use newPe = new PEReader(newStream)
    let oldReader, newReader = oldPe.GetMetadataReader(), newPe.GetMetadataReader()
    let oldMethods, newMethods = methods oldPe oldReader, methods newPe newReader
    if oldMethods.Length <> newMethods.Length then invalidOp "Method count mismatch"
    let differences = Array.zip oldMethods newMethods |> Array.filter (fun (a,b) -> a <> b)
    printfn "PE_METHODS name=%s old=%s current=%s counts=%d/%d differences=%d" name oldHash currentHash oldMethods.Length newMethods.Length differences.Length
    if differences.Length <> 0 then printfn "METHOD_DIFF %A" differences; invalidOp "Method continuity failed"
    let oldAttributes, newAttributes = attributes oldReader, attributes newReader
    if oldAttributes.Length <> newAttributes.Length then invalidOp "Custom attribute count mismatch"
    let attrDifferences = Array.zip oldAttributes newAttributes |> Array.filter (fun (a,b) -> a <> b)
    printfn "PE_CUSTOM_ATTRIBUTES name=%s counts=%d/%d differences=%d" name oldAttributes.Length newAttributes.Length attrDifferences.Length
    for oldValue, newValue in attrDifferences do
        printfn "CUSTOM_ATTRIBUTE_DIFF old=%A current=%A" oldValue newValue
        let oldParent, oldConstructor, oldType, oldBlob, oldVersion = oldValue
        let newParent, newConstructor, newType, newBlob, newVersion = newValue
        if oldParent <> 536870913 || newParent <> oldParent || newConstructor <> oldConstructor
           || oldType <> "System.Reflection.AssemblyInformationalVersionAttribute" || newType <> oldType
           || oldBlob <> "c45ddbd1ad8f134faef9542cea99b3fe4c6a3e37e01dafec3c8d03c9ae52efab"
           || newBlob <> "f0d10c8605324e37d40afaf4ea625f5812b03c2c257d9b2ddec628517d6ea2ca"
           || oldVersion <> "0.2.0+a8863473fd53eddc7cb47201508426db2e454f84"
           || newVersion <> "0.2.0+d8744c934d86833f9817245ecc7c78b1177b8b76" then
            invalidOp "Unexpected custom attribute difference"
    if attrDifferences.Length <> 1 then invalidOp "Expected exactly the observed assembly commit stamp delta"
    printfn "PE_ATTRIBUTE_CONTINUITY name=%s onlyKnownAssemblyCommitStampChanged=true" name
printfn "PE_CONTINUITY_VERIFIED complete method bodies/signatures/flags and all custom attributes match except the exact source-bound assembly commit stamp."
