using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPHVAC.CAD;
using System.Linq;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHAVC.CAD;

namespace ThMEPHVAC
{
    public class ThMEPHAVCApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THselDB", CommandFlags.Modal)]
        public void THSelDB()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var selectionresult = Active.Editor.GetSelection();
                if (selectionresult.Status != PromptStatus.OK)
                {
                    return;
                }

                var lineobjects = new DBObjectCollection();
                ObjectId modelobjectid = ObjectId.Null;
                foreach (var oid in selectionresult.Value.GetObjectIds().ToList())
                {
                    var obj = oid.GetDBObject();
                    if (obj.IsModel())
                    {
                        modelobjectid = oid;
                    }
                    else
                    {
                        lineobjects.Add(obj);
                    }
                }
                ThDbModelFan DbFanModel = new ThDbModelFan(modelobjectid, lineobjects);

                ThFanInletOutletAnalysisEngine thinouteng = new ThFanInletOutletAnalysisEngine(DbFanModel);
                thinouteng.InletAnalysis();
                thinouteng.OutletAnalysis();
                ThDuctSelectionEngine ductselectioneng = new ThDuctSelectionEngine(DbFanModel);
            }
        }


        //[CommandMethod("TIANHUACAD", "THDuctDraw", CommandFlags.Modal)]
        //public void THDuctDraw()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        ThDuctParameters DuctParameters = new ThDuctParameters();

        //        var startpointresult = Active.Editor.GetPoint("\n选择管道起点");
        //        if (startpointresult.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        DuctParameters.DuctStartPositionX = startpointresult.Value.X;
        //        DuctParameters.DuctStartPositionY = startpointresult.Value.Y;

        //        var endpointresult = Active.Editor.GetPoint("\n选择管道终点");
        //        if (endpointresult.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        DuctParameters.DuctEndPositionX = endpointresult.Value.X;
        //        DuctParameters.DuctEndPositionY = endpointresult.Value.Y;

        //        DuctParameters.DuctSectionWidth = 1500;

        //        ThDuct duct = ThPipeGeometryFactoryService.Instance.CreateThDuct(DuctParameters);
        //        foreach (Line obj in duct.Geometries)
        //        {
        //            acadDatabase.ModelSpace.Add(obj);
        //        }

        //    }
        //}

        //[CommandMethod("TIANHUACAD", "THDuctGraph", CommandFlags.Modal)]
        //public void THDuctGraph()
        //{
        //    using (AcadDatabase acadDatabase = AcadDatabase.Active())
        //    {
        //        var lines = new DBObjectCollection();
        //        var entsresult = Active.Editor.GetSelection();
        //        if (entsresult.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        foreach (var item in entsresult.Value.GetObjectIds())
        //        {
        //            lines.Add(acadDatabase.Element<Entity>(item));
        //        }

        //        var pointresult = Active.Editor.GetPoint("\n选择线路起点");
        //        if (pointresult.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }

        //        ThDuctGraphEngine ductGraphEngine = new ThDuctGraphEngine();
        //        ductGraphEngine.BuildGraph(lines, pointresult.Value);
        //        ThDuctGraphAnalysisEngine graphAnalysisEngine = new ThDuctGraphAnalysisEngine(ductGraphEngine.Graph);

        //        var countresult = Active.Editor.GetInteger("\n输入点数");
        //        if (countresult.Status != PromptStatus.OK)
        //        {
        //            return;
        //        }
        //        DraughtDesignParameters DesignParameters = new DraughtDesignParameters()
        //        {
        //            DraughtCount = countresult.Value,
        //            DraughtType = TypeOfThDraught.OnBelow,
        //            AirSpeed = 5,
        //            TotalVolume = 1234
        //        };
        //        ThDraughtDesignEngine DraughtDesignEngine = new ThDraughtDesignEngine(graphAnalysisEngine.EndLevelEdges, DesignParameters);
        //        ThDuctDesignEngine DuctDesignEngine = new ThDuctDesignEngine(ductGraphEngine.Graph, ductGraphEngine.GraphStartVertex);

        //        var jsongraph = new AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>();
        //        using (var xmlwriter = XmlWriter.Create(@"D:\管道设计\需求文档\Graplxml.xml"))
        //        {
        //            var graphjson = GraphSerializeService.Instance.GetJsonStringFromGraph(ductGraphEngine.Graph);
        //            jsongraph = GraphSerializeService.Instance.GetGraphFromJsonString(graphjson);
        //            //var edgejson = FuncJson.Serialize(Graphedgedateset);
        //            //var deedges = FuncJson.Deserialize<IEnumerable<ThDuctEdge<ThDuctVertex>>>(edgejson);
        //            //var trangraph2 = Graphedgedateset.ToAdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>(false);
        //            ductGraphEngine.Graph.SerializeToGraphML<ThDuctVertex, ThDuctEdge<ThDuctVertex>, AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>>(xmlwriter);
        //            //GraphMLExtensions.SerializeToGraphML<ThDuctVertex, ThDuctEdge<ThDuctVertex>, AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>>(ductGraphEngine.Graph, xmlwriter);
        //            //ductGraphEngine.Graph.SerializeToXml<ThDuctVertex, ThDuctEdge<ThDuctVertex>, AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>>>();
        //        }

        //        //测试 将最后一级管路画出来
        //        foreach (var edges in DraughtDesignEngine.DraughtEndEdges)
        //        {
        //            foreach (var edge in edges)
        //            {
        //                acadDatabase.ModelSpace.Add(new Line(edge.Source.VertexToPoint3D(), edge.Target.VertexToPoint3D()) { ColorIndex = 1 });
        //                if (edge.DraughtInfomation == null)
        //                {
        //                    continue;
        //                }
        //                foreach (var draft in edge.DraughtInfomation)
        //                {
        //                    acadDatabase.ModelSpace.Add(new DBPoint(new Point3d(draft.Parameters.XPosition, draft.Parameters.YPosition, 0)));
        //                }
        //            }
        //        }

        //        foreach (var edge in jsongraph.Edges)
        //        {
        //            DBText volumeinfo = new DBText()
        //            {
        //                TextString = edge.AirVolume.ToString(),
        //                Position = edge.Target.VertexToPoint3D(),
        //                Height = 1500
        //            };
        //            acadDatabase.ModelSpace.Add(volumeinfo);
        //        }
        //    }
        //}
    }
}
