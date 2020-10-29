using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Pipe
{
    public abstract class ThWPipeEngine : IDisposable
    {
        public void Dispose()
        {
            //
        }

        public abstract void Run();
    }
}
