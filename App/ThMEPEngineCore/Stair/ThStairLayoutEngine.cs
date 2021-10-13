using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Stair;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Stair
{
    public class ThStairLayoutEngine
    {
        public Dictionary<Point3d, double> Layout(Database database, List<Polyline> rooms, Point3dCollection points, double scale, string equimentName, bool platOnly)
        {
            // 提取楼梯块
            var engine = new ThDB3StairRecognitionEngine { Rooms = rooms };
            engine.Recognize(database, points);
            var stairs = engine.Elements.Cast<ThIfcStair>().ToList();

            // 计算布置位置
            return Calculate(stairs, scale, equimentName, platOnly);
        }

        private Dictionary<Point3d, double> Calculate(List<ThIfcStair> stairs, double scale, string equimentName, bool platOnly)
        {
            var dictionary = new Dictionary<Point3d, double>();
            stairs.ForEach(stair =>
            {
                var doorsEngine = new ThStairDoorService();
                var doors = doorsEngine.GetDoorList(stair.SrcBlock);
                var layoutEngine = new ThStairLayoutEngine();
                var angle = 0.0;
                if (equimentName == "E-BFAS110")
                {
                    if (stair.Storey == "顶层")
                    {
                        var dirction = new Vector3d(0, 1, 0);
                        if (stair.StairType == "双跑楼梯")
                        {
                            var position = stair.Rungs.GeometricExtents().CenterPoint();
                            angle = GetAngle(dirction, GetVector(stair.PlatForLayout[0][0], stair.PlatForLayout[0][3]));
                            if (!dictionary.ContainsKey(position))
                            {
                                dictionary.Add(position, angle);
                            }
                        }
                        else if (stair.StairType == "剪刀楼梯")
                        {
                            foreach (Entity rung in stair.Rungs)
                            {
                                var position = rung.GeometricExtents.CenterPoint();
                                angle = GetAngle(dirction, GetVector(stair.PlatForLayout[0][0], stair.PlatForLayout[0][3]));
                                if (!dictionary.ContainsKey(position))
                                {
                                    dictionary.Add(position, angle);
                                }
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        stair.PlatForLayout.ForEach(o =>
                        {
                            var position = layoutEngine.Displacement(o, doors, equimentName, scale, stair.Storey, ref angle);
                            if (!dictionary.ContainsKey(position))
                            {
                                dictionary.Add(position, angle);
                            };
                        });
                    }
                }
                else
                {
                    stair.PlatForLayout.ForEach(o =>
                    {
                        var position = layoutEngine.Displacement(o, doors, equimentName, scale, stair.Storey, ref angle);
                        if (!dictionary.ContainsKey(position))
                        {
                            dictionary.Add(position, angle);
                        }
                    });
                    if (!platOnly)
                    {
                        stair.HalfPlatForLayout.ForEach(o =>
                        {
                            var position = layoutEngine.Displacement(o, doors, equimentName, scale, stair.Storey, ref angle);
                            if (!dictionary.ContainsKey(position))
                            {
                                dictionary.Add(position, angle);
                            }
                        });
                    }
                }
            });
            return dictionary;
        }

        private Point3d Displacement(List<Point3d> platform, List<List<Point3d>> doors, string equimentName,
                                     double scale, string storey, ref double angle)
        {
            // 默认方向
            var dirction = new Vector3d(0, 1, 0);
            // 正常照明点位 平台中心吸顶布置
            var position = new Vector3d();
            if (equimentName == "E-BL302")
            {
                position = (platform[0].GetAsVector() + platform[1].GetAsVector()
                            + platform[2].GetAsVector() + platform[3].GetAsVector()) / platform.Count();
                angle = GetAngle(dirction, GetVector(platform[0], platform[3]));
            }
            // 平台靠近下行方向1/4位置中心吸顶布置
            else if (equimentName == "E-BFEL800")
            {
                position = platform[0].GetAsVector() + (GetVector(platform[0], platform[1]) / 4)
                           + (GetVector(platform[0], platform[3]) / 2);
                angle = GetAngle(dirction, GetVector(platform[0], platform[3]));
            }
            // 平台靠近上行方向1/4位置中心吸顶布置
            else if (equimentName == "E-BFAS110")
            {
                if (storey != "顶层")
                {
                    position = platform[0].GetAsVector() + (GetVector(platform[0], platform[1]) * 3 / 4)
                           + (GetVector(platform[0], platform[3]) / 2);
                    angle = GetAngle(dirction, GetVector(platform[0], platform[3]));
                }
            }
            // 平台中心壁装布置
            else if (equimentName == "E-BFEL110")
            {
                var length = 5.0 * scale;
                var width = 2.5 * scale;
                var layoutLine = LayoutLine(platform, doors, length);
                var positionTidal = ToPoint3d((platform[0].GetAsVector() + platform[1].GetAsVector()) / 2);
                position = Avoid(platform, positionTidal, layoutLine, width, ref angle);
            }
            // 平台靠近下行方向1/4位置壁装布置
            else if (equimentName == "E-BFAS410-4")
            {
                var length = 5.0 * scale;
                var width = 3.0 * scale;
                var layoutLine = LayoutLine(platform, doors, length);
                var positionTidal = ToPoint3d((platform[0].GetAsVector() + GetVector(platform[0], platform[1]) / 4));
                position = Avoid(platform, positionTidal, layoutLine, width, ref angle);
            }

            return ToPoint3d(position);
        }

        private Vector3d GetVector(Point3d starPoint, Point3d endPoint)
        {
            return endPoint.GetAsVector() - starPoint.GetAsVector();
        }

        private Vector3d GetUnitVector(Point3d starPoint, Point3d endPoint)
        {
            return (endPoint.GetAsVector() - starPoint.GetAsVector()) / starPoint.DistanceTo(endPoint);
        }

        /// <summary>
        /// 输出带正负号的旋转角度
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="referenceVector"></param>
        /// <returns></returns>
        private double GetAngle(Vector3d vector, Vector3d referenceVector)
        {
            double angle;
            if (((vector.X * referenceVector.Y) - (vector.Y * referenceVector.X)) > 0)
            {
                angle = vector.GetAngleTo(referenceVector);
            }
            else
            {
                angle = -vector.GetAngleTo(referenceVector);
            }
            if (angle > Math.PI / 2 || angle - ThStairCommon.radian_tolerance <= -Math.PI / 2)
            {
                angle -= Math.PI;
            }
            return angle;
        }

        private Point3d ToPoint3d(Vector3d vector)
        {
            return new Point3d(vector.X, vector.Y, vector.Z);
        }

        private Vector3d ToVector3d(Point3d point)
        {
            return new Vector3d(point.X, point.Y, point.Z);
        }

        /// <summary>
        /// 计算出可布置的线段区域
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="doors"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private List<List<Line>> LayoutLine(List<Point3d> platform, List<List<Point3d>> doors, double length)
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
                if (new Line(platform[0], platform[1]).DistanceTo(doors[i][0], false) < 10.0)
                {
                    layoutArea[0].Add(doors[i][0]);
                    layoutArea[0].Add(doors[i][1]);
                }
                else if (new Line(platform[1], platform[2]).DistanceTo(doors[i][0], false) < 10.0)
                {
                    layoutArea[1].Add(doors[i][0]);
                    layoutArea[1].Add(doors[i][1]);
                }
                else if (new Line(platform[3], platform[0]).DistanceTo(doors[i][0], false) < 10.0)
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
                    if (line.Length > length)
                    {
                        layoutLine[i].Add(NewLine(line, length));
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

        /// <summary>
        /// 计算出可布置的线段
        /// </summary>
        /// <param name="line"></param>
        /// <param name="halfLength"></param>
        /// <returns></returns>
        private Line NewLine(Line line, double length)
        {
            var newStartPoint = ToPoint3d(ToVector3d(line.StartPoint) + (GetUnitVector(line.StartPoint, line.EndPoint) * length / 2));
            var newEndPoint = ToPoint3d(ToVector3d(line.EndPoint) + (GetUnitVector(line.EndPoint, line.StartPoint) * length / 2));
            return new Line(newStartPoint, newEndPoint);
        }

        private Vector3d Avoid(List<Point3d> platform, Point3d positionTidal, List<List<Line>> layoutLine, double width, ref double angle)
        {
            var directionList = new List<Vector3d>
                    {
                        GetUnitVector(platform[0], platform[3]),
                        GetUnitVector(platform[1], platform[0]),
                        GetUnitVector(platform[0], platform[1])
                    };
            var normalVector = new Vector3d();
            var minDistance = -1.0;
            for (int i = 0; i < layoutLine.Count; i++)
            {
                for (int j = 0; j < layoutLine[i].Count; j++)
                {
                    var distance = layoutLine[i][j].DistanceTo(positionTidal, false);
                    if (minDistance < 0.0 || minDistance > distance)
                    {
                        minDistance = distance;
                        positionTidal = layoutLine[i][j].GetClosestPointTo(positionTidal, false);
                        normalVector = directionList[i];
                    }
                }
            }
            angle = GetAngle(new Vector3d(0, 1, 0), normalVector);
            return ToVector3d(positionTidal) + (normalVector * width / 2);
        }
    }
}