using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Service;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractPipeMark
    {
        public IEnumerable<BlockReference> Results { get; private set; }
        public DBObjectCollection DBobj { get; private set; }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                Results = acadDatabase
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => IsTargetBlock(o)).ToList();
                
                var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                DBobj = spatialIndex.SelectCrossingPolygon(polygon);
            }
        }
        private bool IsTargetBlock(BlockReference block)
        {
            try
            {
                var valve = block.GetEffectiveName();
                return valve == "消火栓环管标记";
            }
            catch
            {
                return false;
            }
        }

        public List<List<Point3d>> GetPipeMarkPoisition(out Dictionary<Point3dEx, double> markAngleDic)
        {
            markAngleDic = new Dictionary<Point3dEx, double>();
            var poisition = new List<List<Point3d>>();
            foreach (var db in DBobj)
            {
                var pos = new List<Point3d>();
                var br = db as BlockReference;
                var offset1x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X"));
                var offset1y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y"));
                var offset2x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X"));
                var offset2y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y"));

                var offset1 = new Point3d(offset1x, offset1y, 0);
                var offset2 = new Point3d(offset2x, offset2y, 0);
                var pt1 = offset1.TransformBy(br.BlockTransform);
                var pt2 = offset2.TransformBy(br.BlockTransform);

                var ang1 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度1")) + br.Rotation - Math.PI / 2;
                var ang2 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度2")) + br.Rotation - Math.PI / 2;
                
                pos.Add(pt1);
                pos.Add(pt2);
                poisition.Add(pos);
                markAngleDic.Add(new Point3dEx(pt1), ang1);
                markAngleDic.Add(new Point3dEx(pt2), ang2);
            }
            return poisition;
        }
    }
}
