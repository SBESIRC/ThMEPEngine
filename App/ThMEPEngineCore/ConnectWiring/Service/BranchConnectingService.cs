using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPEngineCore.ConnectWiring.Service
{
    public class BranchConnectingService
    {
        //double disTol = 1;
        double TesslateLength = 50;
        double mergeDis = 500;
        public Polyline CreateBranch(Polyline wiring, List<BlockReference> blocks)
        {
            return ConnectToBlock(wiring, blocks);
        }

        /// <summary>
        /// 连接到块
        /// </summary>
        /// <param name="wiring"></param>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private Polyline ConnectToBlock(Polyline wiring, List<BlockReference> blocks)
        {
            var sBlocks = blocks.Where(x => x.Position.DistanceTo(wiring.StartPoint) < mergeDis).ToList();
            var eBlocks = blocks.Where(x => x.Position.DistanceTo(wiring.EndPoint) < mergeDis).ToList();
            if (sBlocks.Count > 0)
            {
                var objs = sBlocks.SelectMany(x => CAD.ThDrawTool.Explode(x).Cast<Entity>()).ToList();
                var exObjs = ExplodeToBasic(objs);
                wiring = TrimWiring(wiring, exObjs);
            }
            if (eBlocks.Count > 0)
            {
                var objs = eBlocks.SelectMany(x => CAD.ThDrawTool.Explode(x).Cast<Entity>()).ToList();
                var exObjs = ExplodeToBasic(objs);
                wiring = TrimWiring(wiring, exObjs);
            }

            return wiring;
        }

        /// <summary>
        /// 砍掉多余的线部分
        /// </summary>
        /// <param name="wiring"></param>
        /// <param name="blockGeos"></param>
        /// <returns></returns>
        private Polyline TrimWiring(Polyline wiring, DBObjectCollection blockGeos)
        {
            var blockLst = blockGeos.Cast<Curve>().ToList();
            blockGeos.Add(wiring);
            var nodeGeo = blockGeos.ToNTSNodedLineStrings();
            var handleLine = wiring;
            if (nodeGeo != null)
            {
                var resLine = nodeGeo.ToDbObjects()
                    .Select(x => x as Polyline)
                    .Where(x=> blockLst.All(y => {
                        if (y is Line || y is Polyline)
                        {
                            return y.StartPoint.DistanceTo(x.StartPoint) > 1
                            && y.EndPoint.DistanceTo(x.StartPoint) > 1
                            && y.StartPoint.DistanceTo(x.EndPoint) > 1
                            && y.EndPoint.DistanceTo(x.EndPoint) > 1;
                        }
                        return true;
                    }))
                    .OrderByDescending(x => x.Length)
                    .FirstOrDefault();
                if (resLine != null)
                {
                    handleLine = resLine;
                }
            }
            return handleLine;
        }

        /// <summary>
        /// 处理块中的图元
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        private DBObjectCollection ExplodeToBasic(List<Entity> objs)
        {
            //理想是炸到Line,Arc,Circle,Ellipse,目前不支持对椭圆的处理，这里不抛出不支持的异常
            var results = new DBObjectCollection();
            objs.Where(o => o is Curve).ForEach(c =>
            {
                if (c is Line)
                {
                    results.Add(c);
                }
                else if (c is Arc arc)
                {
                    results.Add(arc.TessellateArcWithArc(TesslateLength));
                }
                else if (c is Circle circle)
                {
                    results.Add(circle.TessellateCircleWithArc(TesslateLength));
                }
                else if (c is Polyline2d || c is Polyline)
                {
                    var subObjs = new DBObjectCollection();
                    c.Explode(subObjs);
                    subObjs.Cast<Curve>().ForEach(e => results.Add(e));
                }
            });
            return results;
        }
    }
}
