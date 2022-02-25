using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPWSS.DrainageSystemDiagram;
using ThMEPWSS.SprinklerPiping.Engine;
using ThMEPWSS.SprinklerPiping.Model;
using ThMEPWSS.SprinklerConnect.Model;
using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Engine;

namespace ThMEPWSS.SprinklerPiping.Model
{
    public class SprinklerTreeState
    {
        public HashSet<SprinklerPipe> pipes = new HashSet<SprinklerPipe>(); //TODO: pipes, assigned
        //public List<SprinklerPoint> sprinklerPoints = new List<SprinklerPoint>(); //可以优化内存 比如只存assigned

        public Dictionary<Point3d, bool> assignList = new Dictionary<Point3d, bool>();
        //public bool isTerminal;
        public List<SprinklerTreeNode> choices = new List<SprinklerTreeNode>(); //可选择的项（建立时设置，选择过之后加入node的children）
        //public int unAssignedCnt;

        public SprinklerTreeState(List<SprinklerPoint> sprinklerPoints)
        {
            foreach(var pt in sprinklerPoints)
            {
                assignList.Add(pt.pos, false);
            }
            ////pipes = new HashSet<SprinklerPipe>();
            ////sprinklerPoints = new List<SprinklerPoint>();
            //foreach (var pt in sprinklerPoints)
            //{
            //    this.sprinklerPoints.Add(new SprinklerPoint(pt));
            //}
            ////isTerminal = false;
            ////choices = new List<SprinklerTreeNode>();
            //unAssignedCnt = sprinklerPoints.Count;
        }

        
        public SprinklerTreeState(Dictionary<Point3d, bool> assignList)
        {
            this.assignList = assignList;
        }

        public SprinklerTreeState(SprinklerTreeState state)
        {
            //pipes = new HashSet<SprinklerPipe>();
            foreach(var pipe in state.pipes)
            {
                pipes.Add(new SprinklerPipe(pipe));
            }
            //sprinklerPoints = new List<SprinklerPoint>();
            //foreach(var pt in state.sprinklerPoints)
            //{
            //    sprinklerPoints.Add(new SprinklerPoint(pt));
            //}
            //isTerminal = state.isTerminal;
            assignList = new Dictionary<Point3d, bool>(state.assignList);
            choices = new List<SprinklerTreeNode>(state.choices);
            //unAssignedCnt = state.unAssignedCnt;
        }

        public SprinklerTreeState(SprinklerTreeState initState, List<Line> newLines)
        {
            //pipes = new HashSet<SprinklerPipe>(initState.pipes);
            //pipes = new HashSet<SprinklerPipe>();
            foreach (var pipe in initState.pipes)
            {
                pipes.Add(new SprinklerPipe(pipe));
            }
            //pipes.UnionWith(new HashSet<SprinklerPipe>(newLines));
            foreach (var line in newLines)
            {
                SprinklerPipe pipe = new SprinklerPipe(line, false);
                pipes.Add(pipe);
            }
            assignList = new Dictionary<Point3d, bool>(initState.assignList);
            //sprinklerPoints = new List<SprinklerPoint>(initState.sprinklerPoints);
            //sprinklerPoints = new List<SprinklerPoint>();
            //foreach (var pt in initState.sprinklerPoints)
            //{
            //    sprinklerPoints.Add(new SprinklerPoint(pt));
            //}
            //isTerminal = initState.isTerminal;
            //choices = new List<SprinklerTreeNode>();
            //unAssignedCnt = initState.unAssignedCnt;
        }

        public void AssignPts(HashSet<Point3d> ptList)
        {
            foreach(var pt in ptList)
            {
                assignList[pt] = true;
            }
        }

        //public void AssignPts(HashSet<int> ptIdxList)
        //{
        //    foreach(var idx in ptIdxList)
        //    {
        //        if(idx < sprinklerPoints.Count && sprinklerPoints[idx].assigned == false)
        //        {
        //            sprinklerPoints[idx].assigned = true;
        //            unAssignedCnt--;
        //            //if(unAssignedCnt == 0)
        //            //{
        //            //    isTerminal = true;
        //            //}
        //        }
        //    }
        //}

        public int assignedCnt()
        {
            int cnt = 0;
            foreach (var pipe in pipes)
            {
                if (pipe.assigned)
                {
                    cnt++;
                }
            }
            return cnt;
        }

        public bool isTerminal()
        {
            if (assignList.Count() != 0 && assignList.Count(x => x.Value == false) == 0)
            {
                return true;
            }
            if(pipes.Count != 0)
            {
                foreach (var pipe in pipes)
                {
                    if (!pipe.assigned)
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
            //public SprinklerTreeState takeAction()
            //{
            //    Random rand = new Random();
            //    //int k = 
            //}
        }
    }


}
