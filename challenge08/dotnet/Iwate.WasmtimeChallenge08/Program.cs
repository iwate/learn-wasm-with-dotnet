using Newtonsoft.Json.Linq;
using System;
using System.Threading;

var dir = AppDomain.CurrentDomain.BaseDirectory;
var rpc = new QjsRpc(dir);
var host = new Host();

rpc.Start(host);

var transformed = await rpc.InvokeAsync<JObject>("transform", new[] { new { name = "iwate" } }, CancellationToken.None);

Console.WriteLine(transformed);

await rpc.CloseWaitAsync();