using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;

namespace ThMEPArchitecture.ParkingStallArrangement.Extractor
{
    public static class InputData
    {
        private static BlockReference SelectBlock(AcadDatabase acadDatabase)
        {
            var entOpt = new PromptEntityOptions("\n请选择地库:");
            var entityResult = Active.Editor.GetEntity(entOpt);
            var entId = entityResult.ObjectId;
            var dbObj = acadDatabase.Element<Entity>(entId);
            if (dbObj is BlockReference blk)
            {
                return blk;
            }
            else
            {
                Active.Editor.WriteMessage("选择的地库对象不是一个块！");
                return null;
            }
        }
        public static DBObjectCollection SelectObstacles(AcadDatabase acadDatabase)
        {
            var entOpt = new PromptSelectionOptions { MessageForAdding = "\n请选择包含障碍物的块:" };
            var result = Active.Editor.GetSelection(entOpt);
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            var objs = new DBObjectCollection();
            foreach (var id in result.Value.GetObjectIds())
            {

                var obj = acadDatabase.Element<Entity>(id);
                if(obj is BlockReference blk)
                {
                    objs.Add(blk);
                }
            }
            return objs;
        }
        public static bool GetOuterBrder(AcadDatabase acadDatabase, out OuterBrder outerBrder, Serilog.Core.Logger Logger = null)
        {
            outerBrder = new OuterBrder();
            var block = SelectBlock(acadDatabase);//提取地库对象
            if (block is null)
            {
                return false;
            }
            var extractRst = outerBrder.Extract(block);//提取多段线
            if (!extractRst)
            {
                return false;
            }
            if (!(Logger == null))
            {
                //check seg lines
                if (IsOrthogonal(outerBrder.SegLines, out List<Line> NewSegLines, Logger))
                {
                    outerBrder.SegLines = NewSegLines;
                }
                else
                {
                    return false;
                }
                if (!IsValidSegLines(outerBrder.SegLines, outerBrder.WallLine, Logger)) return false;
            }

            return true;
        }

        private static bool IsOrthogonal(List<Line> SegLines, out List<Line> NewSegLines, Serilog.Core.Logger Logger)
        {
            double tol = 0.02;// arctan 0.02 （1.146°）以下的交会自动归正
            NewSegLines = new List<Line>();
            for (int i = 0; i < SegLines.Count; i++)
            {

                var line = SegLines[i];
                var spt = line.StartPoint;

                var ept = line.EndPoint;
                //1. check parallel, perpendicular
                var X_dif = Math.Abs(spt.X - ept.X);
                var Y_dif = Math.Abs(spt.Y - ept.Y);
                if (Y_dif > X_dif)// 垂直线
                {
                    if (X_dif / Y_dif > tol)
                    {
                        Logger?.Information("发现非正交分割线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分割线 ！\n");
                        Active.Editor.WriteMessage("起始点：" + spt.ToString() + "\n");
                        Active.Editor.WriteMessage("终点：" + ept.ToString() + "\n");
                        return false;
                    }
                    var newX = (spt.X + ept.X) / 2;
                    spt = new Point3d(newX, spt.Y, 0);
                    ept = new Point3d(newX, ept.Y, 0);
                    NewSegLines.Add(new Line(spt, ept));
                }
                if (X_dif > Y_dif)// 水平线
                {
                    if (Y_dif / X_dif > tol)
                    {
                        Logger?.Information("发现非正交分割线 ！\n");
                        Logger?.Information("起始点：" + spt.ToString() + "终点：" + ept.ToString() + "的分割线不符合要求\n");
                        Active.Editor.WriteMessage("发现非正交分割线 ！\n");
                        Active.Editor.WriteMessage("起始点：" + spt.ToString() + "\n");
                        Active.Editor.WriteMessage("终点：" + ept.ToString() + "\n");
                        return false;
                    }
                    var newY = (spt.Y + ept.Y) / 2;
                    spt = new Point3d(spt.X, newY, 0);
                    ept = new Point3d(ept.X, newY, 0);
                    NewSegLines.Add(new Line(spt, ept));
                }
            }
            return true;
        }

        private static bool IsValidSegLines(List<Line> SegLines, Polyline WallLine, Serilog.Core.Logger Logger)
        {
            //double tol = 1e-4;
            for (int i = 0; i < SegLines.Count; i++)
            {
                var pts = new List<Point3d>();
                var line = SegLines[i];
                var spt = line.StartPoint;

                // check intersection points
                pts.AddRange(line.Intersect(WallLine, 0));//求与边界的交点
                for (int j = 0; j < SegLines.Count; j++)
                {
                    if (i == j) continue;
                    pts.AddRange(line.Intersect(SegLines[j], 0));//求与其他分割线的交点
                }
                var orderPts = pts.OrderBy(p => p.DistanceTo(line.StartPoint)).ToList();
                if (orderPts.Count < 2)
                {
                    Logger?.Information("该分割线只有" + orderPts.Count.ToString() + "个交点" + "\n");
                    Logger?.Information("起始点：" + line.StartPoint.ToString() + "终点：" + line.EndPoint.ToString() + "的分割线不符合要求" + "\n");
                    Active.Editor.WriteMessage("该分割线只有" + orderPts.Count.ToString() + "个交点" + "\n");
                    Active.Editor.WriteMessage("起始点：" + line.StartPoint.ToString() + "\n");
                    Active.Editor.WriteMessage("终点：" + line.EndPoint.ToString() + "\n");
                    return false;
                }
                // Check if two intersection points on the segline are the same
                //for(int k = 0;k < orderPts.Count-1; k++)
                //{
                //    //logger.
                //    if (orderPts[k].Equals(orderPts[k + 1]))
                //    {
                //        Logger?.Information("最多两条线（分割线或者边界线）相交与同一点 \n");
                //        Logger?.Information("交点：" + orderPts[k].ToString());
                //        Active.Editor.WriteMessage("最多两条线相交于同一点");
                //        Active.Editor.WriteMessage("交点：" + orderPts[k].ToString());

                //        return false;
                //    }
                //}
            }
            return true;
        }
    }
}
