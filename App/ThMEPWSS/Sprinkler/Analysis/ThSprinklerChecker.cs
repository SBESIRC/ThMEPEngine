using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;
using System.Linq;
using DotNetARX;
using ThCADCore.NTS;
using NFox.Cad;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public abstract class ThSprinklerChecker
    {
        // 校核喷头类型
        public string Category { get; set; }
        // 半径A
        public int RadiusA { get; set; }
        // 半径B
        public int RadiusB { get; set; }
        // 梁高
        public int BeamHeight { get; set; }
        // 三连喷头间距阈值

        public ThSprinklerChecker()
        {
            //
        }

        public abstract void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline);

        public abstract void Clean(Polyline pline);

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

        public void CleanDimension(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = acadDatabase.ModelSpace
                    .OfType<AlignedDimension>()
                    .Where(o => o.Layer == layerName).ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                spatialIndex.SelectCrossingPolygon(bufferPoly)
                            .OfType<AlignedDimension>()
                            .ToList()
                            .ForEach(o =>
                            {
                                o.UpgradeOpen();
                                o.Erase();
                            });
            }
        }

        public void CleanPline(string layerName, Polyline polyline)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.UnFrozenLayer(layerName);
                acadDatabase.Database.UnLockLayer(layerName);
                acadDatabase.Database.UnOffLayer(layerName);

                var objs = acadDatabase.ModelSpace
                    .OfType<Polyline>()
                    .Where(o => o.Layer == layerName).ToCollection();
                var bufferPoly = polyline.Buffer(1)[0] as Polyline;
                var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                spatialIndex.SelectCrossingPolygon(bufferPoly)
                            .OfType<Polyline>()
                            .ToList()
                            .ForEach(o =>
                            {
                                o.UpgradeOpen();
                                o.Erase();
                            });
            }
        }
    }

    public static class DangerGradeDataManager
    {
        public static List<DangerGradeData> Datas;
        static DangerGradeDataManager()
        {
            Datas = new List<DangerGradeData>();
            Datas.Add(new DangerGradeData { DangerGrade = "轻危险级", Range = "标准覆盖", A = 3111, B = 2200 });
            Datas.Add(new DangerGradeData { DangerGrade = "中危险级Ⅰ级", Range = "标准覆盖", A = 2546, B = 1800 });
            Datas.Add(new DangerGradeData { DangerGrade = "中危险级Ⅱ级", Range = "标准覆盖", A = 2404, B = 1700 });
            Datas.Add(new DangerGradeData { DangerGrade = "严重危险级、仓库危险级", Range = "标准覆盖", A = 2121, B = 1500 });

            Datas.Add(new DangerGradeData { DangerGrade = "轻危险级", Range = "扩大覆盖", A = 3818, B = 2700 });
            Datas.Add(new DangerGradeData { DangerGrade = "中危险级Ⅰ级", Range = "扩大覆盖", A = 3394, B = 2400 });
            Datas.Add(new DangerGradeData { DangerGrade = "中危险级Ⅱ级", Range = "扩大覆盖", A = 2970, B = 2100 });
            Datas.Add(new DangerGradeData { DangerGrade = "严重危险级、仓库危险级", Range = "扩大覆盖", A = 2546, B = 1800 });
        }
        public static DangerGradeData Query(string dangerGrade, string range)
        {
            return Datas.FirstOrDefault(d => d.DangerGrade == dangerGrade && d.Range == range);
        }
    }

    public class DangerGradeData
    {
        public string DangerGrade { get; set; } = "";
        public string Range { get; set; } = "";
        public int A { get; set; }
        public int B { get; set; }
        public DangerGradeData()
        {
        }
    }
}