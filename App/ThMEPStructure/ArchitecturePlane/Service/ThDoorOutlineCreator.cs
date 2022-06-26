using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.ArchitecturePlane.Service
{
    internal class ThDoorOutlineCreator : ThComponentOutlineCreator
    {
        /// <summary>
        /// 墙厚度与门厚度映射
        /// </summary>
        protected Tuple<double, double> WallDoorThickMap { get; set; }
        public ThDoorOutlineCreator()
        {
            // 天华默认100的墙厚，对应30的门厚
            WallDoorThickMap = Tuple.Create(100.0, 30.0);
        }
        public override DBObjectCollection Create(List<ThComponentInfo> doors)
        {
            var results = new DBObjectCollection();
            doors.ForEach(d =>
            {
                var outline = Create(d);
                if(outline!=null)
                {
                    results.Add(outline);
                }
            });
            return results;
        }
        public void SetWallDoorThickMap(double wallThick, double doorThick)
        {
            WallDoorThickMap = Tuple.Create(wallThick, doorThick);
        }
        private Polyline Create(ThComponentInfo door)
        {
            var startCord = door.Start.ToPoint3d();
            var endCord = door.End.ToPoint3d();
            if (!startCord.HasValue && !endCord.HasValue)
            {
                return null;
            }
            // 获取构件本身的墙厚度
            var wallThick = GetWallThick(door.Thickness);
            if(wallThick ==0.0)
            {
                wallThick = 200.0;// for test
                //return null;
            }
            // 获取门厚度
            var realThick = GetMapWallThick(wallThick);
            double doorThick = WallDoorThickMap.Item2;
            if (realThick > 0.0)
            {
                doorThick = GetDoorThick(realThick);
            }
            else
            {
                doorThick = GetDoorThick(wallThick);
            }

            // 创建门轮廓
            if (doorThick>0.0)
            {
                return ThDrawTool.ToRectangle(startCord.Value, endCord.Value, doorThick);
            }
            else
            {
                return null;
            }
        }
        private double GetDoorThick(double wallThick)
        {
            if (WallDoorThickMap.Item1 > 0.0)
            {
                return wallThick * WallDoorThickMap.Item2 / WallDoorThickMap.Item1;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
