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
    public class SprinklerTreeNode
    {
        public enum nodeType
        {
            root, parking, direction, length, connecting
        }

        public SprinklerTreeState state { get; set; }
        public nodeType type;
        public Vector3d dir;
        public int len; //dttol的倍数
        public Point3d endPos;
        public int turnCnt;
        public int wallCnt;
        public bool isTerminal = false;
        public bool isFullyExpanded = false;
        public SprinklerTreeNode parent { get; set; }
        public List<SprinklerTreeNode> children { get; set; }
        public double initWeight;
        public int numVisits { get; set; }
        public double totalReward { get; set; }

        public double bestReward;

        public SprinklerTreeNode() { }
        public SprinklerTreeNode(SprinklerTreeState state, SprinklerTreeNode parent, nodeType type)
        {
            this.state = state;
            isTerminal = state.isTerminal();
            //isFullyExpanded = state.isTerminal;
            this.parent = parent;
            this.type = type;
            if(parent != null)
            {
                endPos = parent.endPos;
                dir = parent.dir;
                turnCnt = parent.turnCnt;
                wallCnt = parent.wallCnt;
                len = parent.len;
                isFullyExpanded = parent.isFullyExpanded;
            }
            
            children = new List<SprinklerTreeNode>();
        }

    }
}
