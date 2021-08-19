using System;
using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;

namespace ThMEPEngineCore.Test
{
    public class ThBuildEngineTest : IExtensionApplication
    {
        public void Initialize()
        {
            ThMPolygonTool.Initialize();
        }

        public void Terminate()
        {
        }
        [CommandMethod("TIANHUACAD","THColumnBuilderTest",CommandFlags.Modal)]
        public void THColumnBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThColumnBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                      {
                          acadDatabase.ModelSpace.Add(o);
                          o.SetDatabaseDefaults();
                      });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                      {
                          acadDatabase.ModelSpace.Add(o);
                          o.SetDatabaseDefaults();
                      });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THShearwallBuilderTest", CommandFlags.Modal)]
        public void THShearwallBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThShearwallBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THArchwallBuilderTest", CommandFlags.Modal)]
        public void THArchwallBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThArchWallBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THSlabBuilderTest", CommandFlags.Modal)]
        public void THSlabBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThSlabBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THWindowBuilderTest", CommandFlags.Modal)]
        public void THWindowBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThWindowBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THCornicesBuilderTest", CommandFlags.Modal)]
        public void THCornicesBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThCornicesBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THRailingBuilderTest", CommandFlags.Modal)]
        public void THRailingBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThRailingBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }


        [CommandMethod("TIANHUACAD", "THStairBuilderTest", CommandFlags.Modal)]
        public void THStairBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThStairBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    engine.Extract(acadDatabase.Database).Select(o => o.Geometry).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }

        [CommandMethod("TIANHUACAD", "THDoorBuilderTest", CommandFlags.Modal)]
        public void THDoorBuilderTest()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var options = new PromptKeywordOptions("\n选择处理方式");
                options.Keywords.Add("提取", "E", "提取(E)");
                options.Keywords.Add("识别", "R", "识别(R)");
                options.Keywords.Add("构建", "B", "构建(B)");
                options.Keywords.Default = "构建";
                var result2 = Active.Editor.GetKeywords(options);
                if (result2.Status != PromptStatus.OK)
                {
                    return;
                }

                var engine = new ThDoorBuilderEngine();

                if (result2.StringResult == "提取")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "识别")
                {
                    throw new NotImplementedException();
                }
                else if (result2.StringResult == "构建")
                {
                    engine.Build(acadDatabase.Database, frame.Vertices()).Select(o => o.Outline).ForEach(o =>
                    {
                        acadDatabase.ModelSpace.Add(o);
                        o.SetDatabaseDefaults();
                    });
                }
                else
                    throw new NotImplementedException();
            }
        }
    }
}
