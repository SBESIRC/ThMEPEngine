using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using AcHelper.Commands;
using GeometryExtensions;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPEngineCore.Engine;
using DotNetARX;

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
            ThAutoFireAlarmSystemModel diagram;

            // 1 按防火分区区分   2 按回路区分
            if (FireCompartmentParameter.SystemDiagramGenerationType == 1)
            {
                diagram = new ThAutoFireAlarmSystemModelFromFireCompartment();
            }
            else
            {
                diagram = new ThAutoFireAlarmSystemModelFromWireCircuit();
            }

            //加载块集合配置文件白名单
            ThBlockConfigModel.Init();

            //选择插入点
            var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位");
            if (ppr.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
            {
                return;
            }

            //加载所有已打开的文件
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                var FileName = doc.Name.Split('\\').Last();
                if (FireCompartmentParameter.ChoiseFileNames.Count(file => string.Equals(FileName, file)) != 1)
                {
                    continue;
                }
                using (DocumentLock docLock = doc.LockDocument())
                using (var acadDatabase = Linq2Acad.AcadDatabase.Use(doc.Database))
                {
                    using (var StoreysRecognitionEngine = new ThEStoreysRecognitionEngine())//楼层引擎
                    using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())//防火分区块引擎
                    {
                        //业务逻辑，处理0图层，将0图层 解锁，解冻，打开
                        acadDatabase.Database.UnLockLayer("0");
                        acadDatabase.Database.UnFrozenLayer("0");
                        acadDatabase.Database.UnOffLayer("0");

                        var points = new Point3dCollection();

                        //获取选择区域的所有的楼层框线
                        StoreysRecognitionEngine.Recognize(acadDatabase.Database, points);
                        if (StoreysRecognitionEngine.Elements.Count == 0)
                        {
                            continue;
                        }

                        //获取选择区域的所有所需块
                        BlockReferenceEngine.Recognize(acadDatabase.Database, points);
                        BlockReferenceEngine.RecognizeMS(acadDatabase.Database, points);
                        if (BlockReferenceEngine.Elements.Count == 0)
                        {
                            continue;
                        }

                        //拿到全图所有防火分区
                        var builder = new ThFireCompartmentBuilder()
                        {
                            LayerFilter = FireCompartmentParameter.LayerNames,
                        };
                        var compartments = builder.BuildFromMS(acadDatabase.Database, points);
                        if (compartments.Count(o => o.Number.Contains("*")) > 0)
                        {
                            Active.Editor.WriteLine($"\n检测到{doc.Name}图纸有未正确命名的防火分区，请先手动命名");
                            return;
                        }

                        //获取块引擎附加信息
                        var datas = BlockReferenceEngine.QueryAllOriginDatas();

                        List<Entity> requiredElement = new List<Entity>();
                        // 1 按防火分区区分   2 按回路区分
                        if (FireCompartmentParameter.SystemDiagramGenerationType == 2)
                        {
                            var RequiredElementEngine = new ThRequiredElementRecognitionEngine();
                            var ControlCircuitEngine = new ThControlCircuitRecognitionEngine() { LayerFilter = new List<string>() { "E-FAS-WIRE" } };
                            //按回路区分需要额外拿数据
                            //拿到全图所有线
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
                    }
                }
            }
            //画系统图
            diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
        }
    }
}
