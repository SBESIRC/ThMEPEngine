using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutResult
{
    internal class ThDefaultLinkWireFilter: ThLinkWireFilter
    {
        /*
         *  --------***-------***-------***--------***---------
         *   --- 代表在Edge上创建的线 Wires
         *   *** 代表默认灯自己的线 Lights
         *   目的是为了过滤此类的线: 一端连着灯，一端未连灯
         *   一个编号代表一个回路，每个回路都有自己连接的线
         */
        #region ---------- input ------------
        /// <summary>
        /// 灯线
        /// </summary>
        private DBObjectCollection Wires { get; set; }
        /// <summary>
        /// 灯
        /// </summary>
        private DBObjectCollection Lights { get; set; }

        #endregion
        #region ---------- output ----------
        public DBObjectCollection Results { get; private set; }
        #endregion

        public ThDefaultLinkWireFilter(
            DBObjectCollection wires,
            DBObjectCollection lights)
        {
            Wires = wires;
            Lights = lights;
            Results = new DBObjectCollection();

        }

        public override void Filter()
        {
            var lightRoute = new ThLightRouteService(Wires, Lights);
            lightRoute.Traverse();
            lightRoute.Links.ForEach(l => AddToResults(l.Wires));
        }

        private void AddToResults(List<Curve> pathLines)
        {
            pathLines
                .Where(o => !Results.Contains(o))
                .ForEach(o => Results.Add(o));
        }
    }
    
}
