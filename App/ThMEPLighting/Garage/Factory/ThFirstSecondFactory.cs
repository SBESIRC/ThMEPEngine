using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Factory
{
    public abstract class ThFirstSecondFactory
    {
        /// <summary>
        /// 记录边线是1号线，还是2号线
        /// </summary>
        protected Dictionary<Line, EdgePattern> SideLineNumberDict { get; set; }
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDict { get; protected set; }
        public List<Line> FirstLines
        {
            get
            {
                return SideLineNumberDict.Where(o => o.Value == EdgePattern.First).Select(o => o.Key).ToList();
            }
        }
        public List<Line> SecondLines
        {
            get
            {
                return SideLineNumberDict.Where(o => o.Value == EdgePattern.Second).Select(o => o.Key).ToList();
            }
        }
        public ThFirstSecondFactory()
        {
            SideLineNumberDict = new Dictionary<Line, EdgePattern>();
            CenterSideDict = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
        }
        public abstract void Produce();
    }
}
