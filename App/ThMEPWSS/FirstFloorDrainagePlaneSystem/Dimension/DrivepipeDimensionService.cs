using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Model;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.Print;
using ThMEPWSS.FirstFloorDrainagePlaneSystem.ViewModel;

namespace ThMEPWSS.FirstFloorDrainagePlaneSystem.Dimension
{
    public class DrivepipeDimensionService
    {
        double length = 157;
        double noteLength = 500;
        double dimLength = 1000;
        double txtMoveLength = 80;
        List<RouteModel> routes;
        List<Polyline> outFrames;
        FirstFloorPlaneViewModel firstFloorPlane;
        public DrivepipeDimensionService(List<RouteModel> _routes, List<Polyline> _outFrames, FirstFloorPlaneViewModel _firstFloorPlane)
        {
            routes = _routes.Where(x => !x.IsBranchPipe).ToList();
            outFrames = _outFrames;
            firstFloorPlane = _firstFloorPlane;
        }

        public void CreateDim()
        {
            var layoutInfos = GetLayoutInfo();
            Layout(layoutInfos);
        }

        /// <summary>
        /// 布置
        /// </summary>
        /// <param name="layoutInfos"></param>
        private void Layout(List<List<KeyValuePair<Point3d, Vector3d>>> layoutInfos)
        {
            GetDrivepipeType(out string type, out string size);
            var attri = new Dictionary<string, string>() { { "可见性", type } };
            foreach (var lInfo in layoutInfos)
            {
                LayoutDrivepipe(lInfo, attri);  //放置套管
                LayoutDimension(lInfo, size);
            }
        }

        /// <summary>
        /// 布置套管
        /// </summary>
        /// <param name="layoutInfo"></param>
        /// <param name="attri"></param>
        private void LayoutDrivepipe(List<KeyValuePair<Point3d, Vector3d>> layoutInfo, Dictionary<string, string> attri)
        {
            var lst = new List<KeyValuePair<Point3d, Vector3d>>();
            foreach (var lInfo in layoutInfo)
            {
                var dir = lInfo.Value;
                var pt = lInfo.Key + dir * length;
                lst.Add(new KeyValuePair<Point3d, Vector3d>(pt, dir));
            }
            InsertBlockService.InsertBlock(lst, ThWSSCommon.DrivepipeLayerName, ThWSSCommon.DrivepipeBlockName, attri);
        }

        /// <summary>
        /// 布置标注引线和文字
        /// </summary>
        /// <param name="layoutInfo"></param>
        /// <param name="size"></param>
        private void LayoutDimension(List<KeyValuePair<Point3d, Vector3d>> layoutInfo, string size)
        {
            var moveDir = layoutInfo.First().Value;
            var dir = Vector3d.ZAxis.CrossProduct(moveDir);
            var allPts = OrderPipes(dir, layoutInfo.Select(x => x.Key).ToList());
            var connectPt = allPts.First() + noteLength * dir - moveDir * noteLength;
            var noteLines = new List<Polyline>();
            foreach (var pt in allPts)
            {
                Polyline polyline = new Polyline();
                polyline.AddVertexAt(0, pt.ToPoint2D(), 0, 0, 0);
                polyline.AddVertexAt(1, connectPt.ToPoint2D(), 0, 0, 0);
                noteLines.Add(polyline);
            }
            var connectDir = Vector3d.XAxis;
            if (connectDir.DotProduct(moveDir) > 0.001)
            {
                if (connectDir.DotProduct(moveDir) < 0)
                {
                    connectDir = -connectDir;
                }
            }
            else if (connectDir.DotProduct(dir) < 0)
            {
                connectDir = -connectDir;
            }
            var lastPt = connectPt + connectDir * dimLength;
            Polyline dimPoly = new Polyline();
            dimPoly.AddVertexAt(0, lastPt.ToPoint2D(), 0, 0, 0);
            dimPoly.AddVertexAt(1, connectPt.ToPoint2D(), 0, 0, 0);
            noteLines.Add(dimPoly);

            var dbTexts = new List<DBText>();
            var midPt =new Point3d((connectPt.X + lastPt.X) / 2, (connectPt.Y + lastPt.Y) / 2, 0);
            var txtDir = Vector3d.YAxis;
            var txtPt = midPt + txtMoveLength * txtDir;
            var dimtext = new DBText() { Height = 200, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = size, Position = txtPt, AlignmentPoint = txtPt };
            dbTexts.Add(dimtext);
            var levelPt = midPt - txtMoveLength * txtDir;
            var leveltext = new DBText() { Height = 200, WidthFactor = 0.7, HorizontalMode = TextHorizontalMode.TextMid, TextString = firstFloorPlane.DrivepipeLevel.ToString(), Position = levelPt, AlignmentPoint = levelPt };
            dbTexts.Add(leveltext);

            PrintMarks.PrintNoteLines(noteLines);
            PrintMarks.PrintText(dbTexts);
        }

        /// <summary>
        /// 排序管线
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="allPts"></param>
        /// <returns></returns>
        private List<Point3d> OrderPipes(Vector3d dir, List<Point3d> allPts)
        {
            var xDir = dir;
            var yDir = Vector3d.ZAxis.CrossProduct(xDir);
            var zDir = Vector3d.ZAxis;
            Matrix3d matrix = new Matrix3d(new double[]{
                    xDir.X, yDir.X, zDir.X, 0,
                    xDir.Y, yDir.Y, zDir.Y, 0,
                    xDir.Z, yDir.Z, zDir.Z, 0,
                    0.0, 0.0, 0.0, 1.0});
            return allPts.ToDictionary(x => x, y => y.TransformBy(matrix.Inverse())).OrderByDescending(x => x.Value.X).Select(x => x.Key).ToList();
        }

        /// <summary>
        /// 分类布置管线
        /// </summary>
        /// <returns></returns>
        private List<List<KeyValuePair<Point3d, Vector3d>>> GetLayoutInfo()
        {
            var layoutInfos = new List<List<KeyValuePair<Point3d, Vector3d>>>();
            while (routes.Count > 0)
            {
                var firRoute = routes.First();
                var connectLine = firRoute.route;
                var resPoly = GetOutFrames(connectLine);
                if (resPoly != null)
                {
                    var resRoutes = routes.Where(x => resPoly.IsIntersects(x.route)).ToList();
                    var layoutInfo = GetLayoutPt(resRoutes, resPoly);
                    layoutInfos.Add(layoutInfo);
                    routes = routes.Except(resRoutes).ToList();
                }
                else
                {
                    routes.Remove(firRoute);
                }
            }

            return layoutInfos;
        }

        /// <summary>
        /// 获取同一个出户框线下的连接管线
        /// </summary>
        /// <param name="resRoutes"></param>
        /// <param name="resPoly"></param>
        /// <returns></returns>
        private List<KeyValuePair<Point3d, Vector3d>> GetLayoutPt(List<RouteModel> resRoutes, Polyline resPoly)
        {
            var layoutInfo = new List<KeyValuePair<Point3d, Vector3d>>();
            foreach (var rRoute in resRoutes)
            {
                var pts = resPoly.IntersectWithEx(rRoute.route);
                var endPt = rRoute.route.GetPoint3dAt(0);
                if (pts.Count >= 2)
                {
                    var orderPts = pts.Cast<Point3d>().OrderBy(x => x.DistanceTo(endPt)).ToList();
                    var firPt = orderPts.First();
                    var secPt = orderPts.Last();
                    var midPt = new Point3d((firPt.X + secPt.X) / 2, (firPt.Y + secPt.Y) / 2, 0);
                    var dir = (secPt - firPt).GetNormal();
                    layoutInfo.Add(new KeyValuePair<Point3d, Vector3d>(midPt, dir));
                }
            }
            return layoutInfo;
        }

        /// <summary>
        /// 获取最外层出户框线
        /// </summary>
        /// <param name="connectLine"></param>
        /// <returns></returns>
        private Polyline GetOutFrames(Polyline connectLine)
        {
            var endPt = connectLine.GetPoint3dAt(0);
            var interPolys = outFrames.Where(x => x.IsIntersects(connectLine))
                .OrderBy(x => x.GetClosestPointTo(endPt, false).DistanceTo(endPt)).ToList();
            return interPolys.FirstOrDefault();
        }

        /// <summary>
        /// 获取套管布置类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="size"></param>
        private void GetDrivepipeType(out string type, out string size)
        {
            type = "";
            size = "";
            switch (firstFloorPlane.DirvepipeDimensionType.Value)
            {
                case (int)DirvepipeDimensionTypeEnum.OrdinarySteel:
                    type = "C型防护密闭套管（单侧挡板）";
                    size = "DN100";
                    break;
                case (int)DirvepipeDimensionTypeEnum.BSteelWaterproof:
                    type = "B型刚性防水套管/A型防护密闭套管";
                    size = "BG-DN100";
                    break;
                case (int)DirvepipeDimensionTypeEnum.AFlexibleWaterproof:
                    type = "A型柔性防水套管";
                    size = "AR-DN100";
                    break;
                case (int)DirvepipeDimensionTypeEnum.AProtectiveSealing:
                    type = "B型刚性防水套管/A型防护密闭套管";
                    size = "AF-DN100";
                    break;
                case (int)DirvepipeDimensionTypeEnum.CProtectiveSealing:
                    type = "C型防护密闭套管（单侧挡板）";
                    size = "CF-DN100";
                    break;
                case (int)DirvepipeDimensionTypeEnum.EProtectiveSealing:
                default:
                    type = "E型防护密闭套管（双侧挡板）";
                    size = "EF-DN100";
                    break;
            }
        }
    }
}
