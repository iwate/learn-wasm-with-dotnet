using System;
using System.IO;
using Wasmtime;

var dir = AppDomain.CurrentDomain.BaseDirectory;
var script =
@"
import * as host from 'host';
host.hello();
";
File.WriteAllText(Path.Combine(dir, "main.js"), script);

using var engine = new Engine();
using var module = Module.FromFile(engine, "qjs.wasm");
using var linker = new Linker(engine);
using var store = new Store(engine);

linker.DefineFunction("env", "hello", () => { Console.WriteLine("This is called from wasm."); });

linker.DefineWasi();

store.SetWasiConfiguration(new WasiConfiguration()
    .WithPreopenedDirectory(dir, ".")
    .WithArgs("--", "main.js"));

var instance = linker.Instantiate(store, module);

var run = instance.GetFunction(store, "_start");

run.Invoke(store);