using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.ParkingStall.Model
{
    /// <summary>
    /// 根据内墙线 和 车道线进行分割后第一次连接的结构
    /// </summary>
    public class PipeLighterPolyInfo
    {
        public Polyline LanePoly;

        public SidePipeInfo OneSideInfo;

        public SidePipeInfo OtherSideInfo;

        public PipeLighterPolyInfo(Polyline polyline, SidePipeInfo oneSidePipeInfo, SidePipeInfo otherSidePipeInfo)
        {
            LanePoly = polyline;
            OneSideInfo = oneSidePipeInfo;
            OtherSideInfo = otherSidePipeInfo;
        }
    }

    /// <summary>
    /// 车道线一侧的连接情况（一侧可能有多组）
    /// </summary>
    public class SidePipeInfo
    {
        public List<PipeGroup> PipeGroups;

        public SidePipeInfo(List<PipeGroup> pipeGroups)
        {
            PipeGroups = pipeGroups;
        }
    }

    /// <summary>
    /// 最小连通的灯连接情况
    /// </summary>
    public class PipeGroup
    {
        public List<LightPlaceInfo> LightPlaceInfos;
        public List<Line> PipeLines;

        public Polyline LanePolyline;

        public PipeGroup(List<LightPlaceInfo> lightPlaceInfos, Polyline lanePolyline)
        {
            LightPlaceInfos = lightPlaceInfos;
            LanePolyline = lanePolyline;
        }
    }
}
