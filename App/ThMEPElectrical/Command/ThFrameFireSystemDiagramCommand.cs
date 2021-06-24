using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using AcHelper.Commands;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model.Electrical;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;

namespace ThMEPElectrical.Command
{
    class ThFrameFireSystemDiagramCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }
        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                using (var StoreysRecognitionEngine = new ThStoreysRecognitionEngine())//楼层引擎
                {
                    //选择区域
                    var points = SelectFrame();
                    if (points.Count == 0)
                    {
                        return;
                    }

                    //选择插入点
                    var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位!");
                    if (ppr.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                    {
                        return;
                    }

                    //楼层
                    StoreysRecognitionEngine.Recognize(acadDatabase.Database, points);
                    if (StoreysRecognitionEngine.Elements.Count == 0)
                    {
                        return;
                    }
                    //图块
                    BlockReferenceEngine.Recognize(acadDatabase.Database, points);
                    BlockReferenceEngine.RecognizeMS(acadDatabase.Database, points);
                    if (BlockReferenceEngine.Elements.Count == 0)
                    {
                        return;
                    }

                    //防火分区
                    var builder = new ThFireCompartmentBuilder()
                    {
                        LayerFilter = FireCompartmentParameter.LayerNames,
                    };
                    var compartments = builder.BuildFromMS(acadDatabase.Database, points);

                    //加载块集合配置文件白名单
                    ThBlockConfigModel.Init();

                    //初始化楼层
                    var diagram = new ThAutoFireAlarmSystemModel();
                    var AddFloorss = diagram.InitStoreys(
                        acadDatabase,
                        StoreysRecognitionEngine.Elements,
                        compartments);

                    //获取块引擎附加信息
                    var datas = BlockReferenceEngine.QueryAllOriginDatas();

                    //填充块数量到防火分区
                    diagram.SetGlobalBlockInfo(acadDatabase.Database,datas);
                    AddFloorss.ForEach(floor =>
                    {
                        var FloorBlockInfo = diagram.GetFloorBlockInfo(floor.FloorBoundary);
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

                    //画系统图
                    diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
                }
            }
        }
        private Point3dCollection SelectFrame()
        {
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window))
            {
                try
                {
                    pc.Collect();
                    var frame = new Polyline();
                    Point3dCollection winCorners = pc.CollectedPoints;
                    frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                    frame.TransformBy(Active.Editor.UCS2WCS());
                    return frame.Vertices();
                }
                catch
                {
                    return new Point3dCollection();
                }
            }
        }
    }
}
