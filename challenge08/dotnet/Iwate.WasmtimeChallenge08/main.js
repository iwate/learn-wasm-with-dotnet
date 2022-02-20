import * as host from 'host';
import * as os from 'os';
import * as std from 'std';
import ejs from './ejs.mjs';
ejs.fileLoader = function (filename) {
  const file = std.open(filename, 'r');
  return file.readAsString();
};

let id = 0;
const handlers = {};
const resolves = {};
const rejects = {};
function register(methodName, handler) {
  handlers[methodName] = handler;
}
function invoke(method) {
  const params = Array.from(arguments).slice(1);
  return new Promise(function (resolve, reject) {
    const rpccall = {
      jsonrpc: '2.0',
      method,
      params,
      id: ++id,
    };
    resolves[rpccall.id] = resolve;
    rejects[rpccall.id] = reject;
    host.writeLine(JSON.stringify(rpccall));
  })
}
function unitOfWork() {
  const line = host.readLine();
  if (line == null || line=='') {
    return true;
  }
  if (line == '.quit') {
    return false;
  }

  let data = JSON.parse(line);

  if (data.method) {
    const func = handlers[data.method];
    if (typeof func == 'function' && Array.isArray(data.params)) {
      func.apply(null, data.params.concat([function (result) {
        host.writeLine(JSON.stringify({
          jsonrpc: '2.0',
          result,
          id: this.id,
        }));
      }.bind(data)]));
    }
  }
  else if (data.result) {
    std.err.puts("resolving\n");
    std.err.puts(resolves[data.id] + "\n");
    resolves[data.id](data.result);

  }
  else if (data.error) {
    rejects[data.id](data.error);
    delete resolves[data.id];
    delete rejects[data.id];
  }
  return true;
}
function listen() {
  const loop = () => {
    if (unitOfWork()) {
      os.setTimeout(function () {
        loop();
      }, 0);
    }
  }

  loop();
}
register('transform', function (payload, callback) {
  invoke('HostMethod1', 1, 1.0, 'hello', {property:'propvalue'}).then(function (value) {
    ejs.renderFile('template.ejs', payload, {}, function (err, str) {
      if (err) {
        std.err.puts(err);
      }
      else {
        const transformed = {
          name: payload.name,
          html: str,
          value,
        };
        callback(transformed);
      }
    });
  });
});

listen();

