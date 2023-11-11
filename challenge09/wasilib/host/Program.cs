using System.Text;
using Wasmtime;

var wasmfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib.wasm");
var wasi = new WasiConfiguration();

using var engine = new Engine();
using var module = Module.FromFile(engine, wasmfile);
using var linker = new Linker(engine);
using var store = new Store(engine);

linker.DefineWasi();
store.SetWasiConfiguration(wasi);

var instance = linker.Instantiate(store, module);

var init = instance.GetAction("_initialize");
if (init is null)
{
    Console.WriteLine("error: MyAdd export is missing");
    return;
}

init();

var add = instance.GetFunction<int, int, int>("MyAdd");
if (add is null)
{
    Console.WriteLine("error: MyAdd export is missing");
    return;
}

Console.WriteLine(add(1,2));


var helloworld = instance.GetFunction<int>("HelloWorld");
if (helloworld is null)
{
    Console.WriteLine("error: HelloWorld export is missing");
    return;
}

var ptr = helloworld();
var mem = instance.GetMemory("memory");
var str = Encoding.ASCII.GetString(mem!.GetSpan(ptr, 13));

Console.WriteLine(str);