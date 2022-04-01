using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.General;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class LoopMarkPt//提取环管标记点
    {
        public DBObjectCollection DBObjs { get; set; }
        public LoopMarkPt()
        {
            DBObjs = new DBObjectCollection();
        }
        public void Extract(Database database, SprayIn sprayIn)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => IsTraget(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                foreach(var polygon in sprayIn.FloorRectDic.Values)
                {
                    var dbObjs = spatialIndex.SelectCrossingPolygon(polygon);
                    dbObjs.Cast<Entity>()
                        .ForEach(e => DBObjs.Add(e));
                }
            }
        }

        public bool Extract(Database database, Point3d insertPt)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => IsTraget(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                var dbObjs = spatialIndex.SelectCrossingPolygon(CreatePolyline(insertPt));
                if(dbObjs.Count > 0)
                {
                    return true;
                }
                return false;

            }
        }
        private static Polyline CreatePolyline(Point3d c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c.X - tolerance, c.Y - tolerance)); // low left
            pts.Add(new Point2d(c.X - tolerance, c.Y + tolerance)); // high left
            pts.Add(new Point2d(c.X + tolerance, c.Y + tolerance)); // high right
            pts.Add(new Point2d(c.X + tolerance, c.Y - tolerance)); // low right
            pts.Add(new Point2d(c.X - tolerance, c.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        private bool IsTraget(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("喷淋总管标记");
        }

        public void CreateStartPts(List<Line> pipeLine, SprayIn sprayIn, Point3d startPt)
        {
            var StartPoints = new List<List<Point3d>>();
            foreach (var db in DBObjs)
            {
                var pos = new List<Point3d>();
                var br = db as BlockReference;

                var offset1x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X"));
                var offset1y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y"));
                var offset2x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X"));
                var offset2y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y"));

                var offset1 = new Point3d(offset1x, offset1y, 0);
                var offset2 = new Point3d(offset2x, offset2y, 0);
                var pt1 = offset1.TransformBy(br.BlockTransform);
                var pt2 = offset2.TransformBy(br.BlockTransform);

                pos.Add(pt1);
                pos.Add(pt2);
                StartPoints.Add(pos);
            }
            foreach(var pts in StartPoints)
            {
                var pt1 = pts[0];
                var pt2 = pts[1];
                var sLine = pt1.GetClosestLine(pipeLine);
                if(sLine.Length < 1) continue;

                var eLine = pt2.GetClosestLine(pipeLine);
                if (eLine.Equals(new Line())) continue;
                var spt = new Point3dEx();
                var ept = new Point3dEx();

                if (sprayIn.PtDic[new Point3dEx(sLine.StartPoint)].Count == 1)
                {
                    spt = new Point3dEx(sLine.StartPoint);
                }
                //if(sprayIn.PtDic[new Point3dEx(sLine.EndPoint)].Count == 1)
                else
                {
                    spt = new Point3dEx(sLine.EndPoint);
                }

                if (sprayIn.PtDic[new Point3dEx(eLine.StartPoint)].Count == 1)
                {
                    ept = new Point3dEx(eLine.StartPoint);
                }
                if (sprayIn.PtDic[new Point3dEx(eLine.EndPoint)].Count == 1)
                {
                    ept = new Point3dEx(eLine.EndPoint);
                }

                if (pt1.DistanceTo(startPt) < pt2.DistanceTo(startPt))
                {
                    sprayIn.LoopStartPt = spt;
                    sprayIn.LoopEndPt = ept;
                }
                else
                {
                    sprayIn.LoopStartPt = ept;
                    sprayIn.LoopEndPt = spt;
                }
                break;
            }
        }
    }
}
