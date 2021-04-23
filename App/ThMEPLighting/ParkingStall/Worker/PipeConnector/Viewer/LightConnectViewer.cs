using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Assistant;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using DotNetARX;
using Linq2Acad;

namespace ThMEPLighting.ParkingStall.Worker.PipeConnector
{
    class LightConnectViewer
    {
        private List<PipeLighterPolyInfo> m_pipeLighterPolyInfos;

        public LightConnectViewer(List<PipeLighterPolyInfo> pipeLighterPolyInfos)
        {
            m_pipeLighterPolyInfos = pipeLighterPolyInfos;
        }


        public static void MakeLightConnectViewer(List<PipeLighterPolyInfo> pipeLighterPolyInfos)
        {
            var lighterConnectViewer = new LightConnectViewer(pipeLighterPolyInfos);
            lighterConnectViewer.Do();
        }

        public void Do()
        {
            var debugSwitch = (Convert.ToInt16(Application.GetSystemVariable("USERR2")) == 1);
            if (!debugSwitch)
                return;

            foreach (var pipeLighterPolyInfo in m_pipeLighterPolyInfos)
            {
                PrintPipeLighterPolyInfo(pipeLighterPolyInfo);
            }
        }

        private void PrintPipeLighterPolyInfo(PipeLighterPolyInfo pipeLighterPolyInfo)
        {
            PrintSidePipeInfo(pipeLighterPolyInfo.OneSideInfo);
            PrintSidePipeInfo(pipeLighterPolyInfo.OtherSideInfo);
        }

        private void PrintSidePipeInfo(SidePipeInfo sidePipeInfo)
        {
            foreach (var pipeGroup in sidePipeInfo.PipeGroups)
            {
                PrintPipeGroup(pipeGroup);
            }
        }

        private void PrintPipeGroup(PipeGroup pipeGroup)
        {
            var addLines = pipeGroup.PipeLines;
            var addCurves = new List<Curve>();
            addLines.ForEach(line => addCurves.Add(line));
            addCurves.Add(pipeGroup.LanePolyline);

            var addLineIds = DrawUtils.DrawProfileDebug(addCurves, "dividePipeGroup", Color.FromRgb(0, 0, 255));
            var totalIds = new ObjectIdList();
            totalIds.AddRange(addLineIds);

            var groupName = totalIds.First().ToString();
            using (var db = AcadDatabase.Active())
            {
                GroupTools.CreateGroup(db.Database, groupName, totalIds);
            }
        }
    }
}
