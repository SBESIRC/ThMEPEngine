using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Progress
{
    public interface IProgressHandler
    {
        void Progress(string key, int percent, string message);
    }
}
