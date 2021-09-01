using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.AFASRegion.Model;

namespace ThMEPElectrical.AFASRegion.Expand
{
    public static class DetectorExtend
    {
        public static int RegionLevel(this AFASDetector detector,double area)
        {
            switch (detector)
            {
                case AFASDetector.TemperatureDetectorLow:
                    {
                        if (area > 12E+6)
                            return 1;
                        else if (area > 8E+6)
                            return 2;
                        else if (area > 6E+6)
                            return 3;
                        else if (area > 4E+6)
                            return 4;
                        else
                            return 5;
                    }
                case AFASDetector.TemperatureDetectorHigh:
                    {
                        if (area > 18E+6)
                            return 1;
                        else if (area > 12E+6)
                            return 2;
                        else if (area > 9E+6)
                            return 3;
                        else if (area > 6E+6)
                            return 4;
                        else
                            return 5;
                    }
                case AFASDetector.SmokeDetectorLow:
                    {
                        if (area > 36E+6)
                            return 1;
                        else if (area > 24E+6)
                            return 2;
                        else if (area > 18E+6)
                            return 3;
                        else if (area > 12E+6)
                            return 4;
                        else
                            return 5;
                    }
                case AFASDetector.SmokeDetectorHigh:
                    {
                        if (area > 48E+6)
                            return 1;
                        else if (area > 32E+6)
                            return 2;
                        else if (area > 24E+6)
                            return 3;
                        else if (area > 16E+6)
                            return 4;
                        else
                            return 5;
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
