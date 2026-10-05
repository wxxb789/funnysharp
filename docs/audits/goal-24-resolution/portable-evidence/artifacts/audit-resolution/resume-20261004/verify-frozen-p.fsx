open System
open System.IO
open System.Text.Json
open System.Security.Cryptography
open System.Text.RegularExpressions

let root = fsi.CommandLineArgs |> Array.skip 1 |> Array.exactlyOne
let pBytes = File.ReadAllBytes(Path.Combine(root, "P.json"))
let document = JsonDocument.Parse(ReadOnlyMemory pBytes)
let p = document.RootElement
let text key (node: JsonElement) = node.GetProperty(key: string).GetString()
let sha (bytes: byte array) = Convert.ToHexString(SHA256.HashData bytes).ToLowerInvariant()
let mutable files = 0
let mutable totalBytes = 0L
for inventory, pathKey, hashKey in [ "evidence", "path", "sha256"; "http", "bodyPath", "bodySha256" ] do
    for entry in p.GetProperty(inventory).EnumerateArray() do
        let path = text pathKey entry
        if not (Regex.IsMatch(path, "^(payload/[A-Za-z0-9][A-Za-z0-9._-]{0,127}|http/[0-9]+[.]body)$")) then invalidOp "Unsafe P attachment path."
        use stream = File.OpenRead(Path.Combine(root, path))
        let actual = Convert.ToHexString(SHA256.HashData stream).ToLowerInvariant()
        if actual <> text hashKey entry then invalidOp ("P attachment hash mismatch: " + path)
        if inventory = "evidence" && stream.Length <> entry.GetProperty("length").GetInt64() then invalidOp ("P attachment length mismatch: " + path)
        files <- files + 1
        totalBytes <- totalBytes + stream.Length
let expected = p.GetProperty("evidence").GetArrayLength() + p.GetProperty("http").GetArrayLength()
if files <> expected then invalidOp "Incomplete P attachment inventory."
Console.WriteLine(sprintf "P_ATTACHMENT_VERIFIED files=%d bytes=%d pSha256=%s pBytes=%d" files totalBytes (sha pBytes) pBytes.Length)
document.Dispose()

