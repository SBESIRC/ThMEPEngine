using System;
using AcHelper;
using System.Linq;
using AcHelper.Commands;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.Command
{
    class ThAllDrawingsFireSystemDiagramCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            //火灾自动报警系统diagram实例化
            ThAutoFireAlarmSystemModel diagram = new ThAutoFireAlarmSystemModel();

            //加载块集合配置文件白名单
            ThBlockConfigModel.Init();

            //选择插入点
            var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位!");
            if (ppr.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return;
            }

            //加载所有已打开的文件
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                using (DocumentLock docLock = doc.LockDocument())
                using (var acadDatabase = Linq2Acad.AcadDatabase.Use(doc.Database))
                {
                    using (var StoreysRecognitionEngine = new ThStoreysRecognitionEngine())//楼层引擎
                    using (var FireCompartmentEngine = new ThFireCompartmentRecognitionEngine() { LayerFilter = FireCompartmentParameter.LayerNames })//防火分区引擎
                    using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                    {
                        var points = new Point3dCollection();

                        //获取选择区域的所有的楼层框线
                        StoreysRecognitionEngine.Recognize(acadDatabase.Database, points);

                        //拿到全图所有防火分区
                        FireCompartmentEngine.RecognizeMS(acadDatabase.Database, points);

                        //获取选择区域的所有所需块
                        BlockReferenceEngine.Recognize(acadDatabase.Database, points);
                        BlockReferenceEngine.RecognizeMS(acadDatabase.Database, points);

                        //初始化楼层
                        var AddFloorss = diagram.InitStoreys(
                            acadDatabase,
                            StoreysRecognitionEngine.Elements,
                            FireCompartmentEngine.Elements.Cast<ThFireCompartment>().ToList());

                        //获取块引擎附加信息
                        var datas = BlockReferenceEngine.QueryAllOriginDatas();

                        //填充块数量到防火分区
                        diagram.SetGlobalBlockInfo(acadDatabase.Database,datas);
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
                                fireDistrict.DrawFireDistrict = fireDistrict.Data.BlockData.BlockStatistics.Values.Count(v => v > 0) > 3;
                            });
                            int Max_FireDistrictNo = 1;
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
                    }
                }
            }
            //画系统图
            diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
        }
    }
}
