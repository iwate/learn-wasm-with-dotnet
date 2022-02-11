using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Wasmtime;

namespace Iwate.Challenge05
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptArg = "{\"id\":100,\"name\":\"iwate\"}";
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
