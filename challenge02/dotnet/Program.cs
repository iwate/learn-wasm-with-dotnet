using System;
using Wasmtime;

using var engine = new Engine();
using var module = Module.FromFile(engine, "challenge02.wasm");
using var host = new Host(engine);

using dynamic instance = host.Instantiate(module);
var result = instance.add(1, 2);

Console.WriteLine(result);