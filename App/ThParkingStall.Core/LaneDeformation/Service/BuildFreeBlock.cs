using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThParkingStall.Core.LaneDeformation;

using NetTopologySuite.Operation.OverlayNG;

namespace ThParkingStall.Core.LaneDeformation
{
    public class BuildFreeBlock
    {
        public Vector2D MoveDir = new Vector2D();

        public List<FreeBlock> FreeBlocks;
        public BuildFreeBlock(Vector2D dir) 
        {
            MoveDir = dir;   
        }


        public void Pipeline() 
        {
            
            
        
        
        }
    }
}
