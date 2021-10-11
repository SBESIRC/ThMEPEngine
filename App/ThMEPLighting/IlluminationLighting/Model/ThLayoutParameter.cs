using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.IlluminationLighting.Model
{
    class ThLayoutParameter
    {
        public Point3dCollection framePts;
        public ThMEPOriginTransformer transformer;
        public double Scale { get; set; } = 100;
        public double AisleAreaThreshold { get; set; } = 0.025;

        public string BlkNameN = "";
        public string BlkNameE = "";
        public double radiusN = 3000;
        public double radiusE = 6000;
        public bool ifLayoutEmg = true;
        public double priorityExtend = 0;
        public List<Point3d> stairPartResult { get; set; } = new List<Point3d>();
        public Dictionary<Polyline, ThIlluminationCommon.layoutType> roomType { get; set; } = new Dictionary<Polyline, ThIlluminationCommon.layoutType>();
    }
}
