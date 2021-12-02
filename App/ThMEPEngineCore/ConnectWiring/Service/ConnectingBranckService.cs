using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPEngineCore.ConnectWiring.Model;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public class ConnectingBranckService
    {
        double mergeDis = 500;
        public Polyline CreateBranch(Polyline wiring, List<BlockReference> blocks, List<LoopBlockInfos> loopBlockInfos)
        {
            var sBlocks = blocks.Where(x => x.Position.DistanceTo(wiring.StartPoint) < mergeDis).OrderBy(x=>x.Position.DistanceTo(wiring.StartPoint)).ToList();
            var eBlocks = blocks.Where(x => x.Position.DistanceTo(wiring.EndPoint) < mergeDis).OrderBy(x => x.Position.DistanceTo(wiring.EndPoint)).ToList();
            //if (sBlocks.Count > 0)
            //{
            //    var objs = sBlocks.SelectMany(x => CAD.ThDrawTool.Explode(x).Cast<Entity>()).ToList();
            //    var exObjs = ExplodeToBasic(objs);
            //    var holes = sBlocks.SelectMany(x => x.ToOBB(x.BlockTransform).Buffer(-5).Cast<Polyline>()).ToList();
            //    wiring = TrimWiring(wiring, exObjs, holes);
            //}
            //if (eBlocks.Count > 0)
            //{
            //    var objs = eBlocks.SelectMany(x => CAD.ThDrawTool.Explode(x).Cast<Entity>()).ToList();
            //    var exObjs = ExplodeToBasic(objs);
            //    var holes = eBlocks.SelectMany(x => x.ToOBB(x.BlockTransform).Buffer(-5).Cast<Polyline>()).ToList();
            //    wiring = TrimWiring(wiring, exObjs, holes);
            //}
            return null;//ConnectToBlock(wiring, blocks);
        }

    }
}
