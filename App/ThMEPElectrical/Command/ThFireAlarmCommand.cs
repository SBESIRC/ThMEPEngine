using AcHelper;
using AcHelper.Commands;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using FireAlarm.Data;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPElectrical.FireAlarm.Model;
using ThMEPElectrical.SecurityPlaneSystem;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.LayoutService;
using ThMEPElectrical.SecurityPlaneSystem.AccessControlSystem.Model;
using ThMEPElectrical.SecurityPlaneSystem.StructureHandleService;
using ThMEPElectrical.StructureHandleService;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    public class ThFireAlarmCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var per = Active.Editor.GetEntity("\n选择一个框线");
                var pts = new Point3dCollection();
                if (per.Status == PromptStatus.OK)
                {
                    var frame = acadDatabase.Element<Polyline>(per.ObjectId);
                    var newFrame = ThMEPFrameService.NormalizeEx(frame);
                    pts = newFrame.VerticesEx(100.0);
                }
                else
                {
                    return;
                }

                // ArchitectureWall、Shearwall、Column、Window、Room
                // Beam、DoorOpening、Railing、FireproofShutter(防火卷帘)

                //先提取楼层框线
                var storeyExtractor = new ThEStoreyExtractor()
                {
                    ElementLayer = "AI-楼层框定E",
                    ColorIndex = 12,
                    UseDb3Engine = true,
                };
                storeyExtractor.Extract(acadDatabase.Database, pts);

                //再提取防火分区，接着用楼层框线对防火分区分组
                var storeyInfos = storeyExtractor.Storeys.Cast<StoreyInfo>().ToList();
                var fireApartExtractor = new ThFireApartExtractor()
                {
                    ElementLayer = "AI-防火分区,AD-AREA-DIVD",
                    ColorIndex = 11,
                    UseDb3Engine = false,
                    StoreyInfos = storeyInfos, //用于创建防火分区
                };
                fireApartExtractor.Extract(acadDatabase.Database, pts);
                fireApartExtractor.Group(storeyExtractor.StoreyIds); //判断防火分区属于哪个楼层框线
                fireApartExtractor.BuildFireAPartIds(); //创建防火分区编号
            }
        }
    }
}
