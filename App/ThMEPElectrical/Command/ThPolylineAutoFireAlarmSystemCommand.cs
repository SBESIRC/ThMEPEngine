using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using System;
using ThCADExtension;
using ThMEPElectrical.SystemDiagram.Engine;
using ThMEPElectrical.SystemDiagram.Model;

namespace ThMEPElectrical.Command
{
    public class ThPolylineAutoFireAlarmSystemCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var BlockReferenceEngine = new ThAutoFireAlarmSystemRecognitionEngine())
            {
                //选择防火分区
                var per = Active.Editor.GetEntity("\n选择一个防火分区(多段线)");
                if (per.Status != PromptStatus.OK)
                {
                    return;
                }
                var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                var pts = frame.TessellatePolylineWithArc(100).Vertices();

                //选择插入点
                var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位");
                if (ppr.Status != PromptStatus.OK)
                {
                    return;
                }

                //获取该区域的所有所需块
                BlockReferenceEngine.Recognize(acadDatabase.Database, pts);
                BlockReferenceEngine.RecognizeMS(acadDatabase.Database, pts);
                if (BlockReferenceEngine.Elements.Count == 0)
                {
                    return;
                }

                //加载块集合配置文件白名单
                ThBlockConfigModel.Init();

                //填充块数量到防火分区
                var diagram = new ThAutoFireAlarmSystemModel();
                //获取块引擎附加信息
                var datas = BlockReferenceEngine.QueryAllOriginDatas();
                diagram.SetGlobalBlockInfo(datas);
                //添加一个楼层信息
                diagram.floors.Add(new ThFloorModel()
                {
                    FloorNumber = 1
                });
                //添加一个防火分区
                var FloorBlockInfo = diagram.GetFloorBlockInfo(frame as Polyline);
                diagram.floors[0].FireDistricts.Add(new ThFireDistrictModel
                {
                    FireDistrictName = "Select",
                    Data = new DataSummary()
                    {
                        BlockData = diagram.FillingBlockNameConfigModel(frame)
                    }
                });

                //绘画该图纸的防火分区编号
                diagram.DrawFireCompartmentNum(acadDatabase.Database, diagram.GetFloorInfo());

                //画系统图
                diagram.DrawSystemDiagram(ppr.Value.GetAsVector(), Active.Editor.UCS2WCS());
            }
        }
    }
}
