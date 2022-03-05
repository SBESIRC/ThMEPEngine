using System;
using System.Linq;
using ThMEPHVAC.IO;

namespace ThMEPHVAC.CAD
{
    public class ThPortParameter
    {
        public PortSizeParameter DuctSizeInfor { get; set; }
        public ThPortParameter(double airVolume)
        {
            DuctSizeInfor = GetCandidateDucts(airVolume);
        }
        private PortSizeParameter GetCandidateDucts(double airVolume)
        {
            double speed = 3.0;
            double area = Round2float(airVolume / 3600.0 / speed);
            var jsonReader = new ThPortParameterJsonReader();
            var sizeFloor = jsonReader.Parameters.Where(d => d.SectionArea >= area).OrderBy(d => d.SectionArea);
            if (sizeFloor.Count() == 0)
                return new PortSizeParameter() { DuctWidth = 500, DuctHeight = 200, SectionArea = 0.1, AspectRatio = 2.5};
            return sizeFloor.First(d => d.DuctHeight == sizeFloor.Min(f => f.DuctHeight));
        }
        private double Round2float(double f)
        {
            string s = f.ToString("#0.00");
            return Double.Parse(s);
        }
    }
}
