using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

using ThMEPElectrical.FireAlarmDistance.Data;

namespace ThMEPElectrical.FireAlarmDistance.Model
{
    public class ThAFASBCLayoutParameter
    {
        public Point3dCollection framePts;

        public double Scale { get; set; } = 100;

        public string BlkNameBroadcast = "";

        public List<Point3d> StairPartResult { get; set; } = new List<Point3d>();
        public ThAFASDistanceDataQueryService Data { get; set; }
    }
}
