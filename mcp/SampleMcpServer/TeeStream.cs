using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleMcpServer
{
    public class TeeStream : TextWriter
    {
        private readonly TextWriter _primary;
        private readonly TextWriter _log;

        public TeeStream(TextWriter primary, TextWriter log)
        {
            _primary = primary;
            _log = log;
        }

        public override Encoding Encoding => _primary.Encoding;

        public override void Write(char value)
        {
            _primary.Write(value);
            _log.Write(value);
        }

        public override void Flush()
        {
            _primary.Flush();
            _log.Flush();
        }
    }
}
