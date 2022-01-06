using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using AcHelper.Commands;
using GeometryExtensions;
using ThMEPEngineCore.Engine;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPElectrical.SystemDiagram.Extension;
using NFox.Cad;
using ThMEPEngineCore.Command;
using Autodesk.AutoCAD.ApplicationServices;

namespace ThMEPElectrical.Command
{
    class ThFrameFireSystemDiagramCommand : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
        }
        public ThFrameFireSystemDiagramCommand()
        {
            this.ActionName="火灾报警系统图-手动选择楼层";
            this.CommandName="THHZXTF";
        }
        public override void SubExecute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                using (var StoreysRecognitionEngine = new ThEStoreysRecognitionEngine())//楼层引擎
                {
                    //选择区域
                    Active.Editor.WriteLine("\n请选择楼层块");
                    var result = Active.Editor.GetSelection();
                    if (result.Status != PromptStatus.OK)
                    {
                        return;
                    }
                    var objs = new ObjectIdCollection();
                    objs = result.Value.GetObjectIds().ToObjectIdCollection();

                    //选择插入点
                    var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位");
                    if (ppr.Status != PromptStatus.OK)
                    {
                        return;
                    }
                    FireCompartmentParameter.WarningCache.Clear();
                    //业务逻辑，处理0图层，将0图层 解锁，解冻，打开
                    acadDatabase.Database.UnLockLayer("0");
                    acadDatabase.Database.UnFrozenLayer("0");
                    acadDatabase.Database.UnOffLayer("0");

                    //选择区域
                    var points = new Point3dCollection();

                    //楼层
                    StoreysRecognitionEngine.RecognizeMS(acadDatabase.Database, objs);
                    if (StoreysRecognitionEngine.Elements.Count == 0)
                    {
                        return;
                    }

                    //加载块集合配置文件白名单
                    ThBlockConfigModel.Init();

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
                    if (compartments.Count(o => o.Number.Contains("*")) > 0)
                    {
                        Active.Editor.WriteLine("\n检测到有未正确命名的防火分区，请先手动命名");
                        return;
                    }
                    if (builder.InvalidResults.Count > 0)
                    {
                        foreach (var invalid in builder.InvalidResults)
                        {
                            Active.Editor.WriteLine($"\n检测到有防火分区内有不止一个命名，请先手动处理。名称[{string.Join(",", invalid.Value)}]");
                        }
                        return;
                    }

                    //获取块引擎附加信息
                    var datas = BlockReferenceEngine.QueryAllOriginDatas();


                    var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
                    var labelDB = labelEngine.Extract(acadDatabase.Database, points);
                    var labelLine = labelEngine.CreateLabelLineList();//----12s----

                    var textEngine = new ThExtractLabelText();//提取文字
                    var textCollection = textEngine.Extract(acadDatabase.Database, points);
                    //var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textCollection);

                    ThQuantityMarkExtension.ReSet();
                    ThQuantityMarkExtension.SetGlobalLineData(labelLine);
                    ThQuantityMarkExtension.SetGlobalMarkData(textCollection);
                    ThQuantityMarkExtension.SetGlobalBlockIOData(datas.Where(o => o.Value.Count == 1 && o.Value[0].Key == "F" && o.Value[0].Value == "I/O").Select(o => acadDatabase.Database.GetBlockReferenceOBB(o.Key as BlockReference)).ToCollection());


                    //火灾自动报警系统diagram实例化
                    ThAutoFireAlarmSystemModel diagram;
                    List<Entity> requiredElement = new List<Entity>();

                    // 1 按防火分区区分   2 按回路区分
                    if (FireCompartmentParameter.SystemDiagramGenerationType == 1)
                    {
                        diagram = new ThAutoFireAlarmSystemModelFromFireCompartment();
                    }
                    else
                    {
                        FireCompartmentParameter.WarningCache.Add(
                            new AlarmModel() { Doc =  acadDatabase.Database.GetDocument() 
                            });
                        diagram = new ThAutoFireAlarmSystemModelFromWireCircuit();
                        var RequiredElementEngine = new ThRequiredElementRecognitionEngine();
                        var ControlCircuitEngine = new ThControlCircuitRecognitionEngine() { LayerFilter = new List<string>() { "E-FAS-WIRE" } };
                        //按回路区分需要额外拿数据
                        //拿到全图所有线
                        ControlCircuitEngine.Recognize(acadDatabase.Database, points);
                        ControlCircuitEngine.RecognizeMS(acadDatabase.Database, points);
                        //获取选择连接关系区域其他的块
                        RequiredElementEngine.Recognize(acadDatabase.Database, points);
                        RequiredElementEngine.RecognizeMS(acadDatabase.Database, points);

                        //填充到
                        requiredElement.AddRange(ControlCircuitEngine.Elements.Select(o => o.Geometry));
                        requiredElement.AddRange(BlockReferenceEngine.Elements.Select(o => o.Outline));
                        requiredElement.AddRange(RequiredElementEngine.Elements.Select(o => o.Outline));
                    }

                    //填充块数量到防火分区
                    diagram.SetGlobalData(acadDatabase.Database, datas, requiredElement);
                    //初始化楼层(按防火分区划分)
                    var AddFloorss = diagram.InitStoreys(
                        acadDatabase,
                        StoreysRecognitionEngine.Elements,
                        compartments);

                    //绘画该图纸的防火分区编号
                    diagram.DrawFloorsNum(acadDatabase.Database, AddFloorss);

                    //把楼层信息添加到系统图中
                    diagram.floors.AddRange(AddFloorss);

                    //画系统图
                    diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
                    diagram.DrawAlarm();
                }
            }
        }
    }
}
