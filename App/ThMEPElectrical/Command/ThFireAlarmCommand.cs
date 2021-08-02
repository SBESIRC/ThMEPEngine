using System;
using AcHelper;
using System.IO;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using FireAlarm.Data;
using AcHelper.Commands;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Algorithm;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.FireAlarm.Interfacce;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.FireAlarm.Service;
using ThMEPEngineCore.GeojsonExtractor.Model;

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

                short colorIndex = 1;
                // ArchitectureWall、Shearwall、Column、Window、Room
                // Beam、DoorOpening、Railing、FireproofShutter(防火卷帘)

                //先提取楼层框线
                var storeyExtractor = new ThFaEStoreyExtractor()
                {
                    ElementLayer = "AI-楼层框定E",
                    ColorIndex = colorIndex++,
                };
                storeyExtractor.Extract(acadDatabase.Database, pts);

                //再提取防火分区，接着用楼层框线对防火分区分组
                var storeyInfos = storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList();
                var fireApartExtractor = new ThFireApartExtractor()
                {
                    ElementLayer = "AI-防火分区,AD-AREA-DIVD",
                    ColorIndex = colorIndex++,
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
                        ColorIndex=colorIndex++,
                    },
                    new ThFaShearWallExtractor()
                    {
                        ElementLayer = "AI-剪力墙",
                        ColorIndex=colorIndex++,
                    },
                    new ThFaColumnExtractor()
                    {
                        ElementLayer = "AI-柱",
                        ColorIndex=colorIndex++,
                    },
                    new ThFaWindowExtractor()
                    {
                        ElementLayer="AI-窗",
                        ColorIndex=colorIndex++,
                    },
                    new ThFaRoomExtractor()
                    {
                        ColorIndex=colorIndex++,
                        UseDb3Engine=true,
                    },
                    new ThFaBeamExtractor()
                    {
                        ElementLayer = "AI-梁",
                        ColorIndex=colorIndex++,
                    },
                    new ThFaDoorOpeningExtractor()
                    {
                        ElementLayer = "AI-门",
                        ColorIndex=colorIndex++,
                    },
                    new ThFaRailingExtractor()
                    {
                        ElementLayer = "AI-栏杆",
                        ColorIndex=colorIndex++,
                    },
                    new ThFaFireproofshutterExtractor()
                    {
                        ElementLayer = "AI-防火卷帘",
                        ColorIndex=colorIndex++,
                    },
                    new ThHoleExtractor()
                    {
                        ElementLayer = "AI-洞",
                        ColorIndex=colorIndex++,
                    },
                };
                extractors.ForEach(o => o.Extract(acadDatabase.Database, pts));
                //把楼层信息传入到提取器中，对于不在防火分区内的图形要判断在哪个楼层
                extractors.ForEach(o =>
                {
                    if(o is ISetStorey iStorey)
                    {
                        iStorey.Set(storeyInfos);
                    }
                });

                //将房间外扩的区域得到的差集作为墙传入到建筑墙中
                var selfBuildWalls = BuildWalls(extractors);
                var architectureWallExtractor= extractors.Where(
                    o => o is ThFaArchitectureWallExtractor).First() 
                    as ThFaArchitectureWallExtractor;
                architectureWallExtractor.Walls.AddRange(selfBuildWalls);

                //用防火分区对墙、柱...分组
                extractors.ForEach(o => 
                {
                    if(o is IGroup group)
                    {
                        group.Group(fireApartExtractor.FireApartIds);
                    }
                });
                
                //找到防火门、防火卷帘邻接的防火分区
                var faDoorExtractor = extractors.Where(o => o is ThFaDoorOpeningExtractor).First() as ThFaDoorOpeningExtractor;
                faDoorExtractor.SetTags(fireApartExtractor.FireApartIds);
                var fireProofShutter = extractors.Where(o => o is ThFaFireproofshutterExtractor).First() as ThFaFireproofshutterExtractor;
                fireProofShutter.SetTags(fireApartExtractor.FireApartIds);  

                //最后将楼层框线和防火分区提取器加入，生成Geometries
                extractors.Add(storeyExtractor);
                extractors.Add(fireApartExtractor);
                //把楼层框线传给列表中的提取器
                extractors.ForEach(o =>
                {
                    if (o is ISetStorey iSetStory)
                    {
                        iSetStory.Set(storeyExtractor.Storeys.Cast<ThStoreyInfo>().ToList());
                    }
                });

                // 把房间传给门提取器
                var roomExtractor = extractors.Where(o => o is ThFaRoomExtractor).First() as ThFaRoomExtractor;
                faDoorExtractor.SetRooms(roomExtractor.Rooms);

                var geos = new List<ThGeometry>();
                extractors.ForEach(o => geos.AddRange(o.BuildGeometries()));

                //输出
                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                string fileName = fileInfo.Name;
                string newFileName = "";
                storeyExtractor.Storeys.ForEach(o =>
                {
                    newFileName = fileName + o.StoreyType + o.StoreyNumber;
                });
                ThGeoOutput.Output(geos, path, newFileName);
                extractors.ForEach(o =>
                {
                    if(o is IPrint printer)
                    {
                        printer.Print(acadDatabase.Database);
                    }
                });
            }
        }
        private List<Entity> BuildWalls(List<ThExtractorBase> extractors)
        {
            var roomExtractor = extractors.Where(o => o is ThFaRoomExtractor).First() as ThFaRoomExtractor;
            var handleBufferService = new ThHandleRoomBufferService(roomExtractor.GetEntities());
            extractors.ForEach(o =>
            {
                if(o is ThFaArchitectureWallExtractor ||
                o is ThFaShearWallExtractor ||
                o is ThFaColumnExtractor ||
                o is ThFaWindowExtractor ||
                o is ThFaDoorOpeningExtractor ||
                o is ThFaBeamExtractor ||
                o is ThFaRailingExtractor ||
                o is ThFaFireproofshutterExtractor)
                {
                    handleBufferService.Add(o.GetEntities());
                }
            });
            handleBufferService.Handle();

            return handleBufferService.Walls;
        }
    }
}
