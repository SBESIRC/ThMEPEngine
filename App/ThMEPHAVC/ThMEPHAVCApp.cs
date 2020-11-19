using AcHelper;
using Linq2Acad;
using ThMEPHAVC.Duct;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHAVC
{
    public class ThMEPHAVCApp : IExtensionApplication
    {
        public void Initialize()
        {
        }

        public void Terminate()
        {
        }

        [CommandMethod("TIANHUACAD", "THDuctGraph", CommandFlags.Modal)]
        public void THDuctGraph()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var lines = new DBObjectCollection();
                var entsresult = Active.Editor.GetSelection();
                if (entsresult.Status != PromptStatus.OK)
                {
                    return;
                }
                foreach (var item in entsresult.Value.GetObjectIds())
                {
                    lines.Add(acadDatabase.Element<Entity>(item));
                }

                var pointresult = Active.Editor.GetPoint("\n选择线路起点");
                if (pointresult.Status != PromptStatus.OK)
                {
                    return;
                }

                ThDuctGraphEngine ductGraphEngine = new ThDuctGraphEngine();
                ductGraphEngine.BuildGraph(lines, pointresult.Value);
                ThDuctGraphAnalysisEngine graphAnalysisEngine = new ThDuctGraphAnalysisEngine(ductGraphEngine.Graph);

                var countresult = Active.Editor.GetInteger("\n输入点数");
                if (countresult.Status != PromptStatus.OK)
                {
                    return;
                }
                DraughtDesignParameters DesignParameters = new DraughtDesignParameters()
                {
                    DraughtCount = countresult.Value,
                    DraughtType = TypeOfThDraught.OnBelow,
                    AirSpeed = 5,
                    TotalVolume = 1234
                };
                ThDraughtDesignEngine DraughtDesignEngine = new ThDraughtDesignEngine(graphAnalysisEngine.EndLevelEdges, DesignParameters);
                ThDuctDesignEngine DuctDesignEngine = new ThDuctDesignEngine(ductGraphEngine.Graph, ductGraphEngine.GraphStartVertex);

                //测试 将最后一级管路画出来
                foreach (var edges in DraughtDesignEngine.DraughtEndEdges)
                {
                    foreach (var edge in edges)
                    {
                        acadDatabase.ModelSpace.Add(new Line(edge.Source.Position, edge.Target.Position) { ColorIndex = 1 });
                        if (edge.DraughtInfomation == null)
                        {
                            continue;
                        }
                        foreach (var draft in edge.DraughtInfomation)
                        {
                            acadDatabase.ModelSpace.Add(new DBPoint(draft.Parameters.CenterPosition));
                        }
                    }
                }

                foreach (var edge in ductGraphEngine.Graph.Edges)
                {
                    DBText volumeinfo = new DBText()
                    {
                        TextString = edge.AirVolume.ToString(),
                        Position = edge.Target.Position,
                        Height = 1500
                    };
                    acadDatabase.ModelSpace.Add(volumeinfo);
                }
            }
        }
    }
}
