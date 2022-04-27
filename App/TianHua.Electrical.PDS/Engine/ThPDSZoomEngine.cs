using System.Linq;

using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;

using ThCADExtension;
using TianHua.Electrical.PDS.Project.Module;
using ProjectGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSZoomEngine
    {
        private ProjectGraph ProjectGraph;

        public ThPDSZoomEngine(ProjectGraph projectGraph)
        {
            ProjectGraph = projectGraph;
        }

        public void Zoom(ThPDSProjectGraphNode projectNode)
        {
            var nodeList = ProjectGraph.Vertices
                .Where(o => o.Load.ID.LoadID.Equals(projectNode.Load.ID.LoadID)).ToList();
            if (nodeList.Count != 1)
            {
                return;
            }
            var node = nodeList[0];

            foreach (Document doc in Application.DocumentManager)
            {
                //var fileName = doc.Name.Split('\\').Last();
                //if (FireCompartmentParameter.ChoiseFileNames.Count(file => string.Equals(fileName, file)) != 1)
                //{
                //    continue;
                //}

                using (var docLock = doc.LockDocument())
                using (var activeDb = AcadDatabase.Use(doc.Database))
                {
                    var referenceDWG = doc.Database.OriginalFileName.Split("\\".ToCharArray()).Last();
                    if (node.Load.Location.ReferenceDWG.Equals(referenceDWG))
                    {
                        Application.DocumentManager.MdiActiveDocument = doc;
                        var scaleFactor = 8000;
                        var minPoint = new Point3d(node.Load.Location.BasePoint.X - scaleFactor,
                                                   node.Load.Location.BasePoint.Y - scaleFactor, 0);
                        var maxPoint = new Point3d(node.Load.Location.BasePoint.X + scaleFactor,
                                                   node.Load.Location.BasePoint.Y + scaleFactor, 0);
                        Active.Editor.ZoomWindow(minPoint, maxPoint);
                    }
                }
            }
        }
    }
}
