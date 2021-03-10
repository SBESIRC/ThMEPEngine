using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Assistant;

namespace ThMEPLighting.ParkingStall.Worker.PipeConnector
{
    public class LightConnector
    {
        private List<LaneGroup> m_laneGroups;
        private List<Polyline> m_splitPolys = new List<Polyline>();
        private Light_Place_Type m_light_Place_Type;
        private List<Line> m_srcLaneLines;

        public List<PipeLighterPolyInfo> PipeLighterPolylInfos;


        public LightConnector(List<LaneGroup> laneGroups, List<Line> srcLaneLines, List<Polyline> innerWalls, Light_Place_Type light_Place_Type)
        {
            m_laneGroups = laneGroups;
            m_splitPolys = innerWalls;
            m_light_Place_Type = light_Place_Type;
            m_srcLaneLines = srcLaneLines;
        }


        public static List<PipeLighterPolyInfo> MakeLightConnector(List<LaneGroup> laneGroups, List<Line> srcLaneLines, List<Polyline> innerWalls, Light_Place_Type light_Place_Type)
        {
            var lightConnector = new LightConnector(laneGroups, srcLaneLines, innerWalls, light_Place_Type);
            lightConnector.Do();

            return lightConnector.PipeLighterPolylInfos;
        }

        public void Do()
        {
            CalculateSrcSplitPolys();

            // 数据转化，得到车道线和初次单侧灯组匹配的信息
            var pipeLights = LaneGroupReader.MakePipeLaneLighterInfo(m_laneGroups);

            // 两侧连管
            LaneSideConnect(pipeLights);
        }

        /// <summary>
        /// 两侧连管 ，会被隔断分割
        /// </summary>
        /// <param name="pipeLighterInfos"></param>
        private void LaneSideConnect(List<PipeLighterInfo> pipeLighterInfos)
        {
            // 根据车道线，分割线初步分割的结果
            PipeLighterPolylInfos = LaneSideLightCalculator.MakePipeLighterPolyInfo(pipeLighterInfos, m_splitPolys, m_light_Place_Type);
        }

        /// <summary>
        /// 车道线和内墙线构成分割线
        /// </summary>
        private void CalculateSplitPolys()
        {
            m_laneGroups.ForEach(laneGroup =>
            {
                m_splitPolys.Add(laneGroup.LanePoly);
            });
        }

        /// <summary>
        /// 使用原始的车道线和内墙线
        /// </summary>
        private void CalculateSrcSplitPolys()
        {
            foreach (var line in m_srcLaneLines)
            {
                var points = new List<Point3d>();
                points.Add(line.StartPoint);
                points.Add(line.EndPoint);
                var poly = Line2Poly(line);
                m_splitPolys.Add(poly);
            }
        }

        private Polyline Line2Poly(Line line)
        {
            var startPoint = line.StartPoint;
            var endPoint = line.EndPoint;
            var poly = new Polyline();

            poly.AddVertexAt(0, startPoint.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, endPoint.ToPoint2D(), 0, 0, 0);
            return poly;
        }
    }
}
