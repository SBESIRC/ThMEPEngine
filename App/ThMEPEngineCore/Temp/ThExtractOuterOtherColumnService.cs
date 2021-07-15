using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractOuterOtherColumnService :ThExtractService,IColumnData
    {
        public List<Entity> OuterColumns { get; set; }
        public List<Entity> OtherColumns { get; set; }
        /// <summary>
        /// 其他柱颜色
        /// </summary>
        public int OtherColorIndex { get; set; }
        public ThExtractOuterOtherColumnService()
        {
            OuterColumns = new List<Entity>();
            OtherColumns = new List<Entity>();
            OtherColorIndex = 256; // 默认是Bylayer,除此以外都是外圈柱
        }

        public override void Extract(Database db, Point3dCollection pts)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var columns = acadDb.ModelSpace.OfType<Polyline>().Where(o => IsElementLayer(o.Layer)).ToList();
                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(columns.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    columns = objs.Cast<Polyline>().ToList();
                }
                // 0 ->ByBlock,256->ByLayer
                OuterColumns = columns.Where(o => o.ColorIndex != OtherColorIndex).Select(o => o.Clone() as Entity).ToList();
                OtherColumns = columns.Where(o => o.ColorIndex == OtherColorIndex).Select(o => o.Clone() as Entity).ToList();
            }
        }
    }
}
