using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System.Diagnostics;

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
            if (dbObj is BlockReference)
            {
                return (BlockReference)dbObj;
            }
            else
            {
                Active.Editor.WriteMessage("选择的地库对象不是一个块！");
                return null;
            }
            
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
            if(!(Logger == null))
            {
                //check seg lines
                if (!IsValidSegLines(outerBrder.SegLines, outerBrder.WallLine, Logger)) return false;
            }
            
            return true;
        }

        private static bool IsValidSegLines(List<Line> SegLines, Polyline WallLine, Serilog.Core.Logger Logger)
        {
            double tol = 1e-4;
            for (int i = 0; i < SegLines.Count; i++)
            {
                var pts = new List<Point3d>();
                var line = SegLines[i];
                var spt = line.StartPoint;
                //1. check parallel, perpendicular
                if (Math.Abs(line.StartPoint.X - line.EndPoint.X) > tol && Math.Abs(line.StartPoint.Y - line.EndPoint.Y) > tol)
                {
                    Logger?.Information("发现非正交分割线 ！");
                    Logger?.Information("起始点：" + line.StartPoint.ToString() + "终点：" + line.EndPoint.ToString() + "的分割线不符合要求");
                    Active.Editor.WriteMessage("发现非正交分割线 ！");
                    Active.Editor.WriteMessage("起始点：" + line.StartPoint.ToString());
                    Active.Editor.WriteMessage("终点：" + line.EndPoint.ToString());
                    return false;
                }
                //2. check intersection points
                pts.AddRange(line.Intersect(WallLine, 0));//求与边界的交点
                for (int j = 0; j < SegLines.Count; j++)
                {
                    if (i == j) continue;
                    pts.AddRange(line.Intersect(SegLines[j], 0));//求与其他分割线的交点
                }
                var orderPts = pts.OrderBy(p => p.DistanceTo(line.StartPoint)).ToList();
                if (orderPts.Count < 2)
                {
                    Logger?.Information("该分割线只有" + orderPts.Count.ToString() + "个交点");
                    Logger?.Information("起始点："+ line.StartPoint.ToString()+"终点："+ line.EndPoint.ToString()+"的分割线不符合要求");
                    Active.Editor.WriteMessage("该分割线只有"+ orderPts.Count.ToString() + "个交点");
                    Active.Editor.WriteMessage("起始点：" + line.StartPoint.ToString());
                    Active.Editor.WriteMessage("终点：" + line.EndPoint.ToString());
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
