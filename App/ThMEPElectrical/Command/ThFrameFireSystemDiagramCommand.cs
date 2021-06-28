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
using Autodesk.AutoCAD.EditorInput;

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

                    //火灾自动报警系统diagram实例化
                    var diagram = new ThAutoFireAlarmSystemModel();

                    //获取块引擎附加信息
                    var datas = BlockReferenceEngine.QueryAllOriginDatas();

                    //填充块数量到防火分区
                    diagram.SetGlobalBlockInfo(acadDatabase.Database,datas);

                    //初始化楼层
                    var AddFloorss = diagram.InitStoreys(
                        acadDatabase,
                        StoreysRecognitionEngine.Elements,
                        compartments);
                    
                    //绘画该图纸的防火分区编号
                    diagram.DrawFireCompartmentNum(acadDatabase.Database, AddFloorss);

                    //把楼层信息添加到系统图中
                    diagram.floors.AddRange(AddFloorss);

                    //画系统图
                    diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
                }
            }
        }
    }
}
