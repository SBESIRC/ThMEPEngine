using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.UndergroundWaterSystem.Model;
using ThMEPWSS.UndergroundWaterSystem.Tree;
using static ThMEPWSS.UndergroundWaterSystem.Utilities.GeoUtils;

namespace ThMEPWSS.UndergroundWaterSystem.Service
{
    public class PreLine
    {
        public PreLine(Line line, string layer, int lineType = -1)
        {
            Line = line;
            Layer = layer;
            LineType = lineType;
        }
        public Line Line;
        public int LineType = -1;//0横管，1立管
        public string Layer = "0";
        public List<Polyline> CorrespondingValveRec = new List<Polyline>();
    }
    public class CrossedLayerDims
    {
        public CrossedLayerDims(string text, Point3d point)
        {
            Text = text;
            Point = point;
        }
        public string Text;
        public Point3d Point;
    }
    public class Dim
    {
        public Dim(Point3d point, string text, Point3d iniPoint)
        {
            Point = point;
            Text = text;
            IniPoint = iniPoint;
        }
        public Point3d Point;
        public Point3d IniPoint;
        public string Text;
    }
}
