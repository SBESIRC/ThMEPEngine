using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.FireAlarmArea.Model
{
    public class ThAFASSmokeLayoutParameter
    {
        public Point3dCollection framePts;
        public ThMEPOriginTransformer transformer;
        public int FloorHightIdx { get; set; } = -1;
        public int RootThetaIdx { get; set; } = -1;
        public double Scale { get; set; } = 100;
        public double AisleAreaThreshold { get; set; } = 0.75;

        public string BlkNameHeat = "";
        public string BlkNameSmoke = "";
        public string BlkNameHeatPrf = "";
        public string BlkNameSmokePrf = "";
        public double priorityExtend = 0;
        public List<Point3d> StairPartResult { get; set; } = new List<Point3d>();
        public List<ThGeometry> DoorOpenings { get; set; } = new List<ThGeometry>();
        public List<ThGeometry> Windows { get; set; } = new List<ThGeometry>();
        public Dictionary<Polyline, ThFaSmokeCommon.layoutType> RoomType { get; set; } = new Dictionary<Polyline, ThFaSmokeCommon.layoutType>();
    }
}
