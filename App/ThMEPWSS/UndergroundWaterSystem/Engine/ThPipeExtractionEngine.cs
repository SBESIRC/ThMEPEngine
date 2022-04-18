using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundWaterSystem.Command;

namespace ThMEPWSS.UndergroundWaterSystem.Engine
{
    /// <summary>
    /// 横管提取引擎
    /// </summary>
    public class ThPipeExtractionEngine
    {
        public List<Line> GetWaterPipeLines()
        {
            using (var database = AcadDatabase.Active())
            {
                var retLines = new List<Line>();
                var entities = database.ModelSpace.OfType<Entity>();
                foreach (var ent in entities)
                {
                    if (IsLayer(ent.Layer) && ThUndergroundWaterSystemUtils.IsTianZhengElement(ent))
                    {
                        retLines.Add(TianZhengLine(ent));
                    }
                }
                return retLines;
            }
        }
        public List<Line> GetPipeLines(Point3dCollection pts)
        {
            using (var database = AcadDatabase.Active())
            {
                var retLines = new List<Line>();
                var entities = database.ModelSpace.OfType<Entity>();
                DBObjectCollection dbObjs = null;
                if (pts.Count > 0)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(entities.ToCollection());
                    var pline = new Polyline()
                    {
                        Closed = true,
                    };
                    pline.CreatePolyline(pts);
                    dbObjs = spatialIndex.SelectCrossingPolygon(pline);
                }
                else
                {
                    dbObjs = entities.ToCollection();
                }
                foreach (var obj in dbObjs)
                {
                    var ent = obj as Entity;
                    if (IsLayer(ent.Layer) && ThUndergroundWaterSystemUtils.IsTianZhengElement(ent))
                    {
                        var line = TianZhengLine(ent);
                        line.Layer = ent.Layer;
                        if (line.Length > 1.0)
                        {
                            retLines.Add(line);
                        }
                    }
                }
                return retLines;
            }
        }
        public bool IsLayer(string layer)
        {
            if ((layer.ToUpper().Contains("W-WSUP") && layer.ToUpper().Contains("COOL-PIPE")) || layer.ToUpper().Contains("PIPE-给水"))
            {
                return true;
            }
            return false;
        }
        public Line TianZhengLine(Entity ent)
        {
            var retLine = new Line();
            //天正元素处理
            var pt1 = ent.GetType().GetProperty("StartPoint");
            var pt2 = ent.GetType().GetProperty("EndPoint");
            if (pt1 != null && pt2 != null)
            {
                retLine.StartPoint = (Point3d)pt1.GetValue(ent);
                retLine.EndPoint = (Point3d)pt2.GetValue(ent);
            }
            else
            {
                List<Point3d> pts = new List<Point3d>();
                foreach (Entity l in ent.ExplodeToDBObjectCollection())
                {
                    if (l is Polyline)
                    {
                        pts.Add((l as Polyline).StartPoint);
                        pts.Add((l as Polyline).EndPoint);
                    }
                    else if (l is Line)
                    {
                        pts.Add((l as Line).StartPoint);
                        pts.Add((l as Line).EndPoint);
                    }
                }
                var pairPt = pts.GetCollinearMaxPts();
                retLine.StartPoint = pairPt.Item1;
                retLine.EndPoint = pairPt.Item2;
            }
            retLine.StartPoint = new Point3d(retLine.StartPoint.X, retLine.StartPoint.Y, 0.0);
            retLine.EndPoint = new Point3d(retLine.EndPoint.X, retLine.EndPoint.Y, 0.0);
            return retLine;
        }
    }
}
