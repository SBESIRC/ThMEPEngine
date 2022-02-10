using System;
using System.Linq;
using System.Collections.Generic;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;

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
        private ThJumpWireDirectionCalculator Calculator { get; set; }
        public ThStraitLinkCreator(
            ThLightArrangeParameter arrangeParameter, 
            Dictionary<string, int> directionConfig,
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            ArrangeParameter = arrangeParameter;
            DirectionConfig = directionConfig;
            CenterSideDicts = centerSideDicts;
            Calculator = new ThJumpWireDirectionCalculator(centerSideDicts);
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
        public void CreateWireForStraitLink(List<ThLightNodeLink> links)
        {
            var linearLinks = links.Where(o => IsSuiteStraitLink(o, AngleTolerance)).ToList();
            CreateLinearStraitLink(linearLinks);
            var circularLinks = links.Where(o => !linearLinks.Contains(o)).ToList();
            CreateCircularArcStraitLink(circularLinks);
        }
        private void CreateLinearStraitLink(List<ThLightNodeLink> lightNodeLinks)
        {
            if(lightNodeLinks.Count==0)
            {
                return;
            }
            var jumpWireFactory = new ThLightLinearJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                OffsetDis2 = this.ArrangeParameter.JumpWireOffsetDistance + this.ArrangeParameter.LightNumberTextGap / 2.0,
            };
            jumpWireFactory.BuildSideLinesSpatialIndex();
            jumpWireFactory.BuildStraitLinks();
        }
        private void CreateCircularArcStraitLink(List<ThLightNodeLink> lightNodeLinks)
        {
            if (lightNodeLinks.Count == 0)
            {
                return;
            }
            var jumpWireFactory = new ThLightCircularArcJumpWireFactory(lightNodeLinks)
            {
                CenterSideDicts = this.CenterSideDicts,
                DirectionConfig = this.DirectionConfig,
                LampLength = this.ArrangeParameter.LampLength,
                LampSideIntervalLength = this.ArrangeParameter.LampSideIntervalLength,
                Gap = this.ArrangeParameter.CircularArcTopDistanceToDxLine*2,
            };
            jumpWireFactory.BuildStraitLinks();
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
    }
}
