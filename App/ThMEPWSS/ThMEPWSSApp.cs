using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThWSS;
using ThMEPWSS.Pipe;
using System;
using ThMEPWSS.Pipe.Engine;
using DotNetARX;


namespace ThMEPWSS
{
    public class ThMEPWSSApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THLG", CommandFlags.Modal)]
        public void ThConnectPipe()
        {
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "选择区域",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(Polyline)).DxfName,
            };
            var filter = ThSelectionFilterTool.Build(dxfNames);
            var result = Active.Editor.GetSelection(options, filter);
            if (result.Status != PromptStatus.OK)
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                foreach (ObjectId frame in result.Value.GetObjectIds())
                {
                    var plBack = acdb.Element<Polyline>(frame);
                    var plFrame = ThMEPFrameService.Normalize(plBack);

                    var filterlist = OpFilter.Bulid(o =>
                        o.Dxf((int)DxfCode.LayerName) == ThWSSCommon.PipeLine_LayerName &
                        o.Dxf((int)DxfCode.Start) == RXClass.GetClass(typeof(Line)).DxfName);

                    var dBObjectCollection = new DBObjectCollection();
                    var allLines = Active.Editor.SelectAll(filterlist);
                    if (allLines.Status == PromptStatus.OK)
                    {
                        foreach (ObjectId obj in allLines.Value.GetObjectIds())
                        {
                            dBObjectCollection.Add(acdb.Element<Line>(obj));
                        }

                        ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(dBObjectCollection);
                        var pipeLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(plFrame).Cast<Line>().ToList();

                    }
                }
            }
        }

        [CommandMethod("TIANHUACAD", "THPIPEINDEX", CommandFlags.Modal)]
        public void Thpipeindex()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                PromptIntegerOptions ppo = new PromptIntegerOptions("请输入楼层");
                PromptIntegerResult floor = Active.Editor.GetInteger(ppo);

                Active.Editor.WriteMessage("\n 选择废气F管");
                TypedValue[] tvs = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"LWPolyLine")
                };
                SelectionFilter sf = new SelectionFilter(tvs);
                var result = Active.Editor.GetSelection(sf);
                var fpipe = new List<Polyline>();
                if (result.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result.Value.GetObjectIds())
                    {
                        fpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择通气T管");
                TypedValue[] tvs1 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf1 = new SelectionFilter(tvs);
                var result1 = Active.Editor.GetSelection(sf);
                var tpipe = new List<Polyline>();
                if (result1.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result1.Value.GetObjectIds())
                    {
                        tpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择污水W管");
                TypedValue[] tvs2 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf2 = new SelectionFilter(tvs);
                var result2 = Active.Editor.GetSelection(sf);
                var wpipe = new List<Polyline>();
                if (result2.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result2.Value.GetObjectIds())
                    {
                        wpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择污废合流P管");
                TypedValue[] tvs3 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf3 = new SelectionFilter(tvs);
                var result3 = Active.Editor.GetSelection(sf);
                var ppipe = new List<Polyline>();
                if (result3.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result3.Value.GetObjectIds())
                    {
                        ppipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 沉箱D");
                TypedValue[] tvs4 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf4 = new SelectionFilter(tvs);
                var result4 = Active.Editor.GetSelection(sf);
                var dpipe = new List<Polyline>();
                if (result4.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result4.Value.GetObjectIds())
                    {
                        dpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 冷凝N管");
                TypedValue[] tvs5 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf5 = new SelectionFilter(tvs);
                var result5 = Active.Editor.GetSelection(sf);
                var npipe = new List<Polyline>();
                if (result5.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result5.Value.GetObjectIds())
                    {
                        npipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 阳台雨水立管");
                TypedValue[] tvs6 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"Insert")
                };
                SelectionFilter sf6 = new SelectionFilter(tvs);
                var result6 = Active.Editor.GetSelection(sf);
                var rainpipe = new List<Polyline>();
                if (result6.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result6.Value.GetObjectIds())
                    {
                        rainpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }
                var result7 = Active.Editor.GetEntity("\n楼层外框");
                if (result7.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline pboundary = acadDatabase.Element<Polyline>(result7.ObjectId);
                var PipeindexEngine = new ThWInnerPipeIndexEngine();
                PipeindexEngine.Run(fpipe, tpipe, wpipe, ppipe, dpipe, npipe, rainpipe, pboundary);
                for (int i = 0; i < PipeindexEngine.Fpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Fpipeindex[i], PipeindexEngine.Fpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Fpipeindex_tag[3 * i], PipeindexEngine.Fpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Fpipeindex_tag[3 * i + 2],
                        TextString = $"FL{floor.Value}-{i}",
                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Tpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Tpipeindex[i], PipeindexEngine.Tpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Tpipeindex_tag[3 * i], PipeindexEngine.Tpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Tpipeindex_tag[3 * i + 2],
                        TextString = $"TL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Wpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Wpipeindex[i], PipeindexEngine.Wpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Wpipeindex_tag[3 * i], PipeindexEngine.Wpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Wpipeindex_tag[3 * i + 2],
                        TextString = $"WL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Ppipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Ppipeindex[i], PipeindexEngine.Ppipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Ppipeindex_tag[3 * i], PipeindexEngine.Ppipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Ppipeindex_tag[3 * i + 2],
                        TextString = $"PL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Dpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Dpipeindex[i], PipeindexEngine.Dpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Dpipeindex_tag[3 * i], PipeindexEngine.Dpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Dpipeindex_tag[3 * i + 2],
                        TextString = $"DL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Npipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Npipeindex[i], PipeindexEngine.Npipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Npipeindex_tag[3 * i], PipeindexEngine.Npipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Npipeindex_tag[3 * i + 2],
                        TextString = $"NL{floor.Value}-{i}",

                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
                for (int i = 0; i < PipeindexEngine.Rainpipeindex.Count - 1; i++)
                {
                    Line ent_line = new Line(PipeindexEngine.Rainpipeindex[i], PipeindexEngine.Rainpipeindex_tag[3 * i]);
                    Line ent_line1 = new Line(PipeindexEngine.Rainpipeindex_tag[3 * i], PipeindexEngine.Rainpipeindex_tag[3 * i + 1]);
                    //ent_line.Layer = "W-DRAI-NOTE";
                    ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                    acadDatabase.ModelSpace.Add(ent_line);
                    acadDatabase.ModelSpace.Add(ent_line1);
                    DBText taggingtext = new DBText()
                    {
                        Height = 200,
                        Position = PipeindexEngine.Rainpipeindex_tag[3 * i + 2],
                        TextString = $"Y2L{floor.Value}-{i}",
                    };
                    acadDatabase.ModelSpace.Add(taggingtext);
                }
            }
        }
        [CommandMethod("TIANHUACAD", "THToiletRecognize", CommandFlags.Modal)]
        public void THToiletRecognize()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThWToiletRoomRecognitionEngine engine = new ThWToiletRoomRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                Point3dCollection f = new Point3dCollection();
                engine.Recognize(Active.Database, f);
                engine.Rooms.ForEach(o =>
                {
                    ObjectIdCollection objIds = new ObjectIdCollection();
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    dbObjs.Add(o.Toilet.Boundary);
                    o.Closestools.ForEach(m => dbObjs.Add(m.Outline));
                    o.DrainageWells.ForEach(m => dbObjs.Add(m.Boundary));
                    o.FloorDrains.ForEach(m => dbObjs.Add(m.Outline));
                    dbObjs.Cast<Entity>().ForEach(m => objIds.Add(acadDatabase.ModelSpace.Add(m)));
                    if (o.Toilet != null && o.Closestools.Count == 1 &&
                    o.DrainageWells.Count == 1 && o.FloorDrains.Count > 0)
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 3);
                    }
                    else
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 1);
                    }
                    GroupTools.CreateGroup(Active.Database, Guid.NewGuid().ToString(), objIds);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "THKitchenRecognize", CommandFlags.Modal)]
        public void THKitchenRecognize()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThWKitchenRoomRecognitionEngine engine = new ThWKitchenRoomRecognitionEngine())
            {
                Point3dCollection f = new Point3dCollection();
                engine.Recognize(Active.Database, f);
                engine.Rooms.ForEach(o =>
                {
                    ObjectIdCollection objIds = new ObjectIdCollection();
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    dbObjs.Add(o.Kitchen.Boundary);

                    o.DrainageWells.ForEach(m => dbObjs.Add(m.Boundary));

                    dbObjs.Cast<Entity>().ForEach(m => objIds.Add(acadDatabase.ModelSpace.Add(m)));
                    if (o.Kitchen != null && o.DrainageWells.Count == 1)
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 3);
                    }
                    else
                    {
                        dbObjs.Cast<Entity>().ForEach(m => m.ColorIndex = 1);
                    }
                    GroupTools.CreateGroup(Active.Database, Guid.NewGuid().ToString(), objIds);
                });
            }
        }
        [CommandMethod("TIANHUACAD", "ThExtractIfcBasinTool", CommandFlags.Modal)]
        public void ThExtractIfcBasinTool()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var basintoolEngine = new ThBasinRecognitionEngine())
            {
                var result = Active.Editor.GetEntity("\n选择框线");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                Polyline frame = acadDatabase.Element<Polyline>(result.ObjectId);
                basintoolEngine.Recognize(acadDatabase.Database, frame.Vertices());
                basintoolEngine.Elements.ForEach(o =>
                {
                    acadDatabase.ModelSpace.Add(o.Outline);
                });
            }
        }
    }
}