using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using DotNetARX;
using ThMEPWSS.Uitl;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class Floor
    {
        public DBObjectCollection DBObjs { get; set; }//楼层框定
        public void Extract(Database database, Point3dCollection polygon)
        {
            var objs = new DBObjectCollection();
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var Results = acadDatabase
                    .ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => IsTargetLayer(o.Layer))
                    .Where(o => IsTraget(o))
                    .ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBObjs = spatialIndex.SelectCrossingPolygon(polygon);
            }
        }
        private bool IsTargetLayer(string layer)
        {
            return layer.ToUpper() == "0";
        }
        private bool IsTraget(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            return blkName.Contains("楼层框定");
        }
        public Dictionary<string, Polyline> CreateRects(AcadDatabase acadDatabase)
        {
            var floorsDic = new Dictionary<string, Polyline>();
            foreach (var db in DBObjs)
            {
                var br = db as BlockReference;
                var pline = new Polyline();
                string floor = br.ObjectId.GetAttributeInBlockReference("楼层编号");
                var pt1 = br.GeometricExtents.MinPoint.ToPoint2D();
                var pt2 = br.GeometricExtents.MaxPoint.ToPoint2D();
                pline.CreateRectangle(pt1, pt2);
                floorsDic.Add(floor, pline);
            }
            return floorsDic;
        }
    }
}
