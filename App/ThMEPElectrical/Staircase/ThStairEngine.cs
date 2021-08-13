using System;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPElectrical.Stair
{
    public class ThStairEngine : IDisposable
    {
        public void Dispose()
        {
            //
        }

        public ObjectId Insert(string name, Scale3d scale)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    "0",
                    name,
                    Point3d.Origin,
                    scale,
                    0.0);
            }
        }

        public void Displacement(ObjectId blkRef, List<Point3d> platform, List<List<Point3d>> doors)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blkRef.Database))
            {
                var blockReference = acadDatabase.Element<BlockReference>(blkRef, true);
                var targetBlockData = new ThBlockReferenceData(blkRef);
                var targetMCS2WCS = targetBlockData.BlockTransform.PreMultiplyBy(targetBlockData.OwnerSpace2WCS);
                var targetBlockDataPosition = Point3d.Origin.TransformBy(targetMCS2WCS);

                // 平台中心吸顶布置
                var position = new Vector3d();
                var name = targetBlockData.EffectiveName;
                if (name == "E-BL302")
                {
                    position = (platform[0].GetAsVector() + platform[1].GetAsVector()
                                + platform[2].GetAsVector() + platform[3].GetAsVector()) / platform.Count();
                }
                // 平台靠近下行方向1/4位置中心吸顶布置
                else if (name == "E-BFEL800")
                {
                    position = platform[0].GetAsVector() + GetVector(platform[0], platform[1]) / 4
                               + GetVector(platform[0], platform[3]) / 2;
                }
                // 平台靠近上行方向1/4位置中心吸顶布置
                else if (name == "E-BFAS110")
                {
                    position = platform[0].GetAsVector() + GetVector(platform[0], platform[1]) * 3 / 4
                               + GetVector(platform[0], platform[3]) / 2;
                }
                // 平台中心壁装布置
                else if (name == "E-BFEL110")
                {
                    var halfLength = 0.0;
                    var angle = 0.0;
                    var widthVector = GetWidthVector(blockReference, ref halfLength);
                    var positionTidal = ToPoint3d((platform[0].GetAsVector() + platform[1].GetAsVector()) / 2);
                    position = ToVector3d(positionTidal) + GetUnitVector(platform[0], platform[3]) * widthVector.Length / 2;
                    Avoid(blockReference, platform, doors, position, positionTidal, ref angle);
                    angle = GetAngle(widthVector, GetVector(platform[0], platform[3]), angle);
                    blockReference.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, GetCenterPoint(blockReference)));
                }
                // 平台靠近下行方向1/4位置壁装布置
                else if (name == "E-BFAS410-4")
                {
                    var halfLength = 0.0;
                    var angle = 0.0;
                    var widthVector = GetWidthVector(blockReference, ref halfLength);
                    var positionTidal = ToPoint3d((platform[0].GetAsVector() + GetVector(platform[0], platform[1]) / 4));
                    position = ToVector3d(positionTidal) + GetUnitVector(platform[0], platform[3]) * widthVector.Length / 2;
                    Avoid(blockReference, platform, doors, position, positionTidal, ref angle);
                    angle = GetClockwise(widthVector, GetVector(platform[0], platform[3]), angle);
                    blockReference.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, GetCenterPoint(blockReference)));
                }

                var offset = position - targetBlockDataPosition.GetAsVector();
                blockReference.TransformBy(Matrix3d.Displacement(offset));
            }
        }

        private void Avoid(BlockReference blockReference, List<Point3d> platform, List<List<Point3d>> doors, Vector3d position, Point3d positionTidal, ref double angle)
        {
            var halfLength = 0.0;
            var widthVector = GetWidthVector(blockReference, ref halfLength);
            for (int i = 0; i < doors.Count(); i++)
            {
                // 若位置点位于门附近
                var line = new Line(doors[i][0], doors[i][1]);
                if (line.DistanceTo(positionTidal, false) < halfLength)
                {
                    var newDoor = DoorExtend(doors[i], halfLength, platform);
                    if (newDoor.Count == 0)
                    {
                        var adjustPoint0 = ToPoint3d(ToVector3d(platform[0]) + GetUnitVector(platform[0], platform[3]) * halfLength);
                        var adjustPoint1 = ToPoint3d(ToVector3d(platform[1]) + GetUnitVector(platform[1], platform[2]) * halfLength);
                        if (positionTidal.DistanceTo(adjustPoint0) < positionTidal.DistanceTo(adjustPoint1))
                        {
                            positionTidal = adjustPoint0;
                            position = ToVector3d(positionTidal) + GetUnitVector(platform[0], platform[1]) * widthVector.Length / 2;
                            angle = Math.PI / 2;
                        }
                        else
                        {
                            positionTidal = adjustPoint1;
                            position = ToVector3d(positionTidal) + GetUnitVector(platform[1], platform[0]) * widthVector.Length / 2;
                            angle = -Math.PI / 2;
                        }
                    }
                    else if (newDoor.Count == 1)
                    {
                        positionTidal = newDoor[0];
                        position = ToVector3d(positionTidal) + GetUnitVector(platform[0], platform[3]) * widthVector.Length / 2;
                    }
                    else
                    {
                        positionTidal = newDoor[0].DistanceTo(positionTidal) < newDoor[1].DistanceTo(positionTidal)
                                        ? newDoor[0] : newDoor[1];
                        position = ToVector3d(positionTidal) + GetUnitVector(platform[0], platform[3]) * widthVector.Length / 2;
                    }
                }
            }
        }

        private Vector3d GetVector(Point3d starPoint, Point3d endPoint)
        {
            return (endPoint.GetAsVector() - starPoint.GetAsVector());
        }

        private Vector3d GetUnitVector(Point3d starPoint, Point3d endPoint)
        {
            return (endPoint.GetAsVector() - starPoint.GetAsVector()) / (starPoint.DistanceTo(endPoint));
        }

        private double GetAngle(Vector3d vector, Vector3d referenceVector, double adjust)
        {
            var angle = GetClockwise(vector, referenceVector, adjust);
            if (angle > Math.PI / 2 || angle - 10 * ThStairCommon.radian_tolerance <=- Math.PI / 2)
            {
                angle = angle - Math.PI;
            }
            return angle;
        }

        private double GetClockwise(Vector3d vector, Vector3d referenceVector, double adjust)
        {
            var angle = adjust;
            if ((vector.X * referenceVector.Y - vector.Y * referenceVector.X) > 0)
            {
                return vector.GetAngleTo(referenceVector) + angle;
            }
            else
            {
                return -vector.GetAngleTo(referenceVector) + angle;
            }
        }

        private Point3d GetCenterPoint(BlockReference blockReference)
        {
            var entities = new DBObjectCollection();
            blockReference.Explode(entities);
            entities = entities.Cast<Entity>()
                        .Where(e => e is Curve)
                        .ToCollection();
            return entities.GeometricExtents().CenterPoint();
        }

        private Vector3d GetWidthVector(BlockReference blockReference, ref double halfLength)
        {
            var polyline = blockReference.ToOBB(blockReference.BlockTransform);
            var side_a = polyline.Vertices()[0].DistanceTo(polyline.Vertices()[1]);
            var side_b = polyline.Vertices()[1].DistanceTo(polyline.Vertices()[2]);
            if (side_a < side_b)
            {
                halfLength = side_b / 2;
                return GetVector(polyline.Vertices()[0], polyline.Vertices()[1]);
            }
            else
            {
                halfLength = side_a / 2;
                return GetVector(polyline.Vertices()[1], polyline.Vertices()[2]);
            }
        }

        //private bool JudgePosition(Point3d point, List<Point3d> platform, List<List<Point3d>> doors, double halfLength, ref Point3d newPoint)
        //{
        //    for (int i = 0; i < platform.Count(); i++)
        //    {
        //        if (point.DistanceTo(platform[i]) < halfLength)
        //        {

        //            return false;
        //        }
        //    }
        //    if (new Line(platform[2], platform[3]).Distance(point) < 10)
        //    {
        //        return false;
        //    }
        //    for (int i = 0; i < doors.Count(); i++)
        //    {
        //        if (new Line(doors[i][0], doors[i][1]).Distance(point) < halfLength)
        //        {
        //            return false;
        //        }
        //    }
        //    return true;
        //}

        private Point3d ToPoint3d(Vector3d vector)
        {
            return new Point3d(vector.X, vector.Y, vector.Z);
        }

        private Vector3d ToVector3d(Point3d point)
        {
            return new Vector3d(point.X, point.Y, point.Z);
        }

        // 返回位置点是否位于门的halfLength范围附近，位于则返回true
        //private bool JudgePositionInDoor(Point3d point,List<Point3d> doorExtend, ref Point3d newPoint)
        //{
        //    newPoint = doorExtend[0].DistanceTo(point) < doorExtend[1].DistanceTo(point) ? doorExtend[0] : doorExtend[1];

        //    return (new Line(door[0], door[1]).Distance(point) < halfLength);
        //}

        private List<Point3d> DoorExtend(List<Point3d> door, double halfLength, List<Point3d> platform)
        {
            var doorExtend = new List<Point3d>();
            var unitVector = GetUnitVector(door[0], door[1]);
            var firstPoint = ToPoint3d((ToVector3d(door[0]) - unitVector * halfLength));
            var secondPoint = ToPoint3d((ToVector3d(door[1]) + unitVector * halfLength));
            if (Valid(firstPoint, halfLength, platform))
            {
                doorExtend.Add(firstPoint);
            }
            if (Valid(secondPoint, halfLength, platform))
            {
                doorExtend.Add(secondPoint);
            }
            return doorExtend;
        }

        private bool Valid(Point3d position, double halfLength, List<Point3d> platform)
        {
            var minDistance = 2 * halfLength;
            foreach (var point in platform)
            {
                var distance = position.DistanceTo(point);
                if (minDistance > distance)
                {
                    minDistance = distance;
                }
            }
            if (minDistance > halfLength)
            {
                return true;
            }
            return false;
        }
    }
}
