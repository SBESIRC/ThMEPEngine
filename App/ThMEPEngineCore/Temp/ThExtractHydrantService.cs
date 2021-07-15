using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPEngineCore.Temp
{
    public class ThExtractHydrantService : ThExtractService
    {
        public List<DBPoint> Hydrants { get; set; }
        public ThExtractHydrantService()
        {
            Hydrants = new List<DBPoint>();
        }
        public override void Extract(Database db,Point3dCollection pts)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(db))
            {
                var blocks = new List<BlockReference>();
                foreach (var ent in acadDatabase.ModelSpace)
                {
                    if (ent is BlockReference br && br.Bounds != null)
                    {
                        if (IsHydrantBlkName(br.GetEffectiveName()))
                        {
                            blocks.Add(br);
                        }
                    }
                }

                if(pts.Count>=3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(blocks.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    blocks = objs.Cast<BlockReference>().ToList();
                }

                foreach (var br in blocks)
                {
                    var obb = GetBlockOBB(db, br, br.BlockTransform);
                    var firstPt = obb.GetPoint3dAt(0);
                    var cornerPt = obb.GetPoint3dAt(2);
                    var dbPoint = new DBPoint(firstPt.GetMidPt(cornerPt));
                    Hydrants.Add(dbPoint);
                }
            }
        }

        private bool IsHydrantBlkName(string blkName)
        {
            string queryChars = "-新";
            int index = blkName.LastIndexOf(queryChars);
            if(index + queryChars.Length == blkName.Length)
            {
                return blkName.Contains("消火栓");
            }
            return false;
        }

        private Polyline GetBlockOBB(Database database, BlockReference blockObj, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var btr = acadDatabase.Blocks.Element(blockObj.BlockTableRecord);
                var polyline = btr.GeometricExtents().ToRectangle().GetTransformedCopy(matrix) as Polyline;
                return polyline;
            }
        }
    }
}
