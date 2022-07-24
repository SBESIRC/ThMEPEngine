using System.Collections.Generic;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public static class PipeLineTool
    {
        public static void PipeLineSplitByValve(SprayIn sprayIn, Valve valve, ref List<Line> pipeLines)
        {
            pipeLines = pipeLines.PipeLineSplit(valve.SignalValves);
            pipeLines = pipeLines.PipeLineSplit(valve.PressureValves);
            pipeLines = pipeLines.PipeLineSplit(valve.DieValves);
            pipeLines.CreatePtDic(sprayIn);
            DicTools.CreatePtTypeDic(valve.SignalValves, "SignalValve", sprayIn);
            DicTools.CreatePtTypeDic(valve.PressureValves, "PressureValves", sprayIn);
            DicTools.CreatePtTypeDic(valve.DieValves, "DieValves", sprayIn);
        }
    }
}
