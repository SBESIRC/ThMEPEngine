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
                    var widthVector = GetWidthVector(blockReference, ref halfLength);
                    var layoutLine = LayoutLine(platform, doors, halfLength);
                    var positionTidal = ToPoint3d((platform[0].GetAsVector() + platform[1].GetAsVector()) / 2);
                    var newDirection = new Vector3d();
                    Avoid(platform, doors, positionTidal, layoutLine, ref position, ref newDirection, widthVector);
                    var angle = AdjustAngle(widthVector, newDirection);
                    blockReference.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, GetCenterPoint(blockReference)));
                }
                // 平台靠近下行方向1/4位置壁装布置
                else if (name == "E-BFAS410-4")
                {
                    var halfLength = 0.0;
                    var widthVector = GetWidthVector(blockReference, ref halfLength);
                    var layoutLine = LayoutLine(platform, doors, halfLength);
                    var positionTidal = ToPoint3d((platform[0].GetAsVector() + GetVector(platform[0], platform[1]) / 4));
                    var newDirection = new Vector3d();
                    Avoid(platform, doors, positionTidal, layoutLine, ref position, ref newDirection, widthVector);
                    var angle = GetAngle(widthVector, newDirection);
                    blockReference.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, GetCenterPoint(blockReference)));
                }

                var offset = position - targetBlockDataPosition.GetAsVector();
                blockReference.TransformBy(Matrix3d.Displacement(offset));
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

        private double AdjustAngle(Vector3d vector, Vector3d referenceVector)
        {
            var angle = GetAngle(vector, referenceVector);
            if (angle > Math.PI / 2 || angle - 10 * ThStairCommon.radian_tolerance <= -Math.PI / 2)
            {
                angle = angle - Math.PI;
            }
            return angle;
        }

        private double GetAngle(Vector3d vector, Vector3d referenceVector)
        {
            if ((vector.X * referenceVector.Y - vector.Y * referenceVector.X) > 0)
            {
                return vector.GetAngleTo(referenceVector);
            }
            else
            {
                return -vector.GetAngleTo(referenceVector);
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

        private Point3d ToPoint3d(Vector3d vector)
        {
            return new Point3d(vector.X, vector.Y, vector.Z);
        }

        private Vector3d ToVector3d(Point3d point)
        {
            return new Vector3d(point.X, point.Y, point.Z);
        }

        private List<List<Line>> LayoutLine(List<Point3d> platform, List<List<Point3d>> doors, double halfLength)
        {
            var layoutArea = new List<List<Point3d>>();
            for (int i = 0; i < 3; i++)
            {
                layoutArea.Add(new List<Point3d>());
            }
            layoutArea[0].Add(platform[0]);
            layoutArea[0].Add(platform[1]);
            layoutArea[1].Add(platform[1]);
            layoutArea[1].Add(platform[2]);
            layoutArea[2].Add(platform[3]);
            layoutArea[2].Add(platform[0]);
            for (int i = 0; i < doors.Count; i++)
            {
                if (new Line(platform[0], platform[1]).DistanceTo(doors[i][0], false) < 10)
                {
                    layoutArea[0].Add(doors[i][0]);
                    layoutArea[0].Add(doors[i][1]);
                }
                else if (new Line(platform[1], platform[2]).DistanceTo(doors[i][0], false) < 10)
                {
                    layoutArea[1].Add(doors[i][0]);
                    layoutArea[1].Add(doors[i][1]);
                }
                else if (new Line(platform[3], platform[0]).DistanceTo(doors[i][0], false) < 10)
                {
                    layoutArea[2].Add(doors[i][0]);
                    layoutArea[2].Add(doors[i][1]);
                }
            }
            Sort(layoutArea);
            var layoutLine = new List<List<Line>>();
            for (int i = 0; i < layoutArea.Count; i++)
            {
                layoutLine.Add(new List<Line>());
                for (int j = 1; j < layoutArea[i].Count; j += 2)
                {
                    var line = new Line(layoutArea[i][j - 1], layoutArea[i][j]);
                    if (line.Length > 2 * halfLength)
                    {
                        layoutLine[i].Add(NewLine(line, halfLength));
                    }
                }
            }
            return layoutLine;
        }

        private void Sort(List<List<Point3d>> layoutArea)
        {
            for (int i = 0; i < layoutArea.Count; i++)
            {
                for (int j = 1; j < layoutArea[i].Count; j++)
                {
                    for (int k = j + 1; k < layoutArea[i].Count; k++)
                    {
                        if (layoutArea[i][j].DistanceTo(layoutArea[i][0]) > layoutArea[i][k].DistanceTo(layoutArea[i][0]))
                        {
                            var temp = layoutArea[i][j];
                            layoutArea[i][j] = layoutArea[i][k];
                            layoutArea[i][k] = temp;
                        }
                    }
                }
            }
        }

        private Line NewLine(Line line, double halfLength)
        {
            var newStartPoint = ToPoint3d(ToVector3d(line.StartPoint) + GetUnitVector(line.StartPoint, line.EndPoint) * halfLength);
            var newEndPoint = ToPoint3d(ToVector3d(line.EndPoint) + GetUnitVector(line.EndPoint, line.StartPoint) * halfLength);
            return new Line(newStartPoint, newEndPoint);
        }

        private void Avoid(List<Point3d> platform, List<List<Point3d>> doors, Point3d positionTidal, List<List<Line>> layoutLine, ref Vector3d position, ref Vector3d newDirection, Vector3d widthVector)
        {
            var directionList = new List<Vector3d>
                    {
                        GetUnitVector(platform[0], platform[3]),
                        GetUnitVector(platform[1], platform[0]),
                        GetUnitVector(platform[0], platform[1])
                    };
            var minDistance = -1.0;
            var newPositionTidal = positionTidal;
            newDirection = directionList[0];
            for (int i = 0; i < layoutLine.Count; i++)
            {
                for (int j = 0; j < layoutLine[i].Count; j++)
                {
                    var distance = layoutLine[i][j].DistanceTo(positionTidal, false);
                    if (minDistance > distance || minDistance < 0.0)
                    {
                        minDistance = distance;
                        newPositionTidal = layoutLine[i][j].GetClosestPointTo(positionTidal, false);
                        newDirection = directionList[i];
                    }
                }
            }
            position = ToVector3d(newPositionTidal) + newDirection * widthVector.Length / 2;
        }
    }
}
