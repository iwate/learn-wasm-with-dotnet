using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Wasmtime;

namespace Iwate.Challenge04
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptArg = "{\"id\":100,\"name\":\"iwate\"}";
            var script =
@"
import * as std from 'std';
import ejs from 'ejs.esm.js'

ejs.fileLoader = function(filename) {
    const file = std.open(filename,'r');
    return file.readAsString();
};

const original = JSON.parse(scriptArgs[1]);

ejs.renderFile('template.ejs', original, {}, function (err, str) {
    if (err) {
        std.err.puts(err);
    }
    else {
        const transformed = {
            id: original.id,
            html: str
        };

        console.log(JSON.stringify(transformed));
    }
})
";
            File.WriteAllText(Path.Combine(dir, "main.js"), script);

            using var engine = new Engine();
            using var module = Module.FromFile(engine, "qjs.wasm");
            using var linker = new Linker(engine);
            using var store = new Store(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(new WasiConfiguration()
                .WithEnvironmentVariable("WASMTIME_BACKTRACE_DETAILS", "1")
                .WithInheritedStandardInput()
                .WithInheritedStandardOutput()
                .WithInheritedStandardError()
                .WithPreopenedDirectory(dir, ".")
                .WithArgs("--", "-m", "--std", "main.js", scriptArg));

            var instance = linker.Instantiate(store, module);

            var run = instance.GetFunction(store, "_start");

            

            run.Invoke(store);
        }
    }
}
