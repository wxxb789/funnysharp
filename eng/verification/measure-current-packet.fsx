#load "CurrentPacket.fs"
open System
open System.IO
open System.Text.Json
open System.Diagnostics
open CurrentPacket

let args = fsi.CommandLineArgs |> Array.skip 1
require (args.Length = 1) "Expected repository root"
let allocationStart = GC.GetTotalAllocatedBytes(true)
let workerProcess = Process.GetCurrentProcess()
let cpuStart = workerProcess.TotalProcessorTime
let watch = Stopwatch.StartNew()
let packet = new Packet(Path.GetFullPath args[0], TraversalR9)
verify packet
watch.Stop()
printfn "NEW_COST %s" (JsonSerializer.Serialize({| stats = packet.Stats; verifierElapsedMs = watch.Elapsed.TotalMilliseconds; cpuMs = (workerProcess.TotalProcessorTime - cpuStart).TotalMilliseconds; peakWorkingSetBytes = workerProcess.PeakWorkingSet64; allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocationStart |}))
(packet :> IDisposable).Dispose()

