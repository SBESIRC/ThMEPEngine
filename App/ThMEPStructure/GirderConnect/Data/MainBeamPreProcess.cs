using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPStructure.GirderConnect.Data.Utils;

namespace ThMEPStructure.GirderConnect.Data
{
    class MainBeamPreProcess
    {
        /// <summary>
        /// 对主梁连接算法输入的预先处理
        /// </summary>
        /// <param name="outsideColumns"></param>
        /// <param name="shearwallGroupDict"></param>
        /// <param name="columnGroupDict"></param>
        /// <param name="outsideShearwall"></param>
        /// <param name="clumnPts"></param>
        /// <param name="outlineWalls"></param>
        /// <param name="outlineClumns"></param>
        public static void MPreProcess(List<Entity> outsideColumns, Dictionary<Entity, HashSet<Entity>> shearwallGroupDict, Dictionary<Entity, HashSet<Entity>> columnGroupDict,
            List<Entity> outsideShearwall, Point3dCollection clumnPts, ref Dictionary<Polyline, HashSet<Polyline>> outlineWalls, Dictionary<Polyline, HashSet<Point3d>> outlineClumns, 
            ref Dictionary<Polyline, HashSet<Polyline>> outerWalls, ref Dictionary<Polyline, HashSet<Point3d>> olCrossPts, ref Dictionary<Polyline, Polyline> outline2OriOutline, ref HashSet<Point3d> allColumnPts)
        {
            //0、内部柱分类
            //columnGroupDict->outlineWalls/outlinePlColumns
            Dictionary<Polyline, HashSet<Polyline>> outlinePlColumns = new Dictionary<Polyline, HashSet<Polyline>>();
            DataClassify.InnerColumnTypeClassify(columnGroupDict, ref outlineWalls, ref outlinePlColumns);
            //0.1、去重合
            Dictionary<Polyline, HashSet<Polyline>> newOutlinePlColumns = new Dictionary<Polyline, HashSet<Polyline>>();
            foreach (var columnGroup in outlinePlColumns)
            {
                newOutlinePlColumns.Add(columnGroup.Key, new HashSet<Polyline>());
                newOutlinePlColumns[columnGroup.Key] = DataProcess.DeleteOverlap(columnGroup.Value);
            }

            //1、内部墙
            //shearwallGroupDict->outlineWalls
            DataProcess.PolylineAddToOutlineWalls(shearwallGroupDict, ref outlineWalls);

            //1.1、内部墙合并+去毛边
            outlineWalls = DataProcess.MergeWithSimplifyWalls(outlineWalls);

            //1.2、将outlinePlColumns加入outlineWalls
            DataProcess.DicHashAdd(newOutlinePlColumns, ref outlineWalls);

            //1.3、合并outlineWalls
            //墙墙、墙柱、柱柱相交归一
            outlineWalls = DataProcess.MergeWall(outlineWalls);

            //1.4、对房间中的事物进行分类
            DataClassify.ClassifyOutlineWalls(ref outlineWalls, ref outlineClumns);

            //1.5、清理outlineWalls
            outlineWalls = DataClassify.SimplifyOutlineThings(outlineWalls);

            //2、对于房间外的事物（wall & column）
            //outsideColumns & outsideShearwall -> clumnPts & outerWalls
            DataClassify.OuterClassify(outsideColumns, outsideShearwall, clumnPts, ref outerWalls, ref olCrossPts, ref outline2OriOutline);

            //2.1、清理outerWalls+
            outerWalls = DataClassify.SimplifyOutlineThings(outerWalls);

            //3、生成outlineToPts
            //outsideColumns、clumnPts、outlineClumns
            DataClassify.ClassifyColumnPoints(ref allColumnPts, outsideColumns, outlineClumns);
        }
    }
}
