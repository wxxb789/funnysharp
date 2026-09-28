using FunnySharp.VerticalSlice.Baseline;

var app = BaselineApp.Build(args);
if (BaselineApp.IsVerifyRun(args))
{
    Console.WriteLine(BaselineApp.VerifyMarker);
    return 0;
}

await app.RunAsync();
return 0;
