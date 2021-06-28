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
using ThMEPEngineCore.Engine;
using ThMEPElectrical.SystemDiagram.Service;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using System.Linq;

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

                //防火分区
                var builder = new ThFireCompartmentBuilder()
                {
                    LayerFilter = FireCompartmentParameter.LayerNames,
                };
                var compartments = builder.BuildFromMS(acadDatabase.Database, objs);
                if(compartments.Count==0)
                {
                    return;
                }

                //加载块集合配置文件白名单
                ThBlockConfigModel.Init();

                //获取该区域的所有所需块
                var dbobjs =new DBObjectCollection();
                foreach (ObjectId obj in objs)
                {
                    dbobjs.Add(acadDatabase.Element<Entity>(obj));
                }
                var Rectangle = dbobjs.GeometricExtents().ToRectangle();
                var pts = Rectangle.Vertices();
                BlockReferenceEngine.Recognize(acadDatabase.Database, pts);
                BlockReferenceEngine.RecognizeMS(acadDatabase.Database, pts);

                
                //填充块数量到防火分区
                var diagram = new ThAutoFireAlarmSystemModel();
                //获取块引擎附加信息
                var datas = BlockReferenceEngine.QueryAllOriginDatas();
                diagram.SetGlobalBlockInfo(acadDatabase.Database, datas);
                //初始化虚假楼层
                var AddFloorss = diagram.InitStoreys(
                    acadDatabase,
                    Rectangle,
                    compartments);

                AddFloorss.ForEach(floor =>
                {
                    var FloorBlockInfo = diagram.GetFloorBlockInfo(floor.FloorBoundary);
                    floor.FireDistricts.ForEach(fireDistrict =>
                    {
                        fireDistrict.Data = new DataSummary()
                        {
                            BlockData = diagram.FillingBlockNameConfigModel(fireDistrict.FireDistrictBoundary)
                        };
                    });
                    int Max_FireDistrictNo = 0;
                    var choise = floor.FireDistricts.Where(f => f.FireDistrictNo == -2);//.OrderByDescending(f => f.FireDistrictNo).FirstOrDefault();
                    if (choise.Count() > 0)
                    {
                        var The_MaxNo_FireDistrict = choise.Max(o => int.Parse(o.FireDistrictName.Split('-')[1]));
                        Max_FireDistrictNo = The_MaxNo_FireDistrict;
                    }
                    string FloorName = "*";
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
}
