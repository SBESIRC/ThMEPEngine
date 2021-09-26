using Linq2Acad;
using Catel.Collections;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public abstract class ThSprinklerChecker
    {
        public ThMEPDataSet DataSet { get; set; }

        // 校核喷头类型
        public string Category { get; }
        // 半径A
        public int RadiusA { get; }
        // 半径B
        public int RadiusB { get; }
        // 梁高
        public int BeamHeight { get; }
        // 三连喷头间距阈值
        // public int Spacing { get; }

        public ThSprinklerChecker()
        {
            //
        }

        public ThSprinklerChecker(string category, string level, string scope, int beamHeight)
        {
            Category = category;
            BeamHeight = beamHeight;
            if (scope == "标准覆盖")
            {
                switch (level)
                {
                    case "轻危险级":
                        RadiusA = 3111;
                        RadiusB = 2200;
                        break;
                    case "中危险级Ⅰ级":
                        RadiusA = 2546;
                        RadiusB = 1800;
                        break;
                    case "中危险级Ⅱ级":
                        RadiusA = 2404;
                        RadiusB = 1700;
                        break;
                    case "严重危险级、仓库危险级":
                        RadiusA = 2121;
                        RadiusB = 1500;
                        break;
                    default:
                        break;
                }
            }
            else if (scope == "扩大覆盖")
            {
                switch (level)
                {
                    case "轻危险级":
                        RadiusA = 3818;
                        RadiusB = 2700;
                        break;
                    case "中危险级Ⅰ级":
                        RadiusA = 3394;
                        RadiusB = 2400;
                        break;
                    case "中危险级Ⅱ级":
                        RadiusA = 2970;
                        RadiusB = 2100;
                        break;
                    case "严重危险级、仓库危险级":
                        RadiusA = 2546;
                        RadiusB = 1800;
                        break;
                    default:
                        break;
                }
            }
        }

        public abstract void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries);

        public void Present(HashSet<Line> result, ObjectId layerId)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var style = "TH-DIM100-W";
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, acadDatabase.Database);
                result.ForEach(o =>
                {
                    var alignedDimension = new AlignedDimension
                    {
                        XLine1Point = o.StartPoint,
                        XLine2Point = o.EndPoint,
                        DimensionText = "",
                        DimLinePoint = ThSprinklerUtils.VerticalPoint(o.StartPoint, o.EndPoint, 2000.0),
                        ColorIndex = 256,
                        DimensionStyle = id,
                        LayerId = layerId,
                        Linetype = "ByLayer"
                    };

                    acadDatabase.ModelSpace.Add(alignedDimension);
                });
            }
        }
    }
}