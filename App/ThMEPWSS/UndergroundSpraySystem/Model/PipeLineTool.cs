﻿using System.Collections.Generic;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public static class PipeLineTool
    {
        public static void PipeLineDeal(SprayIn sprayIn, VerticalPipeNew vertical, ref List<Line> pipeLines)
        {
            var verticalSpatialIndex = vertical.CreateVerticalSpatialIndex();
            pipeLines = pipeLines.ConnectVerticalLine(sprayIn);
            ;
            pipeLines = pipeLines.PipeLineAutoConnect(sprayIn, verticalSpatialIndex);//自动连接

            pipeLines.CreatePtDic(sprayIn);
            pipeLines = pipeLines.ConnectBreakLine(sprayIn);
            pipeLines.CreatePtDic(sprayIn);
            pipeLines = pipeLines.PipeLineSplit(sprayIn.PtDic.Keys.ToList());
            pipeLines.CreatePtDic(sprayIn);
            DicTools.CreatePtTypeDic(sprayIn.PtDic.Keys.ToList(), "MainLoop", sprayIn);
            foreach (var line in pipeLines.ToList())
            {
                var spt = new Point3dEx(line.StartPoint);
                var ept = new Point3dEx(line.EndPoint);
                if (sprayIn.PtDic[spt].Count == 1 || sprayIn.PtDic[ept].Count == 1)
                {
                    var l = line.Length;
                    if (l < 10)
                    {
                        pipeLines.Remove(line);
                    }
                }
            }
        }

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
