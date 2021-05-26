using System;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSArcTessellationLength : IDisposable
    {
        private double Length { get; set; }

        public ThCADCoreNTSArcTessellationLength(double length)
        {
            Length = ThCADCoreNTSService.Instance.ArcTessellationLength;
            ThCADCoreNTSService.Instance.ArcTessellationLength = length;
        }

        public void Dispose()
        {
            ThCADCoreNTSService.Instance.ArcTessellationLength = Length;
        }
    }
}
