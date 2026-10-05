const spec=await Bun.file(process.argv[2]).json();
const startedUtc=new Date().toISOString();
const proc=Bun.spawn(spec.argv,{cwd:spec.cwd,env:{...process.env,...spec.env},stdout:Bun.file(spec.stdout),stderr:Bun.file(spec.stderr)});
let timedOut=false;
const timer=setTimeout(()=>{timedOut=true;proc.kill();},spec.timeoutMs);
const exitCode=await proc.exited;
clearTimeout(timer);
console.log('U12_CAPTURE_EXIT '+JSON.stringify({schema:'funnysharp-process-capture/v1',startedUtc,finishedUtc:new Date().toISOString(),exitCode,timedOut,signal:proc.signalCode??null,argv:spec.argv,cwd:spec.cwd,environment:spec.env,stdout:spec.stdout,stderr:spec.stderr}));
process.exit(exitCode===0?0:1);
