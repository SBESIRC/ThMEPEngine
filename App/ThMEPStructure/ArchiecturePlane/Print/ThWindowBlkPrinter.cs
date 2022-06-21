using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchiecturePlane.Service;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThWindowBlkPrinter:ThComponentBlkPrinter
    {
        public override ObjectIdCollection Print(Database db, List<ThComponentInfo> windowInfos, double scale = 1.0)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var results = new ObjectIdCollection();
                windowInfos.ForEach(o =>
                {
                    var startPt = o.Start.ToPoint3d();
                    var endPt = o.End.ToPoint3d();
                    if (startPt.HasValue && endPt.HasValue)
                    {
                        var line = new Line(startPt.Value, endPt.Value);                        
                        string layerName = ThArchPrintLayerManager.AEWIND;
                        string blkName = GetWindowBlkName(line.Length / scale);
                        double insertLength = line.Length;                        
                        if (!string.IsNullOrEmpty(blkName))
                        {
                            var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                                           layerName,blkName,Point3d.Origin,
                                           new Scale3d(scale),0.0);
                            var entity = acadDb.Element<BlockReference>(blkId, true);
                            // 调整长度
                            if (entity.IsDynamicBlock)
                            {
                                foreach (DynamicBlockReferenceProperty property in entity.DynamicBlockReferencePropertyCollection)
                                {
                                    if (property.PropertyName == "距离")
                                    {
                                        property.Value = insertLength;
                                        break;
                                    }
                                }
                            }
                            // 先往下移动，保持窗户中心与OX在一条线
                            var height = entity.GeometricExtents.MaxPoint.Y -
                            entity.GeometricExtents.MinPoint.Y;
                            var mt1= Matrix3d.Displacement(new Vector3d(-1.0 * line.Length / 2.0, -height/2.0, 0));
                            var mt2 = Matrix3d.Rotation(line.Angle,Vector3d.ZAxis,Point3d.Origin);
                            var midPt = line.StartPoint.GetMidPt(line.EndPoint);
                            var mt3 = Matrix3d.Displacement(midPt - Point3d.Origin);
                            entity.TransformBy(mt1);
                            entity.TransformBy(mt2);
                            entity.TransformBy(mt3);
                            results.Add(blkId);
                            o.Element = entity;
                            line.Dispose();
                        }
                    }
                });
                return results;
            }
        }

        private string GetWindowBlkName(double length)
        {
            if (length >= 30)
            {
                return ThArchPrintBlockManager.AWin1;
            }
            else
            {
                return "";
            }
        }
    }
}
