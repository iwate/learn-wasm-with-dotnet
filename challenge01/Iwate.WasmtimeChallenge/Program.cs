using System;
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;
using Wasmtime;

namespace Iwate.WasmtimeChallenge
{
    class Program
    {
        static void Main(string[] args)
        {
            var bytes = new byte[32];
            using (var stream = new MemoryStream(bytes))
            {
                new Hello { Message = "Hello, " }.WriteDelimitedTo(stream);
                stream.Flush();
            }
            

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "memory.wat");
            using var host = new Host(engine);
            using var mem = host.DefineMemory("", "mem");

            for (var i = 0; i < bytes.Length; i++)
            {
                mem.WriteByte(100 + i, bytes[i]);
            }

            using dynamic instance = host.Instantiate(module);
            instance.run();
            
            var result = mem.Span.Slice(100, 32).ToArray();
            using var stream1 = new MemoryStream(result);
            var hello = Hello.Parser.ParseDelimitedFrom(stream1);
            Console.WriteLine(hello.Message);
            Console.WriteLine(Helpers.HexDump(result));
        }
    }
}
