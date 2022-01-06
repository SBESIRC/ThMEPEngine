using System;
using AcHelper;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using AcHelper.Commands;
using GeometryExtensions;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.SystemDiagram.Model;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Service;
using ThMEPElectrical.SystemDiagram.Extension;
using ThMEPEngineCore.Command;

namespace ThMEPElectrical.Command
{
    public class ThPolylineAutoFireAlarmSystemCommand : ThMEPBaseCommand, IDisposable
    {
        public ThPolylineAutoFireAlarmSystemCommand()
        {
            this.ActionName="火灾报警系统图-选择防火分区";
            this.CommandName="THHZXTP";
        }

        public void Dispose()
        {
            //
        }

        public override void SubExecute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())
            {
                //选择防火分区
                Active.Editor.WriteLine("\n请选择防火分区(多段线)");
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
                //防火分区
                var builder = new ThFireCompartmentBuilder()
                {
                    LayerFilter = FireCompartmentParameter.LayerNames,
                };
                var compartments = builder.BuildFromMS(acadDatabase.Database, objs);
                if (compartments.Count == 0)
                {
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

                //加载块集合配置文件白名单
                ThBlockConfigModel.Init();

                //获取该区域的所有所需块
                var dbobjs = new DBObjectCollection();
                foreach (ObjectId obj in objs)
                {
                    dbobjs.Add(acadDatabase.Element<Entity>(obj));
                }
                var Rectangle = dbobjs.GeometricExtents().ToRectangle();
                var pts = Rectangle.Vertices();
                BlockReferenceEngine.Recognize(acadDatabase.Database, pts);
                BlockReferenceEngine.RecognizeMS(acadDatabase.Database, pts);

                //填充块数量到防火分区
                ThAutoFireAlarmSystemModel diagram = new ThAutoFireAlarmSystemModelFromFireCompartment();

                //获取块引擎附加信息
                var datas = BlockReferenceEngine.QueryAllOriginDatas();

                var labelEngine = new ThExtractLabelLine();//提取消火栓标记线
                var labelDB = labelEngine.Extract(acadDatabase.Database, pts);
                var labelLine = labelEngine.CreateLabelLineList();//----12s----

                var textEngine = new ThExtractLabelText();//提取文字
                var textCollection = textEngine.Extract(acadDatabase.Database, pts);
                //var textSpatialIndex = new ThCADCoreNTSSpatialIndex(textCollection);

                ThQuantityMarkExtension.ReSet();
                ThQuantityMarkExtension.SetGlobalLineData(labelLine);
                ThQuantityMarkExtension.SetGlobalMarkData(textCollection);
                ThQuantityMarkExtension.SetGlobalBlockIOData(datas.Where(o => o.Value.Count == 1 && o.Value[0].Key == "F" && o.Value[0].Value == "I/O").Select(o => acadDatabase.Database.GetBlockReferenceOBB(o.Key as BlockReference)).ToCollection());

                //填充块数量到防火分区
                diagram.SetGlobalData(acadDatabase.Database, datas, null);

                //初始化虚假楼层
                var AddFloorss = diagram.InitVirtualStoreys(
                    acadDatabase.Database,
                    Rectangle,
                    compartments);

                //绘画该图纸的防火分区编号
                diagram.DrawFloorsNum(acadDatabase.Database, AddFloorss);

                //把楼层信息添加到系统图中
                diagram.floors.AddRange(AddFloorss);

                //画系统图
                diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
            }
        }
    }
}
