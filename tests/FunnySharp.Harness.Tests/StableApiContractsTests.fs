module FunnySharp.Harness.Tests.StableApiContractsTests

open System
open System.IO
open System.Security.Cryptography
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Xunit
open FunnySharp.Harness.StableApiContracts
open FunnySharp.Harness.Tests.Support

let private sha (text: string) =
    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes text)).ToLowerInvariant()

let private expected = Map.ofList [ "C:3", ("ASSEMBLY Fixture", "CLASS Fixture.Value", "  METHOD System.Int32 Read()") ]

let private present (node: JsonNode | null) : JsonNode =
    match node with
    | null -> invalidOp "The controlled JSON fixture is missing a required node."
    | value -> value

let private parse (text: string) = present (JsonNode.Parse text)
let private get (node: JsonNode) (name: string) = present node.[name]
let private at (node: JsonNode) (index: int) = present node.[index]

let private fixture root =
    let source = "public static int Read() => 1;"
    let assertion = "public void Test() { Assert.Equal(1, Read()); }"
    let xml = "<member name=\"M:Fixture.Value.Read\"><summary>Value.</summary></member>"
    let write (name: string) (text: string) =
        File.WriteAllText(Path.Combine(root, name), text, UTF8Encoding(false))
        sha text
    let sourceSha = write "Proof.cs" source
    let assertionSha = write "Assertions.cs" assertion
    let xmlSha = write "Proof.xml" xml
    let receipt = """{"results":{"summary":{"tests":1,"passed":1},"tests":[{"name":"Fixture.Test","status":"passed","extra":{"id":"case-1","type":"Fixture","method":"Test"}}]}}"""
    let receiptSha = write "runtime.json" receipt
    let compiler = """{"rows":[{"identity":"C:3","proofs":[{"Source":"Proof.cs","SourceSha256":"@SOURCE@","ExpectedDiagnostics":[],"ActualDiagnostics":[],"EmitSucceeded":true,"ExactBindingObserved":true,"Terminal":"PASS"}]}]}""".Replace("@SOURCE@", sourceSha)
    let compilerSha = write "compiler.json" compiler
    let template = """
    {
      "fileHashes":{"Proof.cs":"@SOURCE@","Assertions.cs":"@ASSERTION@"},
      "sourceUnits":{"S1":{"path":"Proof.cs","fileSha256":"@SOURCE@","startLine":1,"endLine":1,"code":"public static int Read() => 1;","codeSha256":"@SOURCE@"}},
      "assertionUnits":{"A1":{"path":"Assertions.cs","sha256":"@ASSERTION@","start":1,"end":1,"methodBody":"public void Test() { Assert.Equal(1, Read()); }","assertionLines":[{"line":1,"text":"public void Test() { Assert.Equal(1, Read()); }"}],"helperSpans":[]}},
      "caseCatalog":{"R1":{"name":"Fixture.Test","type":"Fixture","method":"Test","assertionSource":"A1","executions":[{"receipt":"runtime.json","receiptCaseId":"case-1"}]}},
      "finalReceipts":[{"path":"runtime.json","sha256":"@RECEIPT@"}],
      "sourceTransfers":[],
      "finalCompiler":{"path":"compiler.json","sha256":"@COMPILER@"},
      "compilerProofs":{"C:3":{"proofs":[{}]}},
      "xmlNodes":{"Fixture|M:Fixture.Value.Read":{}},
      "rows":[{"identity":"C:3","assembly":"Fixture","assemblyHeader":"ASSEMBLY Fixture","typeHeader":"CLASS Fixture.Value","memberLine":"  METHOD System.Int32 Read()","dimensions":{}}]
    }
    """
    let text = template.Replace("@SOURCE@", sourceSha).Replace("@ASSERTION@", assertionSha).Replace("@RECEIPT@", receiptSha).Replace("@COMPILER@", compilerSha)
    let index = (parse text).AsObject()
    let dimensions = (get (at (get index "rows") 0) "dimensions").AsObject()
    for name in [ "nameAndOutput"; "compilerNullability"; "defaultAndGuards"; "exceptionsCancellationStatusToken"; "callbacksOrderingConsumption"; "counterpartOrAbsence"; "xmlAndAliases"; "requiredRuntimeClauses" ] do
        dimensions.[name] <- parse """{"applicability":"applicable","claim":"A machine-consumed clause.","sourceRefs":["S1"],"runtimeCaseRefs":["R1"]}"""
    (get dimensions "requiredRuntimeClauses").["proofs"] <- parse """[{"caseRef":"R1"}]"""
    let xmlDimension = get dimensions "xmlAndAliases"
    xmlDimension.["packageXmlPath"] <- JsonValue.Create "Proof.xml"
    xmlDimension.["packageXmlSha256"] <- JsonValue.Create xmlSha
    xmlDimension.["directNode"] <- parse """{"id":"M:Fixture.Value.Read"}"""
    xmlDimension.["alias"] <- null
    index

let private check root (index: JsonObject) =
    use document = JsonDocument.Parse(index.ToJsonString())
    validateProofIndex root expected document.RootElement

type StableApiContractsTests() =
    [<Fact>]
    member _.ExactByteBoundReferencesAndActualCaseIdJoinPass() =
        use temp = new TempDirectory()
        check temp.Path (fixture temp.Path)

    [<Theory>]
    [<InlineData("member")>]
    [<InlineData("dimension")>]
    [<InlineData("runtime-proof")>]
    [<InlineData("case-id")>]
    [<InlineData("assertion")>]
    [<InlineData("compiler-proof")>]
    [<InlineData("xml-alias")>]
    [<InlineData("source-hash")>]
    [<InlineData("package-xml-hash")>]
    [<InlineData("unjustified-na")>]
    member _.MissingOrStaleApplicableEvidenceIsRejected(kind: string) =
        use temp = new TempDirectory()
        let index = fixture temp.Path
        let dimensions = get (at (get index "rows") 0) "dimensions"
        match kind with
        | "member" -> (get index "rows").AsArray().Clear()
        | "dimension" -> dimensions.AsObject().Remove("defaultAndGuards") |> ignore
        | "runtime-proof" -> (get (get dimensions "requiredRuntimeClauses") "proofs").AsArray().Clear()
        | "case-id" -> (at (get (get (get index "caseCatalog") "R1") "executions") 0).["receiptCaseId"] <- JsonValue.Create "not-executed"
        | "assertion" -> (at (get (get (get index "assertionUnits") "A1") "assertionLines") 0).["text"] <- JsonValue.Create "Assert.Equal(2, Read());"
        | "compiler-proof" -> (get (get (get index "compilerProofs") "C:3") "proofs").AsArray().Clear()
        | "xml-alias" -> (get dimensions "xmlAndAliases").["directNode"] <- null
        | "source-hash" -> (get index "fileHashes").["Proof.cs"] <- JsonValue.Create(String.replicate 64 "0")
        | "package-xml-hash" -> (get dimensions "xmlAndAliases").["packageXmlSha256"] <- JsonValue.Create(String.replicate 64 "0")
        | "unjustified-na" -> (get dimensions "requiredRuntimeClauses").["applicability"] <- JsonValue.Create "notApplicable"
        | _ -> invalidArg "kind" kind
        Assert.Throws<InvalidOperationException>(fun () -> check temp.Path index) |> ignore
