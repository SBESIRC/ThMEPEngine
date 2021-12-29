using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 根据中心线和宽度创建边线，
    /// 返回中心点对应的两边的线
    /// </summary>
    public class ThFindCenterPairService
    {
        private double width;
        private double ShortLinkLineLength = 100.0;        
        private List<Line> centers = new List<Line>();
        private ThFindCenterPairService(List<Line> centers, double width)
        {
            this.width = width;
            this.centers = centers;
        }

        public static Dictionary<Line, Tuple<List<Line>, List<Line>>> Find(List<Line> centers, double width)
        {
            var instance = new ThFindCenterPairService(centers, width);
            return instance.Find();
        }

        private Dictionary<Line, Tuple<List<Line>, List<Line>>> Find()
        {
            // 创建边线
            var sideLines = CreateSides(centers, width);
            //sideLines.Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 5);

            // 处理边线
            var newSideLines = Handle(sideLines);
            //newSideLines.Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 5);

            // 返回中先线对应的边线
            return FindPairs(centers,newSideLines,width);
        }

        private List<Line> CreateSides(List<Line> lines, double width)
        {
            return ThLightSideLineCreator.Create(lines, width);
        }

        private List<Line> Handle(List<Line> sideLines)
        {
            var handler = new ThLightSideLineHandler(ShortLinkLineLength);
            return handler.Handle(sideLines.ToCollection()).OfType<Line>().ToList();
        }

        private Dictionary<Line, Tuple<List<Line>, List<Line>>> FindPairs(List<Line> centers, List<Line> sides, double width)
        {
            var sideParameter = new ThFindSideLinesParameter
            {
                CenterLines = centers,
                SideLines = sides,
                HalfWidth = width / 2.0
            };
            //查找合并线buffer后，获取中心线对应的两边线槽线
            var instane = new ThFindSideLinesService(sideParameter);
            return instane.FindSides();
        }
    }
}
