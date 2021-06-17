using AcHelper;
using AcHelper.Commands;
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
                var per = Active.Editor.GetEntity("\n选择一个防火分区(多段线):");
                var pts = new Point3dCollection();
                Entity frame;
                if (per.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    frame = acadDatabase.Element<Entity>(per.ObjectId);
                    if (frame is Polyline polyLineframe)
                    {
                        pts = polyLineframe.TessellatePolylineWithArc(100).Vertices();
                    }
                    else
                    {
                        Active.Editor.WriteLine("选择的不是一个多段线!");
                        return;
                    }
                }
                else
                {
                    Active.Editor.WriteLine("并未正确选择!");
                    return;
                }
                //加载块集合配置文件白名单
                ThBlockConfigModel.Init();
                //获取该区域的所有所需块
                BlockReferenceEngine.Recognize(acadDatabase.Database, pts);
                BlockReferenceEngine.RecognizeMS(acadDatabase.Database, pts);

                #region 填充进Model
                ThAutoFireAlarmSystemModel diagram = new ThAutoFireAlarmSystemModel();

                //获取块引擎附加信息
                var datas = BlockReferenceEngine.QueryAllOriginDatas();

                //填充块数量到防火分区
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
                #endregion

                //绘画该图纸的防火分区编号
                diagram.DrawFireCompartmentNum(acadDatabase.Database, diagram.GetFloorInfo());

                var ppr = Active.Editor.GetPoint("\n请选择系统图生成点位!");
                var position = Point3d.Origin;
                if (ppr.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    position = ppr.Value;
                }

                //画系统图
                diagram.DrawSystemDiagram(position.GetAsVector(), Active.Editor.UCS2WCS());
            }
        }


    }
}
