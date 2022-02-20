using Iwate.WasmtimeChallenge08;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wasmtime;
using Module = Wasmtime.Module;

class QjsRpc : IDisposable
{
    private readonly string _dir;
    private BufferPool _pool;
    private ConcurrentDictionary<string, JObject> _results;
    private ConcurrentQueue<JObject> _invokes;
    private IReadOnlyDictionary<string, MethodInfo> _methods;
    private object _methodHost;
    public QjsRpc(string dir)
    {
        _dir = Directory.Exists(dir) ? dir : throw new ArgumentException($"{dir} does not exists.");
        _pool = new BufferPool();
        _results = new ConcurrentDictionary<string, JObject>();
        _invokes = new ConcurrentQueue<JObject>();
    }

    public void Dispose()
    {
        if (_pool != null)
        {
            _pool.Dispose();
            _pool = null;
        }
    }

    private void WriteLine(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        var stream = _pool.NewWriteSream();
        stream.Write(bytes);
        stream.WriteByte((byte)'\n');
        _pool.WriteEnd(stream);
    }
    public Task<TResult> InvokeAsync<TResult>(string method, object[] @params, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var id = Guid.NewGuid().ToString();

            var rpccall = new { jsonrpc = "2.0", method, @params, id };

            WriteLine(JsonConvert.SerializeObject(rpccall));

            while(!cancellationToken.IsCancellationRequested)
            {
                if (_results.TryRemove(id, out var jobj))
                {
                    return jobj["result"].ToObject<TResult>();
                }
            }
            throw new TaskCanceledException();
        }, cancellationToken);
    }

    private Task _task;
    public void Start<T>(T host)
    {
        if (_task != null)
            throw new InvalidOperationException("This instance is aleary running.");

        _methodHost = host;
        _methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.Instance).ToDictionary(m => m.Name, m => m);

        var source = new CancellationTokenSource();

        _task = Task.WhenAny(
            Task.Run(() =>
            {
                QjsRun();
                source.Cancel();
            }),
            Task.Run(() =>
            {
                MethodRun(source.Token);
            })
        );
    }
    void QjsRun()
    {
        using var engine = new Engine();
        using var module = Module.FromFile(engine, "qjs.wasm");
        using var linker = new Linker(engine);
        using var store = new Store(engine);
        MemoryStream wbuf = new MemoryStream();
        linker.DefineFunction("env", "write", (int c) => {
            if (c == '\n')
            {
                var len = wbuf.Position;

                if (len > int.MaxValue)
                    throw new NotSupportedException("The line is too long. It needs to be less than or equal to int.MaxValue.");

                var str = Encoding.UTF8.GetString(wbuf.GetBuffer(), 0, (int)len);

                var jobj = JObject.Parse(str);

                if (jobj["result"] != null || jobj["error"] != null)
                {
                    _results.TryAdd((string)jobj["id"], jobj);
                }
                else if (jobj["method"]!= null)
                {
                    _invokes.Enqueue(jobj);
                }

                wbuf.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                wbuf.WriteByte((byte)c);
            }
        });

        MemoryStream rbuf = null;
        linker.DefineFunction("env", "read", () => {
            if (rbuf == null)
            {
                while ((rbuf = _pool.NewReadStream()) == null) ;
            }
            else if (rbuf.Position >= rbuf.Length)
            {
                _pool.ReadEnd(rbuf);
                while ((rbuf = _pool.NewReadStream()) == null) ;
            }

            return rbuf.ReadByte();
        });

        linker.DefineWasi();

        store.SetWasiConfiguration(new WasiConfiguration()
            .WithPreopenedDirectory(_dir, ".")
            .WithInheritedStandardOutput()
            .WithArgs("--", "main.js"));

        var instance = linker.Instantiate(store, module);

        var run = instance.GetFunction(store, "_start");

        run.Invoke(store);

        wbuf?.Dispose();
        rbuf?.Dispose();
    }

    void MethodRun(CancellationToken cancellationToken)
    {
        var taskType = typeof(Task);
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_invokes.TryDequeue(out var jobj))
            {
                var methodName = (string)jobj["method"];
                if (_methods.ContainsKey(methodName))
                {
                    var method = _methods[methodName];
                    var types = method.GetParameters();
                    var @params = new List<object>();
                    foreach (var (token, info) in jobj["params"].Zip(types))
                    {
                        @params.Add(token.ToObject(info.ParameterType));
                    }
                    try
                    {
                        var result = method.Invoke(_methodHost, @params.ToArray());
                        if (method.ReturnType.BaseType == taskType)
                        {
                            ((Task)result).ContinueWith(task =>
                            {
                                if (task.IsCompletedSuccessfully)
                                {
                                    WriteLine(JsonConvert.SerializeObject(new
                                    {
                                        jsonrpc = "2.0",
                                        result = task.GetType().GetProperty("Result").GetValue(result),
                                        id = (string)jobj["id"]
                                    }));
                                }
                                else
                                {
                                    WriteLine(JsonConvert.SerializeObject(new
                                    {
                                        jsonrpc = "2.0",
                                        error = new { code = -32603, message = task.Exception?.Message },
                                        id = (string)jobj["id"]
                                    }));
                                }
                            }, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            WriteLine(JsonConvert.SerializeObject(new
                            {
                                jsonrpc = "2.0",
                                result,
                                id = (string)jobj["id"]
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteLine(JsonConvert.SerializeObject(new
                        {
                            jsonrpc = "2.0",
                            error = new { code = -32603, message = ex.Message },
                            id = (string)jobj["id"]
                        }));
                    }
                }
            }
        }
    }

    public void Close()
    {
        WriteLine(".quit");
    }

    public void Wait()
    {
        _task.Wait();
    }

    public Task WaitAsync()
    {
        return _task;
    }

    public void CloseWait()
    {
        Close();
        Wait();
    }

    public Task CloseWaitAsync()
    {
        Close();
        return WaitAsync();
    }
}