using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPLighting.IlluminationLighting.Model
{
    class ThAFASIlluminationLayoutParameter
    {
        public Point3dCollection framePts;
        public ThMEPOriginTransformer transformer;
        public double Scale { get; set; } = 100;
        public double AisleAreaThreshold { get; set; } =0.75;

        public string BlkNameN = "";
        public string BlkNameE = "";
        public double radiusN = 3000;
        public double radiusE = 6000;
        public bool ifLayoutEmg = true;
        public double priorityExtend = 0;
        public List<Point3d> stairPartResult { get; set; } = new List<Point3d>();
        public List<ThGeometry> DoorOpenings { get; set; } = new List<ThGeometry>();
        public List<ThGeometry> Windows { get; set; } = new List<ThGeometry>();

        public Dictionary<Polyline, ThIlluminationCommon.LayoutType> roomType { get; set; } = new Dictionary<Polyline, ThIlluminationCommon.LayoutType>();
    }
}
