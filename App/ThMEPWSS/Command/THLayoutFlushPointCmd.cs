using System;
using AcHelper.Commands;

#if ACAD2016
using CLI;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using ThMEPWSS.FlushPoint;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPWSS.FlushPoint.Data;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPWSS.FlushPoint.Service;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace ThMEPWSS.Command
{
    public class THLayoutFlushPointCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public void Execute()
        {
#if ACAD2016
            using (var acadDb = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n选择一个范围框");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                var entity = acadDb.Element<Entity>(result.ObjectId);
                if (!(entity is Polyline))
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(entity as Polyline);
                var pts = nFrame.VerticesEx(100.0);

                //收集数据
                var roomExtractor = new ThRoomExtractor() { ColorIndex = 6, };
                roomExtractor.Extract(acadDb.Database, pts);
                var extractors = new List<ThExtractorBase>()
                {
                    new ThColumnExtractor(){ ColorIndex=1,},
                    new ThShearwallExtractor(){ ColorIndex=2,},
                    new ThArchitectureExtractor(){ ColorIndex=3,},
                    new ThObstacleExtractor(){ ColorIndex=4,},
                    new ThDrainFacilityExtractor(){ ColorIndex=5,},
                };
                extractors.ForEach(o => o.SetRooms(roomExtractor.Rooms));
                extractors.ForEach(o => o.Extract(acadDb.Database, pts));

                var geos = new List<ThGeometry>();
                extractors.Add(roomExtractor);
                extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));

                //extractors.ForEach(o => (o as IPrint).Print(acadDb.Database));
                //ThFlushPointUtils.OutputGeo(Active.Document.Name, geos);

                var washPara = BuildWashParam(); //UI参数
                var geoContent = ThGeoOutput.Output(geos); //数据
                var washData = new ThWashGeoData();
                washData.ReadFromContent(geoContent);

                var washPoint = new ThWashPointLayoutEngine();
                double[] points = washPoint.Layout(washData, washPara);
                var washPoints = ThFlushPointUtils.GetPoints(points);

                // 打印块
                var columns = (extractors[0] as ThColumnExtractor).Columns;
                var walls = new List<Entity>();
                walls.AddRange((extractors[1] as ThShearwallExtractor).Walls);
                walls.AddRange((extractors[2] as ThArchitectureExtractor).Walls);
                var layoutData = new WashPointLayoutData()
                {
                    Columns = columns.Cast<Entity>().ToList(),
                    Walls = walls,
                    WashPointBlkName = "给水角阀平面",
                    WashPointLayerName= "W-WSUP-EQPM",
                    WashPoints= washPoints,
                    Db= acadDb.Database,
                    PtRange=5.0,
                };
                var layoutService = new ThLayoutWashPointBlockService(layoutData);
                layoutService.Layout();
            }
#endif
        }

#if ACAD2016
        private ThWashParam BuildWashParam()
        {
            var washPara = new ThWashParam();
            washPara.R = (int)ThFlushPointParameterService.Instance.FlushPointParameter.ProtectRadius;
            washPara.protect_arch = ThFlushPointParameterService.Instance.
                FlushPointParameter.NecessaryArrangeSpaceOfProtectTarget;
            washPara.protect_park = ThFlushPointParameterService.Instance.
                FlushPointParameter.ParkingAreaOfProtectTarget;
            washPara.protect_other = ThFlushPointParameterService.Instance.
                FlushPointParameter.OtherSpaceOfProtectTarget;
            washPara.extend_arch = ThFlushPointParameterService.Instance.
                FlushPointParameter.NecesaryArrangeSpacePointsOfArrangeStrategy;
            washPara.extend_park = ThFlushPointParameterService.Instance.
                FlushPointParameter.ParkingAreaPointsOfArrangeStrategy;
            return washPara;
        }
#endif
    }
}
