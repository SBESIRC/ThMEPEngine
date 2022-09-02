using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO.SVG;

namespace ThPlatform3D.ArchitecturePlane.Service
{
    internal abstract class ThComponentOutlineCreator
    {
        private Dictionary<double, double> WallThickMap { get; set; }
 
        public ThComponentOutlineCreator()
        {
            WallThickMap = new Dictionary<double, double>();
        }
        public abstract DBObjectCollection Create(List<ThComponentInfo> components);
        protected double GetWallThick(string wallThick)
        {
            if(string.IsNullOrEmpty(wallThick))
            {
                return 0.0;
            }
            var values = wallThick.GetDoubles();
            return values.Count == 1 ? values[0] : 0.0;
        }
        protected double GetMapWallThick(double wallThick,double tolerance=1e-6)
        {
            if(WallThickMap.ContainsKey(wallThick))
            {
                return WallThickMap[wallThick];
            }
            else
            {
                foreach(var item in WallThickMap)
                {
                    if(Math.Abs(item.Key-wallThick)<= tolerance)
                    {
                        return item.Value;
                    }
                }
                return 0.0;
            }
        }
    }
}
