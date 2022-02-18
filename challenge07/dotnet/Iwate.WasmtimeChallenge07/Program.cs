using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Wasmtime;

var dir = AppDomain.CurrentDomain.BaseDirectory;
var script =
@"
import * as host from 'host';
import * as std from 'std';
import ejs from './ejs.mjs';
ejs.fileLoader = function(filename) {
    const file = std.open(filename,'r');
    return file.readAsString();
};

const handlers = {};
function register(methodName, handler) {
    handlers[methodName] = handler;
}
function listen() {
    while(true) {
        const line = host.readLine();
        
        if (line == '.quit') {
            break;
        }

        const data = JSON.parse(line);
        const func = handlers[data.method];
        if (typeof func == 'function' && Array.isArray(data.params)) {
            func.apply(null, data.params.concat([function (result) {
                host.writeLine(JSON.stringify({
                    jsonrpc: '2.0',
                    result,
                    id: data.id,
                }));
            }]));
        }
    }
}
register('transform', function (payload, callback) {
    ejs.renderFile('template.ejs', payload, {}, function (err, str) {
        if (err) {
            std.err.puts(err);
        }
        else {
            const transformed = {
                name: payload.name,
                html: str
            };
            callback(transformed);
        }
    })
});

listen();
";
File.WriteAllText(Path.Combine(dir, "main.js"), script);

var rpccall = JsonSerializer.Serialize(new
{
    jsonrpc = "2.0",
    method = "transform",
    @params = new[] { new { name = "iwate"} },
    id = Guid.NewGuid().ToString()
});

using var rbuf = new MemoryStream(Encoding.UTF8.GetBytes($"{rpccall}\n.quit\n"));
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