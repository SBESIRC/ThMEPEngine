using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.ArchitecturePlane.Service
{
    internal class ThWindowOutlineCreator : ThComponentOutlineCreator
    {
        private Tuple<double, double> WallWindowThickMap { get; set; }
        public ThWindowOutlineCreator()
        {
            // 天华默认100的墙厚，对应30的窗厚
            WallWindowThickMap = Tuple.Create(100.0, 30.0);
        }
        public override DBObjectCollection Create(List<ThComponentInfo> windows)
        {
            var results = new DBObjectCollection();
            windows.ForEach(w =>
            {
                var outline = Create(w);
                if(outline!=null)
                {
                    results.Add(outline);
                }
            });
            return results;
        }
        public void SetWallWindowThickMap(double wallThick, double windowThick)
        {
            WallWindowThickMap = Tuple.Create(wallThick, windowThick);
        }
        private Polyline Create(ThComponentInfo window)
        {
            var startCord = window.Start.ToPoint3d();
            var endCord = window.End.ToPoint3d();
            if (!startCord.HasValue && !endCord.HasValue)
            {
                return null;
            }
            // 获取构件本身的墙厚度
            var wallThick = GetWallThick(window.Thickness);
            if(wallThick ==0.0)
            {
                wallThick = 200.0;// for test
                //return;
            }
            // 获取门厚度
            var realThick = GetMapWallThick(wallThick);
            double windowThick = WallWindowThickMap.Item2;
            if (realThick > 0.0)
            {
                windowThick = GetWindowThick(realThick);
            }
            else
            {
                windowThick = GetWindowThick(wallThick);
            }

            // 创建门轮廓
            if (windowThick > 0.0)
            {
                return ThDrawTool.ToRectangle(startCord.Value, endCord.Value, windowThick);
            }
            else
            {
                return null;
            }
        }
        private double GetWindowThick(double wallThick)
        {
            if (WallWindowThickMap.Item1 > 0.0)
            {
                return wallThick * WallWindowThickMap.Item2 / WallWindowThickMap.Item1;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
