#load "../harness/Output.fs"
#load "../harness/Proc.fs"
#load "../harness/FrozenReplay.fs"

open System.IO

let root = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", ".."))
System.Environment.Exit(FunnySharp.Harness.FrozenReplay.main root (fsi.CommandLineArgs |> Array.skip 1))
