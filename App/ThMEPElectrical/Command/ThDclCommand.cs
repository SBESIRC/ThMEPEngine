#if (ACAD2016 || ACAD2018)
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using ThMEPElectrical.DCL.Data;
using ThMEPElectrical.DCL.Service;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using CLI;
#endif

using System;
using AcHelper.Commands;
using ThMEPEngineCore.CAD;

namespace ThMEPElectrical.Command
{
    public class ThDclCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            
        }
#if (ACAD2016 || ACAD2018)
        public void Execute()
        {
            //Lightning Protection Down Conductors Test Data(防雷保护引下线)
            using (var acadDatabase = AcadDatabase.Active())
            {
                var frame = ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                var pts = frame.Vertices();
                if(pts.Count<3)
                {
                    return;
                }
                bool outerComponentPattern = true; //true1-> 用模式1,false->用模式2 

                var levelIndex = 3;
                var ner = Active.Editor.GetInteger("\n输入防雷等级类别<三类>");
                if (ner.Status == PromptStatus.OK)
                {
                    levelIndex = ner.Value;
                }
                short colorIndex = 1;

                var storeyExtractor = new ThDclEStoreyExtractor()
                {
                    ColorIndex = colorIndex++,
                    GroupSwitch = false,
                    UseDb3Engine = true,
                };
                storeyExtractor.Extract(acadDatabase.Database, pts);

                var architectureOutlineExtractor = new ThArchitectureOutlineExtractor()
                {
                    ColorIndex = colorIndex++,
                    GroupSwitch = true,
                    UseDb3Engine = false,
                    IsolateSwitch = false,
                };
                architectureOutlineExtractor.Extract(acadDatabase.Database, pts);
                architectureOutlineExtractor.MoveSecondToFirst(storeyExtractor.Storeys,storeyExtractor.StoreyNumberMap);//拿到所有的建筑轮廓线
                
                //构建外圈构件

                ThOuterVertialComponentData componentData;
                ThOuterVerticalComponentRecognizer outerRecognizer = null;
                if (outerComponentPattern)
                {
                    var modelData = architectureOutlineExtractor.ModelData;
                    componentData = new ThArchOuterVertialComponentData(modelData)
                    {
                        OuterOutlines = architectureOutlineExtractor.OuterArchOutlineIdDic.Keys.ToList(),
                        InnerOutlines = architectureOutlineExtractor.InnerArchOutlineIdDic.Keys.ToList()
                    };
                    outerRecognizer = new ThArchOuterVerticalComponentRecognizer(componentData,architectureOutlineExtractor)
                    {
                        OuterArchOutlineID = architectureOutlineExtractor.OuterArchOutlineIdDic,
                        InnterArchOutlineID = architectureOutlineExtractor.InnerArchOutlineIdDic
                    };
                }
                else
                {
                    var buildServie = new ThBuildOuterStruOutline();
                    buildServie.Extract(acadDatabase.Database, pts);
                    buildServie.ExtractHoles(acadDatabase.Database, pts);
                    componentData = new ThStruOuterVertialComponentData(buildServie.ModelData)
                    {
                        OuterOutlines = buildServie.OuterOutlineList.Cast<Entity>().ToList(),
                        InnerOutlines = buildServie.InnerOutlineList.Cast<Entity>().ToList()
                    };//此时识别的轮廓线用结构轮廓线
                    outerRecognizer = new ThStruOuterVerticalComponentRecognizer(componentData)
                    {
                        OuterArchOutlineID = architectureOutlineExtractor.OuterArchOutlineIdDic,
                        InnterArchOutlineID = architectureOutlineExtractor.InnerArchOutlineIdDic
                    };//而最终的结果需要建筑轮廓线
                }
                outerRecognizer.Recognize();//识别仅仅是区分外圈构件和其他构件

                var extractors = new List<ThExtractorBase>()
                {                   
                    new ThLightningReceivingBeltExtractor
                    {
                        ColorIndex=colorIndex++,
                        GroupSwitch=true,
                        UseDb3Engine=false,
                        IsolateSwitch=false,
                    },

                    new ThOuterOtherColumnExtractor()
                    {
                        ColorIndex=colorIndex++,
                        GroupSwitch=true,
                        OuterColumns = outerRecognizer.OuterColumns.Cast<Entity>().ToList(),
                        OtherColumns = outerRecognizer.OtherColumns.Cast<Entity>().ToList(),                        
                    },
                    new ThOuterOtherShearWallExtractor()
                    {
                        ColorIndex=colorIndex++,
                        GroupSwitch=true,
                        OuterShearWalls = outerRecognizer.OuterShearwalls.Cast<Entity>().ToList(),
                        OtherShearWalls = outerRecognizer.OtherShearwalls.Cast<Entity>().ToList(),                        
                    },
                };

                var columnExtractor = extractors.Where(o => o is ThOuterOtherColumnExtractor).First() as ThOuterOtherColumnExtractor;
                columnExtractor.BelongArchitectureIdDic = outerRecognizer.GetOuterColumnBelongedOutArchlineId();
                var shearwallExtractor = extractors.Where(o => o is ThOuterOtherShearWallExtractor).First() as ThOuterOtherShearWallExtractor;
                shearwallExtractor.BelongArchitectureIdDic = outerRecognizer.GetOuterShearWallBelongedOutArchlineId();

                extractors.ForEach(e => e.Extract(acadDatabase.Database, pts));
                extractors.Add(architectureOutlineExtractor);
                extractors.Add(storeyExtractor);
                extractors.ForEach(e =>
                {
                    if(e is IGroup iGroup)
                    {
                        iGroup.Group((extractors[4] as ThEStoreyExtractor).StoreyIds);
                    }
                });
                //打印查看建筑轮廓线和外圈墙、柱和其它墙、柱（test）
                extractors.ForEach(o =>
                {
                    if (o is ThArchitectureOutlineExtractor ||
                    o is ThOuterOtherColumnExtractor ||
                    o is ThOuterOtherShearWallExtractor)
                    {
                        (o as IPrint).Print(acadDatabase.Database);
                    }

                });
#if DEBUG
                var geos = new List<ThGeometry>();
                //extractors.ForEach(e => geos.AddRange(e.BuildGeometries()));
                extractors.ForEach(o =>
                {
                    string groupid = ThExtractorPropertyNameManager.GroupIdPropertyName;
                    var temp = o.BuildGeometries();
                    temp = temp.Where(e =>
                    {
                        return (!e.Properties.ContainsKey(groupid)) ||
                        (e.Properties.ContainsKey(groupid) && (string)e.Properties[groupid] != "");
                    }).ToList();
                    geos.AddRange(temp);
                });

                // 对接浙大算法
                string geoContent = ThGeoOutput.Output(geos);
                var dclLayoutEngine = new ThDCLayoutEngineMgd();
                var data = new ThDCDataMgd();
                data.ReadFromContent(geoContent);
                var param = new ThDCParamMgd(levelIndex);
                var result = dclLayoutEngine.Run(data, param);
                var parseResults = ThDclResultParseService.Parse(result);
                var printService = new ThDclPrintService(acadDatabase.Database, "AI-DCL");
                printService.Print(parseResults);
#endif
            }
        }
#else
        public void Execute()
        {
        }
#endif
    }
}
