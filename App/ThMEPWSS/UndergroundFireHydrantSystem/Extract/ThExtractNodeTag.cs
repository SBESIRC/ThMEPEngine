using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractNodeTag//消火栓环管节点标记
    {
        public IEnumerable<BlockReference> Results { get; private set; }
        public DBObjectCollection DBobj { get; private set; }
        public void Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                try
                {
                    Results = acadDatabase
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => IsTargetBlock(o));
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(Results.ToCollection());
                    DBobj = spatialIndex.SelectCrossingPolygon(polygon);
                }
                catch (Exception ex)
                {
                    ;                
                }
            }
        }

        private bool IsTargetBlock(BlockReference block)
        {
            try
            {
                var blockName = block.GetEffectiveName();
                return blockName == "消火栓环管节点标记" ||
                       blockName == "消火栓环管节点标记-2";
            }
            catch
            {
                return false;
            }
        }

        public void GetPointList(FireHydrantSystemIn fireHydrantSysIn)
        {
            if (DBobj is null) return;
            double tolerance = 65;
            foreach (var db in DBobj)
            {
                double minDist1 = tolerance;
                double minDist2 = tolerance;

                var br = db as BlockReference;
                var ptls = new List<Point3dEx>();
                var mark1 = br.ObjectId.GetAttributeInBlockReference("节点1");
                var mark2 = br.ObjectId.GetAttributeInBlockReference("节点2");
                if ((db as BlockReference).GetEffectiveName() == "消火栓环管节点标记-2")
                {
                    mark1 = br.ObjectId.GetDynBlockValue("节点序号") + "'";
                    mark2 = br.ObjectId.GetDynBlockValue("节点序号") ;
                }

                var offset1x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X"));
                var offset1y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y"));
                var offset2x = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X"));
                var offset2y = Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y"));

                var ang1 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度1")) + br.Rotation - Math.PI / 2;
                var ang2 = Convert.ToDouble(br.ObjectId.GetDynBlockValue("角度2")) + br.Rotation - Math.PI / 2;
                var offset1 = new Point3d(offset1x, offset1y, 0);
                var offset2 = new Point3d(offset2x, offset2y, 0);
                var pt1 = offset1.TransformBy(br.BlockTransform);
                var pt2 = offset2.TransformBy(br.BlockTransform);
                
                var ptEx1 = new Point3dEx();
                var ptEx2 = new Point3dEx();
                foreach (var pt in fireHydrantSysIn.PtDic.Keys)
                {
                    if(fireHydrantSysIn.PtDic[pt].Count == 3)
                    {
                        var dist1 = Math.Abs(pt._pt.X-pt1.X) + Math.Abs(pt._pt.Y - pt1.Y);
                        var dist2 = Math.Abs(pt._pt.X - pt2.X) + Math.Abs(pt._pt.Y - pt2.Y);
                        if (dist1 < minDist1)
                        {
                            ptEx1 = pt;
                            minDist1 = dist1;
                        }
                        if (dist2 < minDist2)
                        {
                            ptEx2 = pt;
                            minDist2 = dist2;
                        }
                    } 
                }
                if(ptEx1.Equals(new Point3dEx()) || ptEx2.Equals(new Point3dEx()))
                {
                    continue;
                }
                ptls.Add(ptEx1);
                ptls.Add(ptEx2);
                fireHydrantSysIn.NodeList.Add(ptls);
                fireHydrantSysIn.AngleList.Add(ptEx1, ang1);
                fireHydrantSysIn.AngleList.Add(ptEx2, ang2);
                fireHydrantSysIn.MarkList.Add(ptEx1, mark1);
                fireHydrantSysIn.MarkList.Add(ptEx2, mark2);
            }
        }
    }
}
