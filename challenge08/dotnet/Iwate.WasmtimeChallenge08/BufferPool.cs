using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Iwate.WasmtimeChallenge08
{
    public class BufferPool : IDisposable
    {
        private readonly ConcurrentQueue<MemoryStream> _usings = new ConcurrentQueue<MemoryStream>();
        private readonly ConcurrentQueue<MemoryStream> _empties = new ConcurrentQueue<MemoryStream>();

        public MemoryStream NewWriteSream() =>
            _empties.Count > 0 && _empties.TryDequeue(out var stream) ? stream : new MemoryStream();

        public void WriteEnd(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            _usings.Enqueue(stream);
        }

        public MemoryStream NewReadStream()
        {
            while (_usings.Count > 0)
            {
                if (_usings.TryDequeue(out var stream))
                    return stream;
            }

            return null;
        }

        public void ReadEnd(MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(0);
            _empties.Enqueue(stream);
        }
        
        public void Dispose()
        {
            while (_usings.Count > 0)
            {
                if (_usings.TryDequeue(out var stream)) 
                    stream.Dispose();
            }
            while (_empties.Count > 0)
            {
                if (_empties.TryDequeue(out var stream))
                    stream.Dispose();
            }
        }
    }
}
