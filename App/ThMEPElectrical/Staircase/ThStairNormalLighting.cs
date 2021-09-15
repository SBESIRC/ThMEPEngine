using System.Linq;
using ThMEPElectrical.Stair;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Staircase
{
    public class ThStairNormalLighting
    {
        private readonly string PLAT_LAYOUT_EQUIPMENT = "E-BL302";

        public Dictionary<Point3d, double> Layout(Database database, Point3dCollection points, double scale)
        {
            // 提取楼梯块
            var engine = new ThDB3StairRecognitionEngine();
            engine.Recognize(database, points);
            var stairs = engine.Elements.Cast<ThIfcStair>().ToList();

            // 计算布置位置
            return Lay(stairs, scale);
        }

        public Dictionary<Point3d, double> Lay(List<ThIfcStair> stairs, double scale)
        {
            var dictionary = new Dictionary<Point3d, double>();
            stairs.ForEach(stair =>
            {
                var doorsEngine = new ThStairDoorService();
                var doors = doorsEngine.GetDoorList(stair.SrcBlock);
                var layoutEngine = new ThStairElectricalEngine();
                var angle = 0.0;
                if (stair.PlatForLayout.Count != 0)
                {
                    var position = layoutEngine.Displacement(stair.PlatForLayout, doors, PLAT_LAYOUT_EQUIPMENT, scale, ref angle);
                    dictionary.Add(position, angle);
                }
                if (stair.HalfPlatForLayout.Count != 0)
                {
                    var position = layoutEngine.Displacement(stair.HalfPlatForLayout, doors, PLAT_LAYOUT_EQUIPMENT, scale, ref angle);
                    dictionary.Add(position, angle);
                }
            });
            return dictionary;
        }
    }
}
