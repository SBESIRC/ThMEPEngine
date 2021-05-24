using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Model.WireCircuit;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.Command
{
    class ThWholeFireSystemDiagramCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }
        private static Tuple<Point3d, Point3d> SelectPoints()
        {
            var ptLeftRes = Active.Editor.GetPoint("\n请您框选范围，先选择左上角点");
            Point3d leftDownPt = Point3d.Origin;
            if (ptLeftRes.Status == PromptStatus.OK)
            {
                leftDownPt = ptLeftRes.Value;
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }

            var ptRightRes = Active.Editor.GetCorner("\n再选择右下角点", leftDownPt);
            if (ptRightRes.Status == PromptStatus.OK)
            {
                return Tuple.Create(leftDownPt, ptRightRes.Value);
            }
            else
            {
                return Tuple.Create(leftDownPt, leftDownPt);
            }
        }

        /// <summary>
        /// 计算块的实际坐标
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="BlockInfo"></param>
        /// <returns></returns>
        public Point3d CalculateCoordinates(int rowIndex, ThBlockModel BlockInfo)
        {
            return new Point3d(3000 * (BlockInfo.Index - 1) + BlockInfo.Position.X, 3000 * (rowIndex - 1) + BlockInfo.Position.Y, 0);
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                using (var StoreysRecognitionEngine = new ThStoreysRecognitionEngine())//楼层引擎
                using (var FireCompartmentEngine = new ThFireCompartmentRecognitionEngine() { LayerFilter = new List<string>() { ThAutoFireAlarmSystemCommon.FireDistrictByLayer } })//防火分区引擎
                {
                    //火灾自动报警系统diagram实例化
                    ThAutoFireAlarmSystemModel diagram = new ThAutoFireAlarmSystemModel();

                    //加载块集合配置文件白名单
                    ThBlockConfigModel.Init();

                    #region 选择区域
                    var input = SelectPoints();
                    var points = new Point3dCollection();
                    points.Add(input.Item1);
                    points.Add(new Point3d(input.Item1.X, input.Item2.Y, 0));
                    points.Add(input.Item2);
                    points.Add(new Point3d(input.Item2.X, input.Item1.Y, 0));
                    #endregion

                    //拿到全图所有防火分区
                    FireCompartmentEngine.RecognizeMS(acadDatabase.Database, new Point3dCollection());
                    //foreach (var item in FireCompartmentEngine.Elements.Cast<ThFireCompartment>().ToList())
                    //{
                    //    item.Boundary.ColorIndex = 2;
                    //    acadDatabase.ModelSpace.Add(item.Boundary);
                    //}

                    //获取选择区域的所有所需块
                    BlockReferenceEngine.Recognize(acadDatabase.Database, points);

                    //获取选择区域的所有的楼层框线
                    StoreysRecognitionEngine.Recognize(acadDatabase.Database, points);

                    //初始化楼层
                    diagram.InitStoreys(StoreysRecognitionEngine.Elements, FireCompartmentEngine.Elements.Cast<ThFireCompartment>().ToList());

                    //填充块数量到防火分区
                    diagram.GetFloorInfo().ForEach(floor =>
                    {
                        //在这里可以加OrderBy
                        floor.FireDistricts.ForEach(fireDistrict =>
                        {
                            fireDistrict.Data = new DataSummary()
                            {
                                BlockData = BlockReferenceEngine.FillingBlockNameConfigModel(fireDistrict.FireDistrictBoundary)
                            };
                        });
                    });

                    #region 填充进Model
                    //1
                    //FireStoreys.ToList().ForEach(o =>
                    //{
                    //    var FireDistrict = new ThFireDistrict
                    //    {
                    //        FireDistrictName = o.Key.FloorName,
                    //        Data = new DataSummary()
                    //        {
                    //            BlockData = dataEngine.FillingBlockNameConfigModel(o.Value)
                    //        }
                    //    };
                    //    o.Key.FireDistricts.Add(FireDistrict);
                    //    diagram.floors.Add(o.Key);
                    //});

                    //2
                    //diagram.GetFireDistrictsInfo().ForEach(FireDistricts =>
                    //{
                    //    FireDistricts.Data = new DataSummary()
                    //    {
                    //        BlockData = dataEngine.FillingBlockNameConfigModel(FireDistricts.PointCollection)
                    //    };
                    //});

                    //diagram.GetFloorInfo().ForEach(floor =>
                    //{
                    //            //在这里可以加OrderBy
                    //            floor.FireDistricts.ForEach(fireDistrict =>
                    //                {
                    //        fireDistrict.Data = new DataSummary()
                    //        {
                    //            BlockData = dataEngine.FillingBlockNameConfigModel(fireDistrict.FireDistrictPolyLine)
                    //        };
                    //    });
                    //});
                    #endregion

                    //画
                    diagram.Draw();
                }
            }
        }
    }
}
