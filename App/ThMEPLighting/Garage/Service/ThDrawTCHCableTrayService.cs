using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPTCH.Model;
using ThMEPEngineCore.CAD;
using ThMEPTCH.TCHDrawServices;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThDrawTCHCableTrayService
    {
        public double Width { get; set; } = 150.0;
        public double Height { get; set; } = 75.0;

        public CableTraySystem System => CableTraySystem.CABLETRAY_LIGH;

        private Vector3d Normal = new Vector3d(0, 0, 1);

        // 输入在连接点处打断的桥架
        // 需要在近似原点附近处理
        public void Draw(List<Line> cableTrays, ThMEPOriginTransformer transformer)
        {
            var service = new TCHDrawCableTrayService();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(cableTrays.ToCollection());
            var elbowInputs = new List<ElbowInput>();
            var teeInputs = new List<TeeInput>();
            var crossInputs = new List<CrossInput>();
            cableTrays.ForEach(o =>
            {
                var startResult = EndPointSearch(o, true, spatialIndex, elbowInputs, teeInputs, crossInputs);
                EndPointSearch(startResult, false, spatialIndex, elbowInputs, teeInputs, crossInputs);
            });

            DrawCableTray(service.CableTrays, spatialIndex.SelectAll().OfType<Line>().ToList(), transformer);
            DrawElbow(service.Elbows, elbowInputs, transformer);
            DrawTee(service.Tees, teeInputs, transformer);
            DrawCross(service.Crosses, crossInputs, transformer);
            service.DrawExecute(true, false);
        }

        private Line EndPointSearch(Line o, bool isStartPoint, ThCADCoreNTSSpatialIndex spatialIndex, List<ElbowInput> elbowInputs,
            List<TeeInput> teeInputs, List<CrossInput> crossInputs)
        {
            var cableTrayCopy = o;
            var startPoint = isStartPoint ? o.StartPoint : o.EndPoint;
            var endPoint = isStartPoint ? o.EndPoint : o.StartPoint;
            var direction = isStartPoint ? o.LineDirection() : -o.LineDirection();
            var startFrame = startPoint.CreateSquare(10.0);
            var startFilter = spatialIndex.SelectCrossingPolygon(startFrame).OfType<Line>().ToList();
            if (!startFilter.Contains(o))
            {
                startFilter.ForEach(l =>
                {
                    if (o.DistanceTo(l.StartPoint, false) < 1.0 && o.DistanceTo(l.EndPoint, false) < 1.0)
                    {
                        o = l;
                        startPoint = isStartPoint ? o.StartPoint : o.EndPoint;
                        endPoint = isStartPoint ? o.EndPoint : o.StartPoint;
                    }
                });
            }
            var startFilterExcept = startFilter.Count > 1 ? startFilter.Except(new List<Line> { o }).ToList() : new List<Line>();
            switch (startFilterExcept.Count)
            {
                case 1:
                    var secondInfo = GetDirectionAndReduces(startPoint, startFilterExcept[0]);
                    var elbowInput = new ElbowInput
                    {
                        IntersectPoint = startPoint,
                        FirstDirection = direction,
                        SecondDirection = secondInfo.Item1,
                    };
                    elbowInputs.Add(elbowInput);
                    cableTrayCopy = isStartPoint ? new Line(o.StartPoint + 1.5 * Width * direction, endPoint)
                        : new Line(o.StartPoint, o.EndPoint + 1.5 * Width * direction);
                    var elbowAddLines = new List<Line>
                    {
                        cableTrayCopy,
                        secondInfo.Item2,
                    };
                    spatialIndex.Update(elbowAddLines.ToCollection(), startFilter.ToCollection());
                    break;
                case 2:
                    var teeTuple = CalculateDirection(startPoint, o, startFilterExcept[0], startFilterExcept[1]);
                    var teeInput = new TeeInput
                    {
                        IntersectPoint = startPoint,
                        FirstDirection = teeTuple.Item1,
                        SecondDirection = teeTuple.Item2,
                        ThirdDirection = teeTuple.Item3,
                    };
                    teeInputs.Add(teeInput);
                    cableTrayCopy = isStartPoint ? new Line(o.StartPoint + 1.5 * Width * direction, endPoint)
                        : new Line(o.StartPoint, o.EndPoint + 1.5 * Width * direction);
                    var teeAddLines = new List<Line>
                    {
                        cableTrayCopy,
                        GetDirectionAndReduces(startPoint, startFilterExcept[0]).Item2,
                        GetDirectionAndReduces(startPoint, startFilterExcept[1]).Item2,
                    };
                    spatialIndex.Update(teeAddLines.ToCollection(), startFilter.ToCollection());
                    break;
                case 3:
                    var crossTuple = CalculateDirection(startPoint, o, startFilterExcept[0], startFilterExcept[1], startFilterExcept[2]);
                    var crossInput = new CrossInput
                    {
                        IntersectPoint = startPoint,
                        FirstDirection = crossTuple.Item1,
                        SecondDirection = crossTuple.Item2,
                        ThirdDirection = crossTuple.Item3,
                        ForthDirection = crossTuple.Item4,
                    };
                    crossInputs.Add(crossInput);
                    cableTrayCopy = isStartPoint ? new Line(o.StartPoint + 1.5 * Width * direction, endPoint)
                        : new Line(o.StartPoint, o.EndPoint + 1.5 * Width * direction);
                    var crossAddLines = new List<Line>
                    {
                        cableTrayCopy,
                        GetDirectionAndReduces(startPoint, startFilterExcept[0]).Item2,
                        GetDirectionAndReduces(startPoint, startFilterExcept[1]).Item2,
                        GetDirectionAndReduces(startPoint, startFilterExcept[2]).Item2,
                    };
                    spatialIndex.Update(crossAddLines.ToCollection(), startFilter.ToCollection());
                    break;
            }
            return cableTrayCopy;
        }

        private void DrawCableTray(List<ThTCHCableTray> tchCableTrays, List<Line> cableTrays, ThMEPOriginTransformer transformer)
        {
            foreach (var cableTray in cableTrays)
            {
                var telecObject = new ThTCHTelecObject
                {
                    Type = TelecObjectType.CableTray,
                };
                var startInterface = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(cableTray.StartPoint),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = new Vector3d(1, 0, 0),
                };
                var endInterface = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(cableTray.EndPoint),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = new Vector3d(1, 0, 0),
                };
                var clapboard = new ThTCHTelecClapboard
                {
                    HaveClapboard = false,
                };
                var tchCableTray = new ThTCHCableTray
                {
                    ObjectId = telecObject,
                    Type = "C-01-10-3",
                    Style = CableTrayStyle.Trough,
                    CableTraySystem = System,
                    Height = Height,
                    Cover = false,
                    Clapboard = clapboard,
                    StartInterface = startInterface,
                    EndInterface = endInterface,
                };

                tchCableTrays.Add(tchCableTray);
            }
        }

        private void DrawElbow(List<ThTCHElbow> tchElbows, List<ElbowInput> elbowInputs, ThMEPOriginTransformer transformer)
        {
            foreach (var elbowInput in elbowInputs)
            {
                var telecObject2 = new ThTCHTelecObject
                {
                    Type = TelecObjectType.Elbow,
                };
                var clapboard2 = new ThTCHTelecClapboard
                {
                    HaveClapboard = false,
                };
                var startInterface2 = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(elbowInput.IntersectPoint + Width * 1.5 * elbowInput.FirstDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = -elbowInput.FirstDirection,
                };
                var endInterface2 = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(elbowInput.IntersectPoint + Width * 1.5 * elbowInput.SecondDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = elbowInput.SecondDirection,
                };
                var tchElbow = new ThTCHElbow
                {
                    ObjectId = telecObject2,
                    Type = "C-02A",
                    ElbowStyle = "1",
                    Style = CableTrayStyle.Trough,
                    CableTraySystem = System,
                    Height = Height,
                    Length = 2 * Width,
                    Cover = false,
                    Clapboard = clapboard2,
                    MidPosition = transformer.Reset(elbowInput.IntersectPoint),
                    MajInterfaceId = startInterface2,
                    MinInterfaceId = endInterface2,
                };

                tchElbows.Add(tchElbow);
            }
        }

        private void DrawTee(List<ThTCHTee> tchTees, List<TeeInput> teeInputs, ThMEPOriginTransformer transformer)
        {
            foreach (var teeInput in teeInputs)
            {
                var telecObject = new ThTCHTelecObject
                {
                    Type = TelecObjectType.Tee,
                };
                var clapboard = new ThTCHTelecClapboard
                {
                    HaveClapboard = false,
                };
                var startInterface = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(teeInput.IntersectPoint + Width * 1.5 * teeInput.FirstDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = -teeInput.FirstDirection,
                };
                var endInterface = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(teeInput.IntersectPoint + Width * 1.5 * teeInput.SecondDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = teeInput.SecondDirection,
                };
                var endInterface2 = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(teeInput.IntersectPoint + Width * 1.5 * teeInput.ThirdDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = teeInput.ThirdDirection,
                };
                var tchElbow = new ThTCHTee
                {
                    ObjectId = telecObject,
                    Type = "C-02A",
                    TeeStyle = "1",
                    Style = CableTrayStyle.Trough,
                    CableTraySystem = System,
                    Height = Height,
                    Length = 3 * Width,
                    Length2 = 2 * Width,
                    Cover = false,
                    Clapboard = clapboard,
                    MidPosition = transformer.Reset(teeInput.IntersectPoint),
                    MajInterfaceId = startInterface,
                    MinInterfaceId = endInterface,
                    Min2InterfaceId = endInterface2,
                };

                tchTees.Add(tchElbow);
            }
        }

        private void DrawCross(List<ThTCHCross> tchCrosses, List<CrossInput> crossInputs, ThMEPOriginTransformer transformer)
        {
            foreach (var crossInput in crossInputs)
            {
                var telecObject = new ThTCHTelecObject
                {
                    Type = TelecObjectType.Cross,
                };
                var clapboard = new ThTCHTelecClapboard
                {
                    HaveClapboard = false,
                };
                var startInterface = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(crossInput.IntersectPoint + Width * 1.5 * crossInput.FirstDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = -crossInput.FirstDirection,
                };
                var endInterface = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(crossInput.IntersectPoint + Width * 1.5 * crossInput.SecondDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = crossInput.SecondDirection,
                };
                var endInterface2 = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(crossInput.IntersectPoint + Width * 1.5 * crossInput.ThirdDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = crossInput.ThirdDirection,
                };
                var endInterface3 = new ThTCHTelecInterface
                {
                    Position = transformer.Reset(crossInput.IntersectPoint + Width * 1.5 * crossInput.ForthDirection),
                    Breadth = Width,
                    Normal = Normal,
                    Direction = crossInput.ForthDirection,
                };
                var tchCross = new ThTCHCross
                {
                    ObjectId = telecObject,
                    Type = "C-02A",
                    CrossStyle = "1",
                    Style = CableTrayStyle.Trough,
                    CableTraySystem = System,
                    Height = Height,
                    Length = 3 * Width,
                    Cover = false,
                    Clapboard = clapboard,
                    MidPosition = transformer.Reset(crossInput.IntersectPoint),
                    InclineFit = false,
                    MajInterfaceId = startInterface,
                    MinInterfaceId = endInterface,
                    Min2InterfaceId = endInterface2,
                    Min3InterfaceId = endInterface3,
                };

                tchCrosses.Add(tchCross);
            }
        }

        /// <summary>
        /// 计算相交点指向直线上最远端点的单位向量
        /// </summary>
        /// <param name="intersectPoint"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private Tuple<Vector3d, Line> GetDirectionAndReduces(Point3d intersectPoint, Line line)
        {
            var direction = line.LineDirection();
            // 相交点为线的近似起点
            if (intersectPoint.DistanceTo(line.StartPoint) < intersectPoint.DistanceTo(line.EndPoint))
            {
                return Tuple.Create(direction, new Line(intersectPoint + 1.5 * Width * direction, line.EndPoint));
            }
            else
            {
                return Tuple.Create(-direction, new Line(line.StartPoint, line.EndPoint - 1.5 * Width * direction));
            }
        }

        private Vector3d GetDirection(Point3d intersectPoint, Line line)
        {
            // 相交点为线的近似起点
            if (intersectPoint.DistanceTo(line.StartPoint) < intersectPoint.DistanceTo(line.EndPoint))
            {
                return line.LineDirection();
            }
            else
            {
                return -line.LineDirection();
            }
        }

        private Tuple<Vector3d, Vector3d, Vector3d> CalculateDirection(Point3d intersectPoint, Line first, Line second, Line third)
        {
            var firstDirection = GetDirection(intersectPoint, first);
            var secondDirection = GetDirection(intersectPoint, second);
            var thirdDirection = GetDirection(intersectPoint, third);
            if (Math.Abs(firstDirection.DotProduct(secondDirection)) > Math.Cos(1 / 180.0 * Math.PI))
            {
                return Tuple.Create(firstDirection, thirdDirection, secondDirection);
            }
            else if (Math.Abs(firstDirection.DotProduct(thirdDirection)) > Math.Cos(1 / 180.0 * Math.PI))
            {
                return Tuple.Create(firstDirection, secondDirection, thirdDirection);
            }
            else
            {
                return Tuple.Create(secondDirection, firstDirection, thirdDirection);
            }
        }

        private Tuple<Vector3d, Vector3d, Vector3d, Vector3d> CalculateDirection(Point3d intersectPoint, Line first, Line second, Line third, Line forth)
        {
            var firstDirection = GetDirection(intersectPoint, first);
            var secondDirection = GetDirection(intersectPoint, second);
            var thirdDirection = GetDirection(intersectPoint, third);
            var forthDirection = GetDirection(intersectPoint, forth);
            if (Math.Abs(firstDirection.DotProduct(secondDirection)) > Math.Cos(1 / 180.0 * Math.PI))
            {
                return Tuple.Create(firstDirection, thirdDirection, secondDirection, forthDirection);
            }
            else
            {
                return Tuple.Create(firstDirection, secondDirection, thirdDirection, forthDirection);
            }
        }
    }
}
