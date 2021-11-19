using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Command
{
    public class ThFanConnectUtils
    {
        public static Point3dCollection SelectArea()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return new Point3dCollection();
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());
                return frame.Vertices();
            }
        }

        public static Point3d SelectPoint()
        {
            var point1 = Active.Editor.GetPoint("\n请选择水管起点位置");
            if (point1.Status != PromptStatus.OK)
            {
                return new Point3d();
            }
            return point1.Value.TransformBy(Active.Editor.UCS2WCS());
        }
        public static List<ThFanCUModel> SelectFanCUModel()
        {
            var retModeles = new List<ThFanCUModel>();
            while (true)
            {
                using (var acadDb = Linq2Acad.AcadDatabase.Active())
                {
                    var insertPtRst = Active.Editor.GetEntity("选择一个风机\n");
                    if (insertPtRst.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                        break;
                    if (insertPtRst.ObjectId.IsValid)
                    {
                        var entity = acadDb.Element<Entity>(insertPtRst.ObjectId);
                        if (entity is BlockReference)
                        {
                            var tmpFan = new ThFanCUModel();
                            var blk = entity as BlockReference;
                            var offset1x = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 X"));
                            var offset1y = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 Y"));

                            var offset1 = new Point3d(offset1x, offset1y, 0);
                            var dbcollection = new DBObjectCollection();
                            blk.Explode(dbcollection);
                            dbcollection = dbcollection.OfType<Entity>().Where(O => O is Curve).ToCollection();

                            tmpFan.FanPoint = offset1.TransformBy(blk.BlockTransform);
                            tmpFan.FanObb = dbcollection.GetMinimumRectangle();
                            retModeles.Add(tmpFan);
                        }
                    }
                }
            }
            return retModeles;
        }
        public static List<Line> GetNearbyLine(Point3d pt, List<Line> lines, int N = 3)
        {
            List<Line> returnLines = new List<Line>();
            if (lines.Count <= N)
            {
                return lines;
            }

            lines = lines.OrderBy(o => DistanceToPoint(o,pt)).ToList();
            for (int i = 0; i < N; i++)
            {
                returnLines.Add(lines[i]);
            }
            return returnLines;
        }

        public static void AnalysisPipe(Point3d startPt, List<Line> lines, List<Line> trunkLines, List<Line> branchLines)
        {
            //找到起始线，然后，从起始线开始遍历查询，找到一条首尾相接的线段，该线段为trunkLines
            double tol = 10.0;
            foreach(var l in lines)
            {
                if(l.StartPoint.DistanceTo(startPt) < tol)
                {
                    lines.Remove(l);
                    trunkLines.Add(l);
                    break;
                }
                else if(l.EndPoint.DistanceTo(startPt) < tol)
                {
                    var tmpPoint = l.StartPoint;
                    l.StartPoint = l.EndPoint;
                    l.EndPoint = tmpPoint;
                    lines.Remove(l);
                    trunkLines.Add(l);
                    break;
                }
            }
            if(trunkLines.Count == 0)
            {
                return;
            }
        }

        public static double DistanceToPoint(Line l ,Point3d pt,bool isExtend = false)
        {
            Point3d closestPoint = l.GetClosestPointTo(pt, isExtend);
            return pt.DistanceTo(closestPoint);
        }
        public static Polyline CreateMapFrame(Line line,Point3d pt, double expandLength)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(pt);
            pts.Add(line.GetClosestPointTo(pt, false));

            Polyline polyLine = new Polyline();
            polyLine.AddVertexAt(0, pts[0].ToPoint2D(), 0, 0, 0);
            polyLine.AddVertexAt(0, pts[1].ToPoint2D(), 0, 0, 0);
            var objcet = polyLine.BufferPL(expandLength)[0];
            return objcet as Polyline;
        }
        /// <summary>
        /// 判断是否和外框线相交
        /// </summary>
        /// <param name="line"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static bool CheckIntersectWithFrame(Curve line, Polyline frame)
        {
            return frame.IsIntersects(line);
        }
        /// <summary>
        /// 用nts的selectCrossing计算是否相交
        /// </summary>
        /// <returns></returns>
        public static bool LineIntersctBySelect(List<Polyline> polylines, Polyline line, double bufferWidth)
        {
            DBObjectCollection dBObject = new DBObjectCollection() { line };
            foreach (Polyline polyline in dBObject.Buffer(bufferWidth))
            {
                if (SelelctCrossing(polylines, polyline).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
        public static bool LineIntersctBySelect(List<Line> lines, Polyline pl)
        {
            foreach (var l in lines)
            {
                if (l.IsIntersects(pl))
                {
                    return true;
                }
            }
            return false;
        }
        public static List<Polyline> SelelctCrossing(List<Polyline> polylines, Polyline polyline)
        {
            var objs = polylines.ToCollection();
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var resHoles = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            return resHoles;
        }
    }
}
