using System;
using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThStraitLinkCreator
    {
        #region --------- input ---------
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private Dictionary<string, int> DirectionConfig { get; set; }
        private Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; set; }
        #endregion
        public double AngleTolerance { get; set; } = 10.0;
        protected double OffsetDis2 { get; set; }
        protected double CornerAngle { get; set; } = 30.0;
        public ThStraitLinkCreator(
            ThLightArrangeParameter arrangeParameter,
            Dictionary<string, int> directionConfig,
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            ArrangeParameter = arrangeParameter;
            DirectionConfig = directionConfig;
            CenterSideDicts = centerSideDicts;
            OffsetDis2 = (ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0) * 3.0;
        }

        public List<ThLightNodeLink> CreateStraitLinkJumpWire(List<ThLightEdge> edges, List<string> defaultNumbers)
        {
            //绘制十字路口跨区具有相同编号的的跳线
            var linker = new ThCrossLightNodeLinker(edges, ArrangeParameter.DoubleRowOffsetDis);
            linker.Link();
            if (ArrangeParameter.ConnectMode == ConnectMode.Linear)
            {
                CreateCrossLinearLinkWire(linker.LightLinks, defaultNumbers, OffsetDis2);
            }
            else
            {
                CreateCrossCircularLinkWire(linker.LightLinks, defaultNumbers);
            }
            return linker.LightLinks;
        }

        public List<ThLightNodeLink> CreateStraitLinkJumpWire(List<ThLightNodeLink> lightLinks, List<string> defaultNumbers)
        {
            if (ArrangeParameter.ConnectMode == ConnectMode.Linear)
            {
                CreateCrossLinearLinkWire(lightLinks, defaultNumbers, OffsetDis2);
            }
            else
            {
                CreateCrossCircularLinkWire(lightLinks, defaultNumbers);
            }
            return lightLinks;
        }

        public void CreateWireForStraitLink(List<ThLightNodeLink> links)
        {
            // 直线连接
            var linearLinks = links.Where(o => IsSuiteStraitLink(o, AngleTolerance)).ToList();
            CreateLinearStraitLink(linearLinks);
            var circularLinks = links.Where(o => !linearLinks.Contains(o)).ToList();
            CreateCircularArcStraitLink(circularLinks);
        }

        public List<ThLightNodeLink> CreateElbowStraitLinkJumpWire(List<ThLightEdge> edges)
        {
            var results = new DBObjectCollection();
            var lightNodeLinks = GetElbowStraitLinks(edges);
            CreateWireForStraitLink(lightNodeLinks);
            return lightNodeLinks;
        }

        public List<ThLightNodeLink> CreateThreeWayStraitLinksJumpWire(List<ThLightEdge> edges)
        {
            var lightNodeLinks = GetThreeWayStraitLinks(edges);
            CreateWireForStraitLink(lightNodeLinks);
            return lightNodeLinks;
        }

        public List<ThLightNodeLink> CreateCrossCornerStraitLinkJumpWire(List<ThLightEdge> edges)
        {
            //绘制十字路口跨区具有相同编号的的跳线
            var lightNodeLinks = GetCrossCornerStraitLinks(edges);
            CreateWireForStraitLink(lightNodeLinks);
            return lightNodeLinks;
        }

        private void CreateCircularArcStraitLink(List<ThLightNodeLink> lightNodeLinks)
        {
            if (lightNodeLinks.Count > 0)
            {
                var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
                {
                    CenterSideDicts = this.CenterSideDicts,
                    DirectionConfig = this.DirectionConfig,
                    LampLength = this.ArrangeParameter.LampLength,
                    LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                    Gap = this.ArrangeParameter.CircularArcTopDistanceToDxLine * 2,
                };
                jumpWireFactory.BuildStraitLinks();
            }
        }
        private List<ThLightNodeLink> GetElbowStraitLinks(List<ThLightEdge> edges)
        {
            // 创建弯头跨区跳接线
            if (CenterSideDicts.Count > 0)
            {
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkElbow(); // 连接T型拐角处
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        private List<ThLightNodeLink> GetCrossCornerStraitLinks(List<ThLightEdge> edges)
        {
            // 创建十字路口同一域具有相同1、2线的跳接线
            if (CenterSideDicts.Count > 0)
            {
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkCross();
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        private List<ThLightNodeLink> GetThreeWayStraitLinks(List<ThLightEdge> edges)
        {
            // 创建T型路口跳接线
            if (CenterSideDicts.Count > 0)
            {
                var crossLinker = new ThLightNodeCrossLinkService(edges, CenterSideDicts);
                return crossLinker.LinkThreeWay(); // 连接T型拐角处
            }
            else
            {
                return new List<ThLightNodeLink>();
            }
        }
        private bool IsSuiteStraitLink(ThLightNodeLink link, double angTolerance)
        {
            var direction = link.First.Position.GetVectorTo(link.Second.Position).GetNormal();
            return !link.Edges.Where(o =>
            {
                var ang = direction.GetAngleTo(o.LineDirection()).RadToAng();
                return ang <= angTolerance || (180.0 - ang) <= angTolerance;
            }).Any();
        }

        private void CreateCrossLinearLinkWire(List<ThLightNodeLink> crossLinks, List<string> defaultNumbers, double jumpWireHeight)
        {
            crossLinks.ForEach(link =>
            {
                var firstEdge = FindEdge(link.First.Position, link.Edges);
                var secondEdge = FindEdge(link.Second.Position, link.Edges);
                if (firstEdge != null && secondEdge != null)
                {
                    // 共线，非共线
                    if (ThGarageUtils.IsLessThan45Degree(firstEdge, secondEdge))
                    {
                        var direction = GetDefaultDirection(firstEdge, secondEdge);
                        link.JumpWires = CreateLinearJumpLink(link.First.Position, link.Second.Position, direction, jumpWireHeight);
                    }
                    else
                    {
                        // 共线，非共线                            
                        link.JumpWires = CreateLinearStraitLink(firstEdge, link.First.Position, secondEdge, link.Second.Position);
                    }
                }
            });
        }

        private void CreateCrossCircularLinkWire(List<ThLightNodeLink> crossLinks, List<string> defaultNumbers)
        {
            crossLinks.ForEach(link =>
            {
                var firstEdge = FindEdge(link.First.Position, link.Edges);
                var secondEdge = FindEdge(link.Second.Position, link.Edges);
                if (firstEdge != null && secondEdge != null)
                {
                    link.JumpWires = CreateCircularJumpLink(firstEdge, link.First.Position, secondEdge, link.Second.Position);
                }
            });
        }

        private Line FindEdge(Point3d position, List<Line> lines)
        {
            var results = lines.Where(o => position.IsPointOnLine(o, 1.0));
            return results.Count() > 0 ? results.First() : null;
        }
        private void BuildCircularArcSameLink(List<ThLightNodeLink> lightNodeLinks, List<string> defaultNumbers)
        {
            var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
            {
                DefaultNumbers = defaultNumbers,
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                Gap = this.ArrangeParameter.CircularArcTopDistanceToDxLine,
            };
            jumpWireFactory.Build();
        }
        private void BuildLinearSameLink(List<ThLightNodeLink> lightNodeLinks, List<string> defaultNumbers)
        {
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                DefaultNumbers = defaultNumbers,
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.Build();
        }
        private void CreateLinearStraitLink(List<ThLightNodeLink> lightNodeLinks)
        {
            lightNodeLinks.ForEach(link => link.JumpWires.Add(new Line(link.First.Position, link.Second.Position)));
        }

        private List<Curve> CreateLinearStraitLink(Line first, Point3d firstPt, Line second, Point3d secondPt)
        {
            var linkDir = firstPt.GetVectorTo(secondPt);
            var firstVec = first.LineDirection();
            var secondVec = second.LineDirection();
            var firstLinkAng = firstVec.GetAngleTo(linkDir).RadToAng();
            var secondLinkAng = secondVec.GetAngleTo(linkDir).RadToAng();
            if (firstLinkAng <= AngleTolerance || (180.0 - firstLinkAng) <= AngleTolerance)
            {
                var projectionPt = secondPt.GetProjectPtOnLine(first.StartPoint, first.EndPoint);
                var upDir = projectionPt.GetVectorTo(secondPt);
                return CreateLinearJumpLink(firstPt, secondPt, upDir, OffsetDis2, CornerAngle);
            }
            else if (secondLinkAng <= AngleTolerance || (180.0 - secondLinkAng) <= AngleTolerance)
            {
                var projectionPt = firstPt.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
                var upDir = projectionPt.GetVectorTo(firstPt);
                var upPt = secondPt + upDir;
                return CreateLinearJumpLink(firstPt, secondPt, upDir, OffsetDis2, CornerAngle);
            }
            else
            {
                var dir = GetDefaultDirection(first, second);
                return CreateLinearJumpLink(firstPt, secondPt, dir, OffsetDis2, CornerAngle);
            }
        }

        private List<Curve> CreateLinearJumpLink(Point3d firstPt, Point3d secondPt, Vector3d direction, double height, double angle = 30.0)
        {
            /*
             *     pt1                                 pt2
             *      ------------------------------------
             *     /                                     \
             *    /                                       \
             *   /                                         \
             *  firstPt                                secondPt
             */
            var results = new List<Curve>();
            var linkDir = firstPt.GetVectorTo(secondPt).GetNormal();
            var perpendVec = linkDir.GetPerpendicularVector();
            if (perpendVec.DotProduct(direction) < 0)
            {
                perpendVec = perpendVec.Negate();
            }
            var cornerDis = height / Math.Tan(angle.AngToRad());
            var pt1 = firstPt + perpendVec.MultiplyBy(height);
            pt1 = pt1 + linkDir.MultiplyBy(cornerDis);

            var pt2 = secondPt + perpendVec.MultiplyBy(height);
            pt2 = pt2 - linkDir.MultiplyBy(cornerDis);

            results.Add(new Line(firstPt, pt1));
            results.Add(new Line(pt1, pt2));
            results.Add(new Line(pt2, secondPt));
            return results;
        }

        private Vector3d GetDefaultDirection(Line first, Line second)
        {
            var firstMidPt = first.StartPoint.GetMidPt(first.EndPoint);
            var secondMidPt = second.StartPoint.GetMidPt(first.EndPoint);
            if (ThGeometryTool.IsCollinearEx(first.StartPoint, first.EndPoint, second.StartPoint, second.EndPoint))
            {
                if (Math.Abs(firstMidPt.Y - secondMidPt.Y) <= 1.0)
                {
                    // 水平
                    if (firstMidPt.X < secondMidPt.X)
                    {
                        return firstMidPt.GetVectorTo(secondMidPt).GetPerpendicularVector();
                    }
                    else
                    {
                        return secondMidPt.GetVectorTo(firstMidPt).GetPerpendicularVector();
                    }
                }
                else if (Math.Abs(firstMidPt.X - secondMidPt.X) <= 1.0)
                {
                    // 垂直
                    if (firstMidPt.Y < secondMidPt.Y)
                    {
                        return firstMidPt.GetVectorTo(secondMidPt).GetPerpendicularVector();
                    }
                    else
                    {
                        return secondMidPt.GetVectorTo(firstMidPt).GetPerpendicularVector();
                    }
                }
                else
                {
                    if (firstMidPt.X < secondMidPt.X)
                    {
                        return firstMidPt.GetVectorTo(secondMidPt).GetPerpendicularVector();
                    }
                    else
                    {
                        return secondMidPt.GetVectorTo(firstMidPt).GetPerpendicularVector();
                    }
                }
            }
            else
            {
                var dir = firstMidPt.GetVectorTo(secondMidPt).GetNormal();
                var firstPerpendDir = first.LineDirection().GetPerpendicularVector();
                if (dir.GetAngleTo(firstPerpendDir) < Math.PI / 2.0)
                {
                    return firstPerpendDir;
                }
                else
                {
                    return firstPerpendDir.Negate();
                }
            }
        }

        private List<Curve> CreateCircularJumpLink(Line first, Point3d firstPt, Line second, Point3d secondPt)
        {
            var results = new List<Curve>();
            var direction = GetDefaultDirection(first, second);
            var arcTopVec = ThArcDrawTool.CalculateArcTopVec(firstPt, secondPt, direction);
            var gap = ArrangeParameter.CircularArcTopDistanceToDxLine;
            var radius = ThArcDrawTool.CalculateRadiusByGap(firstPt.DistanceTo(secondPt), gap);
            results.Add(ThArcDrawTool.DrawArc(firstPt, secondPt, radius, arcTopVec));
            return results;
        }
    }
}
