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
    class ThFrameFireSystemDiagramCommand : IAcadCommand, IDisposable
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

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                using (var StoreysRecognitionEngine = new ThStoreysRecognitionEngine())//楼层引擎
                using (var FireCompartmentEngine = new ThFireCompartmentRecognitionEngine() { LayerFilter = FireCompartmentParameter.LayerNames })//防火分区引擎
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
                    FireCompartmentEngine.RecognizeMS(acadDatabase.Database, points);

                    //获取选择区域的所有所需块
                    BlockReferenceEngine.Recognize(acadDatabase.Database, points);
                    BlockReferenceEngine.RecognizeMS(acadDatabase.Database, points);

                    //获取选择区域的所有的楼层框线
                    StoreysRecognitionEngine.Recognize(acadDatabase.Database, points);

                    //初始化楼层
                    var AddFloorss = diagram.InitStoreys(acadDatabase, StoreysRecognitionEngine.Elements, FireCompartmentEngine.Elements.Cast<ThFireCompartment>().ToList());

                    //获取块引擎附加信息
                    var datas = BlockReferenceEngine.QueryAllOriginDatas();

                    //填充块数量到防火分区
                    diagram.SetGlobalBlockInfo(datas);
                    AddFloorss.ForEach(floor =>
                    {
                        var FloorBlockInfo = diagram.GetFloorBlockInfo(floor.FloorBoundary);
                        //在这里可以加OrderBy
                        floor.FireDistricts.ForEach(fireDistrict =>
                        {
                            fireDistrict.Data = new DataSummary()
                            {
                                BlockData = diagram.FillingBlockNameConfigModel(fireDistrict.FireDistrictBoundary, floor.FloorName == "JF")
                            };
                            fireDistrict.DrawFireDistrict = fireDistrict.Data.BlockData.BlockStatistics.Values.Count(v => v > 0) > 0;
                        });
                        int Max_FireDistrictNo = 1;
                        //Max_FireDistrictNo = floor.FireDistricts.OrderByDescending(f=>f.FireDistrictNo).FirstOrDefault().FireDistrictNo+1;
                        var The_MaxNo_FireDistrict = floor.FireDistricts.OrderByDescending(f => f.FireDistrictNo).FirstOrDefault();
                        Max_FireDistrictNo = The_MaxNo_FireDistrict.FireDistrictNo;
                        string FloorName = Max_FireDistrictNo > 1 ? The_MaxNo_FireDistrict.FireDistrictName.Split('-')[0] : floor.FloorName;
                        floor.FireDistricts.Where(f => f.DrawFireDistrict && f.DrawFireDistrictNameText).ToList().ForEach(o =>
                        {
                            o.FireDistrictNo = ++Max_FireDistrictNo;
                            o.FireDistrictName = FloorName + "-" + Max_FireDistrictNo;
                        });
                    });

                    //绘画该图纸的防火分区编号
                    diagram.DrawFireCompartmentNum(acadDatabase.Database, AddFloorss);

                    //把楼层信息添加到系统图中
                    diagram.floors.AddRange(AddFloorss);

                    var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位!");
                    var position = Point3d.Origin;
                    if (ppr.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                    {
                        position = ppr.Value;
                    }

                    //画系统图
                    diagram.DrawSystemDiagram(position.GetAsVector());
                }
            }
        }
    }
}
