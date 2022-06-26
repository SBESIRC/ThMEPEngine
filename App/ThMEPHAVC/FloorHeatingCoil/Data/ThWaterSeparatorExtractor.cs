using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Service;


namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThWaterSeparatorExtractor : ThExtractService
    {
        public List<BlockReference> Blocks { get; set; }
        public string BlockName { get; set; }
        public ThWaterSeparatorExtractor()
        {
            BlockName = "";
            Blocks = new List<BlockReference>();
        }
        public override void Extract(Database db, Point3dCollection pts)
        {
            using (var acadDatabase = AcadDatabase.Use(db))
            {

                Blocks = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(b => !b.BlockTableRecord.IsNull)
                    .Where(b => IsElementLayer(b.Layer) && IsBlockName(b.GetEffectiveName()))
                    .ToList();

                if (pts.Count >= 3)
                {
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Blocks.ToCollection());
                    var objs = spatialIndex.SelectCrossingPolygon(pts);
                    Blocks = objs.Cast<BlockReference>().ToList();
                }
            }
        }
        private bool IsBlockName(string blkName)
        {
            var b= blkName.ToUpper().Contains (this.BlockName.ToUpper());
            return b;
        }

        public override bool IsElementLayer(string layer)
        {
            if (string.IsNullOrEmpty(this.ElementLayer))
            {
                //不考虑图层
                return true;
            }
            else
            {
                return base.IsElementLayer(layer);
            }
        }
    }

}
