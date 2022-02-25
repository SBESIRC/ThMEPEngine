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

using ThMEPWSS.SprinklerConnect.Service;
using ThMEPWSS.SprinklerConnect.Model;

namespace ThMEPWSS.SprinklerPiping.Model
{	public enum Scenes
	{
		Parking, SmallRoom, Tower, Corridor, NarrowCorridor, Others
	}
	
	public class SprinklerPoint
    {
		//Dictionary<string, int> neighborMap = new Dictionary<string, int>{
  //          {"up", 0}, {"right", 1}, {"down", 2}, {"left", 3}
  //      };
		public int idx; //useless
		public Point3d pos; //(x,y,0)
		public double ucsAngle; //0-90
		public int groupIdx { set; get; }
		public int graphIdx { set; get; } //-1 for scattered points
		public int nodeIdx { set; get; } //-1 for scattered points
		public int ptIdx { set; get; }
		public Scenes scene { set; get; }
		public int branchDir { set; get; } //-1:undefined, 0:0-90, 1:90-180
		//public bool assigned { set; get; } //0:unwatered, 1:watered
        public SprinklerPoint upNeighbor { set; get; }
		public SprinklerPoint rightNeighbor { set; get; }
		public SprinklerPoint leftNeighbor { set; get; }
		public SprinklerPoint downNeighbor { set; get; }

		//init
		public SprinklerPoint(int idx, double posx, double posy, int groupIdx, int ptIdx, double ucsAngle)
		{
			this.idx = idx;
			this.pos = new Point3d(posx, posy, 0);
            this.scene = Scenes.Others;
			this.branchDir = -1;
			this.groupIdx = groupIdx;
			this.graphIdx = -1;
			this.nodeIdx = -1;
			this.ptIdx = ptIdx;
			this.ucsAngle = ucsAngle;
		}

		public SprinklerPoint(int idx, double posx, double posy, int groupIdx, int graphIdx, int nodeIdx, int ptIdx, double ucsAngle)
		{
			this.idx = idx;
			this.pos = new Point3d(posx, posy, 0);
			this.scene = Scenes.Others;
			this.branchDir = -1;
			this.groupIdx = groupIdx;
			this.graphIdx = graphIdx;
			this.nodeIdx = nodeIdx;
			this.ptIdx = ptIdx;
			this.ucsAngle = ucsAngle;
		}

		public SprinklerPoint(SprinklerPoint pt)
        {
			idx = pt.idx;
			pos = pt.pos;
			scene = pt.scene;
			ucsAngle = pt.ucsAngle;
			groupIdx = pt.groupIdx;
			graphIdx = pt.graphIdx;
			nodeIdx = pt.nodeIdx;
			ptIdx = pt.ptIdx;
			branchDir = pt.branchDir;
			//assigned = pt.assigned;
			upNeighbor = pt.upNeighbor;
			downNeighbor = pt.downNeighbor;
			leftNeighbor = pt.leftNeighbor;
			rightNeighbor = pt.rightNeighbor;
        }

	}
}
