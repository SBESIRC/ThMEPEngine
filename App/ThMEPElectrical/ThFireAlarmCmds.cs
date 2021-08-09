using AcHelper;
using Linq2Acad;
using System.IO;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Temp;
using ThMEPElectrical.Command;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical
{
    public class ThFireAlarmCmds
    {
        [CommandMethod("TIANHUACAD", "THFireAlarmData", CommandFlags.Modal)]
        public void THFireAlarmData()
        {
            using (var cmd = new ThFireAlarmCommand())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THFireAlarmTestDataExtract", CommandFlags.Modal)]
        public void THFireAlarmTestDataExtract()
        {

            using (var acadDatabase = AcadDatabase.Active())
            using (var extractEngine = new ThExtractGeometryEngine())
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
                    GroupSwitch = false,
                    UseDb3Engine = true,
                    IsolateSwitch = false,
                };
                storeyExtractor.Extract(acadDatabase.Database, pts);

                //再提取防火分区，接着用楼层框线对防火分区分组
                var storeyInfos = storeyExtractor.Storeys.Cast<StoreyInfo>().ToList();
                var fireApartExtractor = new ThFireApartExtractor()
                {
                    ElementLayer = "AI-防火分区,AD-AREA-DIVD",
                    ColorIndex = 11,
                    GroupSwitch = true,
                    UseDb3Engine = false,
                    IsolateSwitch = false,
                    StoreyInfos = storeyInfos, //用于创建防火分区
                };
                fireApartExtractor.Extract(acadDatabase.Database, pts);
                fireApartExtractor.Group(storeyExtractor.StoreyIds); //判断防火分区属于哪个楼层框线
                fireApartExtractor.BuildFireAPartIds(); //创建防火分区编号

                var extractors = new List<ThExtractorBase>()
                {
                    new ThFaArchitectureWallExtractor()
                    {
                        ElementLayer = "AI-墙",
                        ColorIndex=1,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        ColorIndex=2,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        ColorIndex=3,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        ColorIndex=4,
                        GroupSwitch=true,
                        UseDb3Engine=true,
                        IsolateSwitch=false,
                    },
                    new ThFaRoomExtractor()
                    {
                        ColorIndex=5,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        ColorIndex=6,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaDoorOpeningExtractor()
                    {
                        ElementLayer = "AI-门",
                        ColorIndex=7,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaRailingExtractor()
                    {
                        ElementLayer = "AI-栏杆",
                        ColorIndex=8,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThFaFireproofshutterExtractor()
                    {
                        ElementLayer = "AI-防火卷帘",
                        ColorIndex=9,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                    new ThHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        ColorIndex=10,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },
                };
                extractEngine.Accept(extractors);
                extractEngine.Extract(acadDatabase.Database, pts);
                //用防火分区对墙、柱...分组
                extractEngine.Group(fireApartExtractor.FireApartIds);
                //找到防火门、防火卷帘邻接的防火分区
                var faDoorExtractor = extractors.Where(o => o is ThFaDoorOpeningExtractor).First() as ThFaDoorOpeningExtractor;
                faDoorExtractor.SetTags(fireApartExtractor.FireApartIds);
                var fireProofShutter = extractors.Where(o => o is ThFaFireproofshutterExtractor).First() as ThFaFireproofshutterExtractor;
                fireProofShutter.SetTags(fireApartExtractor.FireApartIds);

                //最后将楼层框线和防火分区提取器加入，生成Geometries
                extractEngine.Accept(storeyExtractor);
                extractEngine.Accept(fireApartExtractor);
                //把楼层框线传给列表中的提取器
                extractors.ForEach(o =>
                {
                    if (o is ISetStorey iSetStory)
                    {
                        iSetStory.Set(storeyExtractor.Storeys.Cast<StoreyInfo>().ToList());
                    }
                });
                var geos = extractEngine.BuildGeometries();

                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                string fileName = fileInfo.Name;
                string newFileName = "";
                storeyExtractor.Storeys.ForEach(o =>
                {
                    newFileName = fileName + o.StoreyType + o.StoreyNumber;
                });
                ThGeoOutput.Output(geos, path, newFileName);
                extractEngine.Print(acadDatabase.Database);
            }
        }
    }
}
