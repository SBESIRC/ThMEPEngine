using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Method
{
    public class CheckPipe
    {
        private List<List<Point3dEx>> MainPathList { get; set; }
        private List<List<Point3dEx>> SubPathList { get; set; }
        private readonly string LayerName = "W-辅助";

        public CheckPipe(List<List<Point3dEx>> mainPathList, List<List<Point3dEx>> subPathList)
        {
            MainPathList = mainPathList;
            SubPathList = subPathList;
            DbHelper.EnsureLayerOn(LayerName);
        }
        public void DrawMainLoop(AcadDatabase acadDatabase)
        {
            foreach (var mainLoop in MainPathList)
            {
                for(int i = 0; i < mainLoop.Count - 1; i++)
                {
                    var spt = mainLoop[i]._pt;
                    var ept = mainLoop[i + 1]._pt;
                    var line = new Line(spt, ept);
                    line.Layer = LayerName;
                    line.LineWeight = LineWeight.LineWeight100;
                    line.ColorIndex = 255;
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
#if DEBUG
            string layerName = "主环qsh";
            DbHelper.EnsureLayerOn(layerName);
            foreach (var mainLoop in MainPathList)
            {
                for (int i = 0; i < mainLoop.Count - 1; i++)
                {
                    try
                    {
                        var spt2 = mainLoop[i]._pt;
                        var ept2 = mainLoop[i + 1]._pt;
                        var line2 = new Line(spt2, ept2);
                        line2.Layer = layerName;
                        acadDatabase.CurrentSpace.Add(line2);
                    }
                    catch(Exception ex)
                    {
                        ;
                    }
                    
                }
            }
#endif
        }

        public void DrawSubLoop(AcadDatabase acadDatabase)
        {
            foreach (var subLoop in SubPathList)
            {
                for (int i = 0; i < subLoop.Count - 1; i++)
                {
                    var spt = subLoop[i]._pt;
                    var ept = subLoop[i + 1]._pt;
                    var line = new Line(spt, ept);
                    line.Layer = "W-辅助";
                    line.LineWeight = LineWeight.LineWeight100;
                    line.ColorIndex = 255;
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }

        public void DrawBranchLoop(AcadDatabase acadDatabase, FireHydrantSystemIn fireHydrantSysIn, Dictionary<Point3dEx, List<Point3dEx>> branchDic)
        {
            var branchs = GetBranchLoop(fireHydrantSysIn, branchDic);
            foreach (var branch in branchs)
            {
                for(int i = 0; i < branch.Count - 1; i++)
                {
                    var spt = branch[i]._pt;
                    var ept = branch[i + 1]._pt;
                    var line = new Line(spt, ept);
                    line.Layer = LayerName;
                    line.LineWeight = LineWeight.LineWeight100;
                    line.ColorIndex = 255;
                    acadDatabase.CurrentSpace.Add(line);
                }
            }
        }

        private List<List<Point3dEx>> GetBranchLoop(FireHydrantSystemIn fireHydrantSysIn, Dictionary<Point3dEx, List<Point3dEx>> branchDic)
        {
            var branchs = new List<List<Point3dEx>>();
            var neverVisited = new HashSet<Point3dEx>();
            foreach (var loop in MainPathList)
            {
                foreach(var pt in loop)
                {
                    neverVisited.Add(pt);
                }
            }
            foreach (var loop in SubPathList)
            {
                foreach (var pt in loop)
                {
                    neverVisited.Add(pt);
                }
            }
            foreach (var startPt in branchDic.Keys)
            {
                foreach(var endPt in branchDic[startPt])
                {
                    var tempPath = new List<Point3dEx>();
                    var visited = new HashSet<Point3dEx>();
                    tempPath.Add(startPt);
                    visited.Add(startPt);
                    DepthFirstSearch.dfsBranchLoop(startPt, endPt, tempPath, neverVisited, visited, ref branchs, fireHydrantSysIn);
                }
            }

            return branchs;
        }
    }
}
