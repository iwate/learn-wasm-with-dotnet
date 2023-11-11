using Wasmtime;

var wasmfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.wasm");
var wasi = new WasiConfiguration()
    .WithInheritedArgs()
    .WithInheritedStandardInput()
    .WithInheritedStandardOutput()
    .WithInheritedStandardError();

using var engine = new Engine();
using var module = Module.FromFile(engine, wasmfile);
using var linker = new Linker(engine);
using var store = new Store(engine);

linker.DefineWasi();
store.SetWasiConfiguration(wasi);

var instance = linker.Instantiate(store, module);

var run = instance.GetFunction("_start");
if (run == null)
{
    Console.WriteLine("error: _start export is missing");
    return;
}

run?.Invoke();