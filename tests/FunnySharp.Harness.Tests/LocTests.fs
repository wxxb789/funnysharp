module FunnySharp.Harness.Tests.LocTests

// Parity facts for the two call-site counters ported from
// docs/next-stage/call-sites-code/tools/{loc,rawloc}.py and runner.py's
// count_loc. The two counters differ on purpose, so the encoding/BOM and
// block-comment facts pin each one's own rule.

open System
open System.IO
open System.Text
open Xunit
open FunnySharp.Harness.Loc
open FunnySharp.Harness.Tests.Support

let private utf8NoBom = UTF8Encoding(false)

let private writeText (directory: string) (name: string) (text: string) : string =
    let path = Path.Combine(directory, name)
    File.WriteAllText(path, text, utf8NoBom)
    path

let private writeBomText (directory: string) (name: string) (text: string) : string =
    let path = Path.Combine(directory, name)
    File.WriteAllBytes(path, Array.append [| 0xEFuy; 0xBBuy; 0xBFuy |] (utf8NoBom.GetBytes text))
    path

let private extractFrom (name: string) (text: string) : (string list * string list) option =
    use temp = new TempDirectory()
    let path = writeText temp.Path "fixture.cs" text
    extract path name

let private callSitesCodeDir () : string =
    Path.Combine(repositoryRoot (), "docs", "next-stage", "call-sites-code")

type ExtractTests() =

    [<Fact>]
    member _.ExtractFindsFirstDeclarationLine() =
        let text = "public class C\n{\n    public int FirstMethod()\n    {\n        return 1;\n    }\n\n    public int SecondMethod()\n    {\n        return 2;\n    }\n}\n"

        match extractFrom "FirstMethod" text with
        | None -> Assert.Fail "expected a body for FirstMethod"
        | Some(body, _) ->
            Assert.Contains("FirstMethod", body.[0])
            Assert.DoesNotContain(body, fun line -> line.Contains "SecondMethod")

    [<Fact>]
    member _.ExtractStopsAtBalancedClosingBrace() =
        let text = "public class C\n{\n    public int M()\n    {\n        if (true)\n        {\n            return 1;\n        }\n\n        return 2;\n    }\n\n    public int N()\n    {\n        return 3;\n    }\n}\n"

        match extractFrom "M" text with
        | None -> Assert.Fail "expected a body for M"
        | Some(body, _) ->
            Assert.Equal("    }", List.last body)
            Assert.DoesNotContain(body, fun line -> line.Contains "N()")

    [<Fact>]
    member _.ExtractStopsAtSemicolonForExpressionBody() =
        let text = "public class C\n{\n    public int M() => 42;\n    public int N() => 7;\n}\n"

        match extractFrom "M" text with
        | None -> Assert.Fail "expected a body for M"
        | Some(body, raw) ->
            Assert.Equal<string list>([ "    public int M() => 42;" ], body)
            Assert.Equal(1, raw.Length)

    [<Fact>]
    member _.ExtractExcludesBlankAndSlashSlashLines() =
        let text = "public class C\n{\n    public int M()\n    {\n        // a comment\n\n        return 1;\n    }\n}\n"

        match extractFrom "M" text with
        | None -> Assert.Fail "expected a body for M"
        | Some(body, raw) ->
            Assert.Equal(6, body.Length)
            Assert.Equal(4, raw.Length)
            Assert.DoesNotContain(raw, fun line -> line.Contains "a comment")

    [<Fact>]
    member _.ExtractCountsBlockCommentLinesAsRaw() =
        let text = "public class C\n{\n    public int M()\n    {\n        /* block */\n        return 1;\n    }\n}\n"

        match extractFrom "M" text with
        | None -> Assert.Fail "expected a body for M"
        | Some(_, raw) ->
            Assert.Equal(5, raw.Length)
            Assert.Contains(raw, fun line -> line.Contains "/* block */")

    [<Fact>]
    member _.ExtractReturnsNoneWhenNameAbsent() =
        let text = "public class C\n{\n    public int M()\n    {\n        return 1;\n    }\n}\n"
        Assert.True(Option.isNone (extractFrom "NoSuchMethod" text))

    [<Fact>]
    member _.ExtractKeepsDeclarationAndClosingLines() =
        let text = "public class C\n{\n    public int M()\n    {\n        return 1;\n    }\n}\n"

        match extractFrom "M" text with
        | None -> Assert.Fail "expected a body for M"
        | Some(_, raw) ->
            Assert.Contains("M()", List.head raw)
            Assert.Equal("    }", List.last raw)

    [<Fact>]
    member _.ExtractSplitsOnLfOnlySoCrLfKeepsCarriageReturn() =
        let text = "class C {\r\n    public int M()\r\n    {\r\n        return 1;\r\n    }\r\n}\r\n"

        match extractFrom "M" text with
        | None -> Assert.Fail "expected a body for M"
        | Some(body, _) -> Assert.True(body |> List.forall (fun line -> line.EndsWith "\r"))

    [<Fact>]
    member _.ExtractKeepsBomOnFirstLine() =
        use temp = new TempDirectory()
        let path = writeBomText temp.Path "bom.cs" "public int M()\n{\n    return 1;\n}\n"

        match extract path "M" with
        | None -> Assert.Fail "expected a body for M"
        | Some(body, _) -> Assert.StartsWith("\uFEFF", body.[0])

type CountLocTests() =

    [<Fact>]
    member _.CountLocTextCountsTrailingCommentButNotLoneComment() =
        Assert.Equal(1, countLocText "code; // trailing\n// lone\n\n")

    [<Fact>]
    member _.CountLocStripsBlockCommentsAcrossLines() =
        // DOTALL removal joins the surrounding text: the three comment lines
        // collapse into the space between "line1 " and "line2".
        Assert.Equal(2, countLocText "line1 /*\n commented\n*/\nline2\n")

    [<Fact>]
    member _.CountLocStripsBomSoLeadingCommentIsExcluded() =
        use temp = new TempDirectory()
        let path = writeBomText temp.Path "bom.cs" "// leading comment\nint x = 1;\n"
        Assert.Equal(1, countLoc path)

    [<Fact>]
    member _.CountLocReadsUtf8AndSplitsOnUnicodeBoundaries() =
        Assert.Equal(2, countLocText "a\r\nb\r\n")

type RawLocTests() =

    let expectedReport =
        [ "W1  dictionary+fallback            idiomatic/Workflows.cs       raw=  9  (TimeoutSeconds=9)"
          "W1  dictionary+fallback            funnysharp/Workflows.cs      raw=  9  (TimeoutSeconds=9)"
          "W1  dictionary+fallback            competitors/W1Funcky.cs      raw=  4  (TimeoutSeconds=4)"
          "W2  fail-fast pipeline             idiomatic/Workflows.cs       raw= 27  (CreateInvoiceAsync=27)"
          "W2  fail-fast pipeline             funnysharp/Workflows.cs      raw= 28  (CreateInvoiceAsync=13, FindCustomerAsync=10, InvalidQuantityFor=5)"
          "W2  fail-fast pipeline             competitors/W2Cfe.cs         raw= 21  (CreateInvoiceAsync=16, InvalidQuantityFor=5)"
          "W3  accumulate fields              idiomatic/Workflows.cs       raw= 17  (ValidateSignup=17)"
          "W3  accumulate fields              funnysharp/Workflows.cs      raw= 17  (ValidateSignup=5, ValidateEmail=4, ValidatePassword=4, ValidateAge=4)"
          "W3  accumulate fields              competitors/W3LanguageExt.cs raw= 15  (ValidateSignup=3, ValidateEmail=4, ValidatePassword=4, ValidateAge=4)"
          "W3b accumulate fields (CFE)        competitors/W3bCfe.cs        raw= 20  (ValidateSignup=8, ValidateEmail=4, ValidatePassword=4, ValidateAge=4)"
          "W4  unit-result                    idiomatic/Workflows.cs       raw= 15  (DeleteOrderAsync=15)"
          "W4  unit-result                    funnysharp/Workflows.cs      raw= 15  (DeleteOrderAsync=15)"
          "W4  unit-result                    competitors/W4Cfe.cs         raw= 15  (DeleteOrderAsync=15)"
          "W5  tap/observation                idiomatic/Workflows.cs       raw=  8  (TotalWithAudit=8)"
          "W5  tap/observation                funnysharp/Workflows.cs      raw=  3  (TotalWithAudit=3)"
          "W5  tap/observation                competitors/W5Cfe.cs         raw=  3  (TotalWithAudit=3)"
          "W6  traversal+index                idiomatic/Workflows.cs       raw= 27  (ParseAll=16, ParseLine=11)"
          "W6  traversal+index                funnysharp/Workflows.cs      raw= 16  (ParseAll=4, ParseLine=12)"
          "W7  bounded parallel (op+consumer) idiomatic/Workflows.cs       raw= 65  (SelectParallelAsync=53, SumFetchedAsync=12)"
          "W7  bounded parallel (consumer)    funnysharp/Workflows.cs      raw= 14  (SumFetchedAsync=14)"
          "W8  first success                  idiomatic/Workflows.cs       raw= 25  (FirstInvoiceAsync=25)"
          "W8  first success                  funnysharp/Workflows.cs      raw= 14  (FirstInvoiceAsync=14)"
          "W9  env+resource                   idiomatic/Workflows.cs       raw= 11  (LoadOrderTotalAsync=11)"
          "W9  env+resource                   funnysharp/Workflows.cs      raw= 14  (LoadOrderTotalAsync=14)"
          "W10 nested update                  idiomatic/Workflows.cs       raw=  9  (ReviewPostalCode=9)"
          "W10 nested update                  funnysharp/Workflows.cs      raw=  4  (ReviewPostalCode=4)"
          "W10 nested update                  competitors/W10LanguageExt.cs raw=  4  (ReviewPostalCode=4)"
          "W11 http idiomatic                 funnysharp-aspnet/Workflows.cs raw= 43  (GetCustomerIdiomatic=13, CreateOrderIdiomaticAsync=15, SignUpIdiomatic=15)"
          "W11 http funny                     funnysharp-aspnet/Workflows.cs raw= 33  (GetCustomer=10, CreateOrder=8, SignUp=4, CreateOrderFromEffectAsync=11)"
          "TOTAL raw lines counted: 505" ]

    [<Fact>]
    member _.ReportMatchesCommittedFixture() =
        Assert.Equal<string list>(expectedReport, reportLines (callSitesCodeDir ()))

    [<Fact>]
    member _.ReportHasOneLinePerTargetPlusTotal() =
        // The source TARGETS table has 29 rows; with the TOTAL line the report
        // prints 30 lines. (The spec's "30 TARGETS" counts the output lines.)
        Assert.Equal(29, List.length targets)
        Assert.Equal(List.length targets + 1, List.length (reportLines (callSitesCodeDir ())))

    [<Fact>]
    member _.NotFoundsAreReportedButCountZero() =
        use temp = new TempDirectory()
        writeText temp.Path "alpha.cs" "public class A\n{\n    public int M()\n    {\n        return 1;\n    }\n}\n" |> ignore

        let lines = reportFor [ "L", "alpha.cs", [ "M"; "Missing" ] ] temp.Path
        Assert.Contains("M=4", lines.[0])
        Assert.Contains("Missing=NOT_FOUND", lines.[0])
        Assert.Contains("raw=  4", lines.[0])
        Assert.Equal("TOTAL raw lines counted: 4", List.last lines)

    [<Fact>]
    member _.TotalIsSumOfFoundMethodCounts() =
        use temp = new TempDirectory()

        writeText
            temp.Path
            "alpha.cs"
            "public class A\n{\n    public int M1()\n    {\n        return 1;\n    }\n\n    public int M2()\n    {\n        return 2;\n    }\n}\n"
        |> ignore

        let lines = reportFor [ "L", "alpha.cs", [ "M1"; "M2" ] ] temp.Path
        Assert.Contains("M1=4", lines.[0])
        Assert.Contains("M2=4", lines.[0])
        Assert.Contains("raw=  8", lines.[0])
        Assert.Equal("TOTAL raw lines counted: 8", List.last lines)

    [<Fact>]
    member _.TargetsRunFromCallSitesCodeDirectory() =
        let dir = callSitesCodeDir ()
        Assert.True(Directory.Exists dir)
        Assert.EndsWith(Path.Combine("docs", "next-stage", "call-sites-code"), dir)
