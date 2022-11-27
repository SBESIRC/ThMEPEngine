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
using ThCADExtension;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class PipeDnLineNew//新的提取引线逻辑，用于跨层
    {
        public List<Entity> Results { get; private set; }
        public DBObjectCollection DBObjs { get; private set; }
        public List<Line> DBObjResults { get; private set; }
        public List<Line> LabelPosition { get; private set; }

        public PipeDnLineNew()
        {
            DBObjResults = new List<Line>();
        }

        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<Entity>()
                   .Where(e => e is Line)
                   .Where(o => IsPipeDNLayer(o.Layer))
                   .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);

                foreach (var db in DBObjs)
                {
                    try
                    {
                        if (db is Line line)//线段直接添加
                        {
                            DBObjResults.Add(line);
                        }
                        else if (db is Polyline pline)//多段线打断添加
                        {

                            for (int i = 0; i < pline.NumberOfVertices - 1; i++)
                            {
                                var pt1 = pline.GetPoint3dAt(i).ToPoint2D().ToPoint3d();
                                var pt2 = pline.GetPoint3dAt(i + 1).ToPoint2D().ToPoint3d();
                                DBObjResults.Add(new Line(pt1, pt2));
                            }
                        }
                        else
                        {
                            var br = new DBObjectCollection();
                            (db as Entity).Explode(br);
                            foreach (var l in br)
                            {
                                if (l is Line line1)
                                {
                                    DBObjResults.Add(line1);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                }
            }
        }

        private bool IsPipeDNLayer(string layer)
        {
            return (layer.ToUpper().Contains("W-FRPT-NOTE"));
        }

        public List<Point3d> ExtractSlash()
        {
            var SlashPts = new List<Point3d>();
            foreach (var line in DBObjResults)
            {
                if (PointAngle.IsSplashLine(line))
                {
                    var pt1 = line.StartPoint;
                    var pt2 = line.EndPoint;
                    var pt = new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);//中点作为斜线代表点
                    SlashPts.Add(pt);
                }
            }
            return SlashPts;
        }

        public Dictionary<Line, List<Point3d>> ExtractleadLine(List<Point3d> SlashPts)
        {
            var leadLineDic = new Dictionary<Line, List<Point3d>>();
            foreach (var line in DBObjResults)
            {
                var ptList = new List<Point3d>();
                if (!PointAngle.IsSplashLine(line))//不是斜线才可能是引线
                {
                    foreach (var pt in SlashPts)//遍历斜线点
                    {
                        if (PtOnLine.PtIsOnLine(pt, line))//斜线在引线上
                        {
                            ptList.Add(pt);//添加
                        }
                    }
                }
                if (ptList.Count != 0)//引线存在斜线
                {
                    //对斜线点进行排序，按照从左到右或者从上到下
                    ptList.PointsSort();
                    leadLineDic.Add(line, ptList);
                }
            }
            return leadLineDic;
        }

        public Dictionary<Line, List<Line>> ExtractSegLine(Dictionary<Line, List<Point3d>> leadLineDic)
        {
            var segLineDic = new Dictionary<Line, List<Line>>();
            foreach (var lead in leadLineDic.Keys)
            {
                var lineList = new List<Line>();
                foreach (var line in DBObjResults)
                {
                    if (!PointAngle.IsSplashLine(line) && !leadLineDic.Keys.Contains(line))
                    {
                        if (PtOnLine.PtIsOnLine(line.StartPoint, lead) || PtOnLine.PtIsOnLine(line.EndPoint, lead))
                        {
                            lineList.Add(line);
                        }
                    }
                }
                lineList.LinesSort();
                segLineDic.Add(lead, lineList);
            }
            return segLineDic;
        }
    }
}
