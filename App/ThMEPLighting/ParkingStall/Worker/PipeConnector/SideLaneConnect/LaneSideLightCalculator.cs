using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Model;

namespace ThMEPLighting.ParkingStall.Worker.PipeConnector
{
    /// <summary>
    /// 根据长边或者的短边的布置方式，以及是否打断信息（原始的车道线和内墙线），进行单侧灯组的灯信息连接
    /// </summary>
    public class LaneSideLightCalculator
    {
        private List<PipeLighterInfo> m_pipeLighterInfos;

        private List<Polyline> m_splitPolys;

        private Light_Place_Type m_light_Place_Type;

        public List<PipeLighterPolyInfo> PipeLighterPolylInfos = new List<PipeLighterPolyInfo>();

        public static List<PipeLighterPolyInfo> MakePipeLighterPolyInfo(List<PipeLighterInfo> pipeLighterInfos, List<Polyline> splitPolys, Light_Place_Type light_Place_Type)
        {
            var laneSideLightCalculator = new LaneSideLightCalculator(pipeLighterInfos, splitPolys, light_Place_Type);
            laneSideLightCalculator.Do();
            return laneSideLightCalculator.PipeLighterPolylInfos;
        }

        public LaneSideLightCalculator(List<PipeLighterInfo> pipeLighterInfos, List<Polyline> splitPolys, Light_Place_Type light_Place_Type)
        {
            m_pipeLighterInfos = pipeLighterInfos;
            m_splitPolys = splitPolys;
            m_light_Place_Type = light_Place_Type;
        }

        public void Do()
        {
            foreach (var pipelighterInfo in m_pipeLighterInfos)
            {
                PipeLighterPolylInfos.Add(CalculatePipeLighterPolyInfo(pipelighterInfo, m_splitPolys));
            }
        }

        /// <summary>
        /// 单个分割
        /// </summary>
        /// <param name="pipeLighterInfo"></param>
        /// <param name="splitPolys"></param>
        /// <returns></returns>
        private PipeLighterPolyInfo CalculatePipeLighterPolyInfo(PipeLighterInfo pipeLighterInfo, List<Polyline> splitPolys)
        {
            var lanePoly = pipeLighterInfo.LanePoly;
            var oneSidePipeInfo = CalculateSidePipeInfo(pipeLighterInfo.OneSideLights, lanePoly, splitPolys);
            var otherSidePipeInfo = CalculateSidePipeInfo(pipeLighterInfo.OtherSideLights, lanePoly, splitPolys);

            return new PipeLighterPolyInfo(lanePoly, oneSidePipeInfo, otherSidePipeInfo);
        }

        /// <summary>
        /// 对一组灯组信息进行分组
        /// </summary>
        /// <param name="lightPlaceInfos"></param>
        /// <param name="lanePoly"></param>
        /// <param name="splitPolys"></param>
        /// <returns></returns>
        private List<PipeGroup> DivideGroup(List<LightPlaceInfo> lightPlaceInfos, Polyline lanePoly, List<Polyline> splitPolys)
        {
            var pipeGroups = new List<PipeGroup>();

            // 从一边连起来，中止于分割线
            foreach (var lightPlaceInfo in lightPlaceInfos)
            {
                var lightPlaceInfoPos = lightPlaceInfo.Position;
                lightPlaceInfo.PtOnLanePoly = lanePoly.GetClosestPointTo(lightPlaceInfoPos, true);
                lightPlaceInfo.LengthFromStart = lanePoly.GetDistAtPoint(lightPlaceInfo.PtOnLanePoly);
            }

            lightPlaceInfos.Sort((light1, light2) =>
            {
                return light1.LengthFromStart.CompareTo(light2.LengthFromStart);
            });

            // divide group
            var lightGroup = new List<LightPlaceInfo>();
            for (int i = 0; i < lightPlaceInfos.Count; i++)
            {
                var curLightPlaceInfo = lightPlaceInfos[i];
                lightGroup.Add(curLightPlaceInfo);

                if (i == lightPlaceInfos.Count - 1)
                {
                    pipeGroups.Add(new PipeGroup(lightGroup, lanePoly));
                    break;
                }

                var nextLightPlaceInfo = lightPlaceInfos[i + 1];
                var connectLine = new Line(curLightPlaceInfo.Position, nextLightPlaceInfo.Position);

                if (IsSplitLine(connectLine, splitPolys))
                {
                    var cloneLightGroup = new List<LightPlaceInfo>();
                    lightGroup.ForEach(light => cloneLightGroup.Add(light));
                    lightGroup.Clear();

                    pipeGroups.Add(new PipeGroup(cloneLightGroup, lanePoly));
                }
            }

            return pipeGroups;
        }

        /// <summary>
        /// 车道线单侧连线 点位排序连接(sort)
        /// </summary>
        /// <param name="lightPlaceInfos"></param>
        /// <param name="lanePoly"></param>
        /// <returns> 多根连线</returns>
        private SidePipeInfo CalculateSidePipeInfo(List<LightPlaceInfo> lightPlaceInfos, Polyline lanePoly, List<Polyline> splitPolys)
        {
            var pipeGroups = DivideGroup(lightPlaceInfos, lanePoly, splitPolys);

            // 连线
            foreach (var group in pipeGroups)
            {
                using (var acadDatabase = AcadDatabase.Active())
                {
                    // 计算出连接点的信息
                    foreach (var lightPlaceInfo in group.LightPlaceInfos)
                    {
                        if (lightPlaceInfo.InsertBlockId != ObjectId.Null)
                        {
                            var lightBlock = acadDatabase.Element<BlockReference>(lightPlaceInfo.InsertBlockId);
                            var dbObjs = new DBObjectCollection();
                            lightBlock.Explode(dbObjs);
                            var obbPolyline = dbObjs.GetMinimumRectangle();
                            var midPoint = lightPlaceInfo.Position; // 插入点
                            var endPoints = GetEndPoints(obbPolyline);
                            if (endPoints.Count == 2)
                                lightPlaceInfo.LightBlockConnectInfo = new LightBlockConnectPointInfo(midPoint, endPoints[0], endPoints[1]);
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }

                    // 灯之间的连线
                    group.PipeLines = DivideSideLightCalculator.MakeDivideSideLightAddLines(group, m_light_Place_Type);
                }
            }

            return new SidePipeInfo(pipeGroups);
        }

        private List<Point3d> GetEndPoints(Polyline polyline)
        {
            var points = new List<Point3d>();
            var results = new List<Line>();
            var polylineSegments = new PolylineSegmentCollection(polyline);
            foreach (var segment in polylineSegments)
            {
                var line = new Line(segment.StartPoint.ToPoint3d(), segment.EndPoint.ToPoint3d());
                if (line.Length > 10)
                    results.Add(line);
            }

            results.Sort((line1, line2) =>
            {
                return line1.Length.CompareTo(line2.Length);
            });

            var shortLine1 = results.First();
            var shortLine2 = results[1];

            points.Add(GetMidPoint(shortLine1));
            points.Add(GetMidPoint(shortLine2));
            return points;
        }

        private Point3d GetMidPoint(Line line)
        {
            var midParam = (line.StartParam + line.EndParam) * 0.5;
            return line.GetPointAtParameter(midParam);
        }

        /// <summary>
        /// 分割线
        /// </summary>
        /// <param name="line"></param>
        /// <param name="polylines"></param>
        /// <returns></returns>
        private bool IsSplitLine(Line line, List<Polyline> polylines)
        {
            foreach (var splitPoly in polylines)
            {
                var ptLst = new Point3dCollection();
                line.IntersectWith(splitPoly, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
                if (ptLst.Count != 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
