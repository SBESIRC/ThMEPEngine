using Linq2Acad;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using ThPlatform3D.ArchitecturePlane.Service;

namespace ThPlatform3D.ArchitecturePlane.Print
{
    internal class ThTchDoorBlkPrinter : ThDoorBlkPrinter
    {
        public ThTchDoorBlkPrinter()
        {
        }

        public override ObjectIdCollection Print(Database db,List<ThComponentInfo> doors, double scale = 1.0)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var results = new ObjectIdCollection();
                doors.ForEach(o =>
                {
                    var blkName = GetDoorBlkName(o.BlockName);
                    results.Add(InsertDoor(acadDb, o, blkName, scale));
                });
                return results;
            }
        }
        
        private ObjectId InsertDoor(AcadDatabase acadDb, ThComponentInfo component, string blkName, double scale)
        {
            var startPt = component.Start.ToPoint3d();
            var endPt = component.End.ToPoint3d();
            if (!startPt.HasValue || !endPt.HasValue)
            {
                return ObjectId.Null;
            }

            // 插块
            var doorLength = startPt.Value.DistanceTo(endPt.Value);
            double insertLength = GetDoorLength(doorLength, blkName);
            var blkId = InsertBlock(acadDb, blkName, DoorLayer, scale);
            var entity = acadDb.Element<BlockReference>(blkId, true);
            ModifyDoorLength(entity, insertLength);

            // 把块的中心移动原点
            var mt1 = Matrix3d.Displacement(new Vector3d(-1.0 * doorLength / 2.0, 0, 0));
            entity.TransformBy(mt1);

            // 调整门位置
            if (component.OpenDirection == "0")
            {
                var mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
                entity.TransformBy(mirror);
            }
            else if (component.OpenDirection == "2")
            {
                var mirror = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(1, 0, 0)));
                entity.TransformBy(mirror);
            }
            else if (component.OpenDirection == "3")
            {
                var mirror1 = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
                entity.TransformBy(mirror1);
                var mirror2 = Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(1, 0, 0)));
                entity.TransformBy(mirror2);
            }

            // 旋转
            var rotateRad = GetRotation(component.Rotation);
            var mt2 = Matrix3d.Rotation(rotateRad.HasValue ? rotateRad.Value : GetRotation(startPt.Value, endPt.Value),
                Vector3d.ZAxis, Point3d.Origin);
            entity.TransformBy(mt2);

            // 偏移
            var midPt = startPt.Value.GetMidPt(endPt.Value);
            var mt3 = Matrix3d.Displacement(midPt - Point3d.Origin);
            entity.TransformBy(mt3);

            return blkId;
        }

        private  string GetDoorBlkName(string blkName)
        {
            return ThTCHBlockMapConfig.GetDoorBlkName(blkName);
        }
    }
}
