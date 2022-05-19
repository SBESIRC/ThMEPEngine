using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.Model;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Text.RegularExpressions;
using ThMEPEngineCore.CAD;
using Linq2Acad;

namespace ThMEPWSS.Service
{
    public class CheckService
    {
        public List<ThIfcBeam> allBeams = new List<ThIfcBeam>();

        /// <summary>
        /// 判断是否距梁过近
        /// </summary>
        /// <param name="sprayPoly"></param>
        /// <param name="sprays"></param>
        /// <param name="dir"></param>
        /// <param name="dis"></param>
        /// <returns></returns>
        public bool CheckSprayData(Line sprayPoly, List<SprayLayoutData> sprays, Vector3d dir, double dis)
        {
            var polys = SprayDataOperateService.GetAllSanmeDirLines(dir, sprays);
            var pts = SprayDataOperateService.CalSprayPoint(polys, sprayPoly);
            foreach (var beam in allBeams)
            {
                foreach (var pt in pts)
                {
                    var closet = (beam.Outline as Polyline).GetClosestPointTo(pt, false);
                    if (closet.DistanceTo(pt) < dis)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 校验边界防止产生新盲区
        /// </summary>
        /// <param name="boundarys"></param>
        /// <param name="position"></param>
        /// <param name="newPosition"></param>
        /// <param name="maxLenghth"></param>
        /// <returns></returns>
        public bool CheckBoundaryLines(List<Line> boundarys, Point3d position, Point3d newPosition, double maxLenghth)
        {
            foreach (var bLine in boundarys)
            {
                Point3d closetPt = bLine.GetClosestPointTo(position, true);
                double length = closetPt.DistanceTo(position);

                Point3d newClosetPt = bLine.GetClosestPointTo(newPosition, true);
                double newLength = newClosetPt.DistanceTo(newPosition);

                if (length <= maxLenghth && newLength > maxLenghth)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断两个喷淋点是否满足间距
        /// </summary>
        /// <param name="position"></param>
        /// <param name="newPosition"></param>
        /// <param name="maxSpacing"></param>
        /// <param name="minSpacing"></param>
        /// <returns></returns>
        public bool CheckSprayPtDistance(Point3d position, Point3d newPosition, double maxSpacing, double minSpacing)
        {
            double distance = newPosition.DistanceTo(position);
            Vector3d compareDir = (position - newPosition).GetNormal();
            double compareX = Math.Abs(compareDir.X) > Math.Abs(compareDir.Y) ? Math.Abs(compareDir.X) : Math.Abs(compareDir.Y);
            double compareValue = distance * compareX;

            return compareValue >= minSpacing && compareValue <= maxSpacing;
        }

        /// <summary>
        /// 判断移动之后的点是否落在了洞内
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public bool CheckSprayWithHoles(Point3d newPosition, List<Polyline> holes)
        {
            return !(holes.Where(x => x.Contains(newPosition)).Count() > 0);
        }

        /// <summary>
        /// 判断喷淋图块的大小是否满足要求
        /// </summary>
        /// <param name="block"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public bool CheckSprayBlockSize(Entity block, double maxLength)
        {
            var extents = block.Bounds;
            if (extents == null)
            {
                return false;
            }

            var length = extents.Value.MinPoint.DistanceTo(extents.Value.MaxPoint) / 2;
            return length <= maxLength;
        }

        /// <summary>
        /// 判断图层状态
        /// </summary>
        /// <param name="db"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public bool CheckLayerStatus(Database db, string layerName)
        {
            //打开层表
            LayerTable lt = (LayerTable)db.LayerTableId.GetObject(OpenMode.ForRead);
            //如果不存在名为layerName的图层，则返回
            if (!lt.Has(layerName)) return false;
            ObjectId layerId = lt[layerName];//获取名为layerName的层表记录的Id
            //以写的方式打开名为layerName的层表记录
            LayerTableRecord ltr = (LayerTableRecord)layerId.GetObject(OpenMode.ForWrite);
            if (ltr != null)
            {
                if (ltr.IsLocked && ltr.IsFrozen && ltr.IsOff)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 检查外参
        /// </summary>
        /// <returns></returns>
        public static bool CheckXref(BlockReference br, Regex regex)
        {
            if (br.Database is null) return false;
            using var currentDb = AcadDatabase.Active();
            XrefGraph xrg = currentDb.Database.GetHostDwgXrefGraph(false);
            if (xrg?.RootNode is null) return false;
            string name = "";
            ThXrefDbExtension.XRefNodeName(xrg.RootNode, br.Database, ref name);
            name ??= "";
            return regex.IsMatch(name);
        }
        static readonly Regex reW = new Regex("^W");
        public static bool CheckWssXref(BlockReference br)
        {
            return CheckXref(br, reW);
        }
    }
}
