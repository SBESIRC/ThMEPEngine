using Catel.IoC;
using Catel.Core;
using AcHelper;
using ThMEPEngineCore.Progress;
using System.Threading;

namespace TianHua.Plumbing.UI
{
    [ServiceLocatorRegistration(typeof(IProgressHandler))]
    public class PlumbingProgressHandler : IProgressHandler
    {
        public void Progress(string key, int percent, string message)
        {
            if (percent == 0)
            {
                Active.Editor.WriteMessage("开始");
            }
            else if (percent == 100)
            {
                Active.Editor.WriteMessage("结束");
            }
        }
    }
}
