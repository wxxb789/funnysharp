// Live successor; historical packets and verifier sources remain immutable.
#load "CurrentPacket.fs"
open System.IO
open CurrentPacket

let arguments = fsi.CommandLineArgs |> Array.skip 1
if arguments.Length > 1 then failwith "Usage: dotnet fsi verify-current-performance.fsx -- [repository-root]"
let root = if arguments.Length = 1 then Path.GetFullPath arguments[0] else Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let packet = new Packet(root, Current)
verify packet
(packet :> System.IDisposable).Dispose()
