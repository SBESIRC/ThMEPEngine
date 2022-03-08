using System;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSFixedPrecision : IDisposable
    {
        private bool PrecisionReduce { get; set; }

        public ThCADCoreNTSFixedPrecision(bool precisionReduce = true)
        {
            PrecisionReduce = ThCADCoreNTSService.Instance.PrecisionReduce;
            ThCADCoreNTSService.Instance.PrecisionReduce = precisionReduce;
        }

        public void Dispose()
        {
            ThCADCoreNTSService.Instance.PrecisionReduce = PrecisionReduce;
        }
    }
}
