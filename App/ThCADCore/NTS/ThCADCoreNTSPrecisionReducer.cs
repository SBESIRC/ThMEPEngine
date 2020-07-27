using System;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSPrecisionReducer : IDisposable
    {
        public ThCADCoreNTSPrecisionReducer(double scale = 0.0)
        {
            ThCADCoreNTSService.Instance.Scale = scale;
            ThCADCoreNTSService.Instance.PrecisionReduce = true;
        }

        public void Dispose()
        {
            ThCADCoreNTSService.Instance.PrecisionReduce = false;
        }
    }
}
