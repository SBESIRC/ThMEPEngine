using AcHelper;
using ThCADCore.NTS;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Engine;
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
                Active.Editor.WriteMessage("\n 选择分割线");

                var dxfNames = new string[]
                {
                    //RXClass.GetClass(typeof(Arc)).DxfName,
                    RXClass.GetClass(typeof(Line)).DxfName,
                    //RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                TypedValue[] tvs7 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start, string.Join(",", dxfNames)),
                };
                SelectionFilter sf7 = new SelectionFilter(tvs7);
                var result8 = Active.Editor.GetSelection(sf7);
                var divideLines = new List<Line>();
                if (result8.Status == PromptStatus.OK)
                {
                    //块的集合
                    foreach (var objId in result8.Value.GetObjectIds())
                    {
                        divideLines.Add(acadDatabase.Element<Line>(objId));
                    }
                }
                Active.Editor.WriteMessage("\n 选择屋顶雨水管");
                TypedValue[] tvs9 = new TypedValue[]
                {
                    new TypedValue((int)DxfCode.Start,"LWPolyLine")
                };
                SelectionFilter sf9 = new SelectionFilter(tvs9);
                var result9 = Active.Editor.GetSelection(sf9);
                var roofrainpipe = new List<Polyline>();
                if (result9.Status == PromptStatus.OK)
                {
                    //块的集合

                    foreach (var objId in result.Value.GetObjectIds())
                    {
                        roofrainpipe.Add(acadDatabase.Element<Polyline>(objId));
                    }
                }             
                var PipeindexEngine = new ThWInnerPipeIndexEngine();
                var compositeEngine = new ThWCompositeIndexEngine(PipeindexEngine);
                ThCADCoreNTSSpatialIndex obstacle = null;
                compositeEngine.Run(fpipe, tpipe, wpipe, ppipe, dpipe, npipe, rainpipe, pboundary,divideLines,roofrainpipe,Point3d.Origin, Point3d.Origin,obstacle);
                for (int j=0;j < compositeEngine.PipeEngine.Fpipeindex.Count;j++)
                {   
                    for (int i = 0; i < compositeEngine.PipeEngine.Fpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Fpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Fpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1=PipeindexEngine.Fpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2=PipeindexEngine.Fpipeindex_tag[j][3 * i+1].TransformBy(Matrix);
                        var tag3=PipeindexEngine.Fpipeindex_tag[j][3 * i+2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Fpipeindex[j][i], tag1);                        
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"FL{j}-{i + 1}"//原来为{floor.Value}                        
                        };
                        DBText taggingtext1 = new DBText()                       
                        {
                            Height = 175,
                            Position = tag3,                           
                            TextString = $"FL-{i + 1}"//原来为{floor.Value}                        
                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.Tpipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.Tpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Tpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Tpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = -250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1=PipeindexEngine.Tpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2=PipeindexEngine.Tpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3=PipeindexEngine.Tpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Tpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL{j}-{i+1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"TL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.Wpipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.Wpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Wpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Wpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Wpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Wpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Wpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL{j}-{i+1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"WL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.Ppipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.Ppipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Ppipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Ppipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Ppipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Ppipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Ppipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL{j}-{i+1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"PL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.Dpipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.Dpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Dpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Dpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Dpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Dpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Dpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL{j}-{i+1}",

                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"DL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.Npipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.Npipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Npipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Npipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Npipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Npipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Npipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Npipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL{j}-{i+1}",                         
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"NL-{i + 1}",

                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }                               
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.Rainpipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.Rainpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.Rainpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.Rainpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.Rainpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.Rainpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.Rainpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L{j}-{i+1}",
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y2L-{i + 1}",
                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
                }
                for (int j = 0; j < compositeEngine.PipeEngine.RoofRainpipeindex.Count; j++)
                {
                    for (int i = 0; i < compositeEngine.PipeEngine.RoofRainpipeindex[j].Count; i++)
                    {
                        int num = 0;
                        if (compositeEngine.FpipeDublicated.Count > 0)
                        {
                            foreach (var points in compositeEngine.FpipeDublicated[j])
                            {
                                if (points[0].X == PipeindexEngine.RoofRainpipeindex[j][i].X)
                                {
                                    for (int k = 0; k < points.Count; k++)
                                    {
                                        if (points[k].Y == PipeindexEngine.RoofRainpipeindex[j][i].Y)
                                        {
                                            num = k;
                                        }
                                    }

                                }
                            }
                        }
                        double Yoffset = 250 * num;
                        Vector3d s = new Vector3d(0.0, Yoffset, 0.0);
                        var Matrix = Matrix3d.Displacement(s);
                        var tag1 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i].TransformBy(Matrix);
                        var tag2 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 1].TransformBy(Matrix);
                        var tag3 = PipeindexEngine.RoofRainpipeindex_tag[j][3 * i + 2].TransformBy(Matrix);
                        Line ent_line = new Line(PipeindexEngine.RoofRainpipeindex[j][i], tag1);
                        Line ent_line1 = new Line(tag1, tag2);
                        //ent_line.Layer = "W-DRAI-NOTE";
                        ent_line.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByLayer, 256);
                        acadDatabase.ModelSpace.Add(ent_line);
                        acadDatabase.ModelSpace.Add(ent_line1);
                        DBText taggingtext = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L{j}-{i + 1}",
                        };
                        DBText taggingtext1 = new DBText()
                        {
                            Height = 175,
                            Position = tag3,
                            TextString = $"Y1L-{i + 1}",
                        };
                        if (j > 0)
                        {
                            acadDatabase.ModelSpace.Add(taggingtext);
                        }
                        else
                        {
                            acadDatabase.ModelSpace.Add(taggingtext1);
                        }
                    }
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