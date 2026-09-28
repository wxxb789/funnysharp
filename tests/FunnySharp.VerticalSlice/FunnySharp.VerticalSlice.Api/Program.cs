using FunnySharp.VerticalSlice;

var app = VerticalSliceApp.Build(args);
if (VerticalSliceApp.IsVerifyRun(args))
{
    Console.WriteLine(VerticalSliceApp.VerifyMarker);
    return 0;
}

await app.RunAsync();
return 0;
