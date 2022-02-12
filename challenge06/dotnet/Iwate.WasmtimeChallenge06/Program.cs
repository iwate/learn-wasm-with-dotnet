using System;
using System.IO;
using System.Text;
using Wasmtime;

var dir = AppDomain.CurrentDomain.BaseDirectory;
var script =
@"
import * as host from 'host';
console.log('-- start js');
host.writeLine('Hello, I\'m js!');
const str = host.readLine();
console.log(str);
console.log('-- end js');

";
File.WriteAllText(Path.Combine(dir, "main.js"), script);

using var rbuf = new MemoryStream(Encoding.UTF8.GetBytes("Hello, I'm dotnet!\n"));
using var wbuf = new MemoryStream();

using var engine = new Engine();
using var module = Module.FromFile(engine, "qjs.wasm");
using var linker = new Linker(engine);
using var store = new Store(engine);

linker.DefineFunction("env", "read", () => {
    return (int)rbuf.ReadByte();
});

linker.DefineFunction("env", "write", (int c) => {
    wbuf.WriteByte((byte)c);
});

linker.DefineWasi();

store.SetWasiConfiguration(new WasiConfiguration()
    .WithPreopenedDirectory(dir, ".")
    .WithInheritedStandardOutput()
    .WithArgs("--", "main.js"));

var instance = linker.Instantiate(store, module);

var run = instance.GetFunction(store, "_start");

run.Invoke(store);

wbuf.Seek(0, SeekOrigin.Begin);
var str = Encoding.UTF8.GetString(wbuf.ToArray());
Console.WriteLine(str);