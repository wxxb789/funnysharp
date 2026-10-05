#r "System.Formats.Tar.dll"

open System
open System.IO
open System.IO.Compression
open System.Formats.Tar
open System.Text
open System.Text.Json
open System.Collections.Generic
open System.Security.Cryptography
open System.Text.RegularExpressions

let root = fsi.CommandLineArgs |> Array.skip 1 |> Array.exactlyOne |> Path.GetFullPath
let packet = "docs/reviews/goal24-pr-20261004/traversal-r9-performance-evidence"
let hash (bytes: byte array) = bytes |> SHA256.HashData |> Convert.ToHexStringLower
let document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, packet, "catalog.json")))
let catalog = document.RootElement
let original = catalog.GetProperty("originalWorkspace").GetString().TrimEnd('/')
let patterns =
    [|
        "aws-access-id", @"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b"
        "github-token", @"\b(?:gh[pousr]_[A-Za-z0-9]{30,255}|github_pat_[A-Za-z0-9_]{40,255})\b"
        "gitlab-token", @"\bglpat-[A-Za-z0-9_-]{20,255}\b"
        "slack-token", @"\bxox[baprs]-[A-Za-z0-9-]{20,255}\b"
        "stripe-live-key", @"\b(?:sk|rk)_live_[A-Za-z0-9]{16,255}\b"
        "google-api-key", @"\bAIza[A-Za-z0-9_-]{35}\b"
        "private-key-header", @"-----BEGIN (?:RSA |EC |OPENSSH |DSA |ENCRYPTED )?PRIVATE KEY-----"
        "jwt", @"\beyJ[A-Za-z0-9_-]{8,}\.eyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{16,}\b"
        "bearer-value", @"(?i)\bbearer[ \t]+[A-Za-z0-9._~+/-]{24,}={0,2}"
        "credential-assignment", @"(?i)\b(?:password|passwd|client_secret|api_key|access_token|secret_access_key|accountkey)[""']?[ \t]*[=:][ \t]*[""']?[A-Za-z0-9+/._~-]{16,}={0,2}"
        "azure-sas-signature", @"(?i)[?&]sig=[A-Za-z0-9%+/]{32,}={0,2}"
    |]
    |> Array.map (fun (name, pattern) -> name, Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds 5.0))
let findings = ResizeArray<obj>()
let mutable objects = 0
let mutable decodedBytes = 0L
let mutable archiveEntries = 0
let mutable embeddedPayloads = 0
let mutable nestedBytes = 0L
let seenEmbedded = HashSet<string>()
let rec scan (bytes: byte array) (kind: string) (depth: int) =
    if depth > 12 then failwith "Archive nesting exceeds scanner bound"
    let objectHash = hash bytes
    let views = [| "latin1", Encoding.Latin1.GetString bytes, 1, 0; "utf16le-even", Encoding.Unicode.GetString bytes, 2, 0; "utf16le-odd", (if bytes.Length > 1 then Encoding.Unicode.GetString(bytes, 1, bytes.Length - 1) else ""), 2, 1 |]
    for encoding, value, scale, shift in views do
        for name, regex in patterns do
            for result in regex.Matches value do
                findings.Add(box {| sha256 = objectHash; pattern = name; encoding = encoding; byteOffset = result.Index * scale + shift; byteLength = result.Length * scale; containerKind = kind |})
    let text = Encoding.Latin1.GetString bytes
    for result in Regex.Matches(text, @"(?m)^[ \t]*// funnysharp-workload:([A-Za-z0-9+/]+={0,2})[ \t]*\r?$") do
        let payload = Convert.FromBase64String result.Groups[1].Value
        if seenEmbedded.Add(hash payload) then
            embeddedPayloads <- embeddedPayloads + 1
            nestedBytes <- nestedBytes + int64 payload.Length
            scan payload "decoded-workload-json" (depth + 1)
    if bytes.Length >= 4 && bytes[0] = 0x50uy && bytes[1] = 0x4buy && bytes[2] = 0x03uy && bytes[3] = 0x04uy then
        use stream = new MemoryStream(bytes, false)
        use archive = new ZipArchive(stream, ZipArchiveMode.Read)
        for entry in archive.Entries do
            if not (entry.FullName.EndsWith("/", StringComparison.Ordinal)) then
                use input = entry.Open()
                use output = new MemoryStream()
                input.CopyTo output
                let content = output.ToArray()
                archiveEntries <- archiveEntries + 1
                nestedBytes <- nestedBytes + int64 content.Length
                scan content "zip-entry" (depth + 1)
    elif bytes.Length > 262 && Encoding.ASCII.GetString(bytes, 257, 5) = "ustar" then
        use stream = new MemoryStream(bytes, false)
        use archive = new TarReader(stream)
        let mutable entry = archive.GetNextEntry()
        while not (isNull entry) do
            if not (isNull entry.DataStream) then
                use output = new MemoryStream()
                entry.DataStream.CopyTo output
                let content = output.ToArray()
                archiveEntries <- archiveEntries + 1
                nestedBytes <- nestedBytes + int64 content.Length
                scan content "tar-entry" (depth + 1)
            entry <- archive.GetNextEntry()
    elif bytes.Length >= 2 && bytes[0] = 0x1fuy && bytes[1] = 0x8buy then
        use stream = new MemoryStream(bytes, false)
        use archive = new GZipStream(stream, CompressionMode.Decompress)
        use output = new MemoryStream()
        archive.CopyTo output
        let content = output.ToArray()
        archiveEntries <- archiveEntries + 1
        nestedBytes <- nestedBytes + int64 content.Length
        scan content "gzip-content" (depth + 1)
for item in catalog.GetProperty("objects").EnumerateArray() do
    let locator = item.GetProperty("physicalPath").GetString()
    if not (locator.StartsWith(original + "/", StringComparison.Ordinal)) then failwith "Object locator outside packet root"
    let encoded = File.ReadAllBytes(Path.Combine(root, locator.Substring(original.Length + 1)))
    if hash encoded <> item.GetProperty("physicalSha256").GetString() then failwith "Physical scan input mismatch"
    let bytes = Convert.FromBase64String(Encoding.UTF8.GetString encoded)
    if hash bytes <> item.GetProperty("sha256").GetString() then failwith "Decoded scan input mismatch"
    objects <- objects + 1
    decodedBytes <- decodedBytes + int64 bytes.Length
    scan bytes "decoded-object" 0
printfn "%s" (JsonSerializer.Serialize({| schema = "funnysharp-bounded-credential-pattern-scan/v1"; objects = objects; decodedBytes = decodedBytes; embeddedPayloads = embeddedPayloads; archiveEntries = archiveEntries; nestedBytes = nestedBytes; patterns = patterns |> Array.map fst; findings = findings.ToArray(); writes = 0; network = 0; limit = "Pattern scan of retained decoded bytes and recognized ZIP/TAR/GZIP entries plus workload payloads; no universal credential clearance, entropy proof or encrypted archive coverage." |}))
