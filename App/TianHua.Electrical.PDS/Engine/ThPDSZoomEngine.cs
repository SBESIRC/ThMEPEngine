using System.Linq;

using AcHelper;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;

using ThCADExtension;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Project.Module;
using ProjectGraph = QuikGraph.BidirectionalGraph<
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode,
    TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSZoomEngine
    {
        public ThPDSZoomEngine()
        {

        }

        public void Zoom(ThPDSProjectGraphNode projectNode, ProjectGraph projectGraph)
        {
            var nodeList = projectGraph.Vertices
                .Where(o => o.Load.ID.LoadID.Equals(projectNode.Load.ID.LoadID)).ToList();
            if (nodeList.Count != 1)
            {
                return;
            }
            Zoom(nodeList[0]);
        }

        public void Zoom(ThPDSProjectGraphNode projectNode)
        {
            Zoom(projectNode.Load.Location);
        }

        public void Zoom(ThPDSLocation location)
        {
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
                    if (location.ReferenceDWG.Equals(referenceDWG))
                    {
                        Application.DocumentManager.MdiActiveDocument = doc;
                        var scaleFactor = 8000;
                        var minPoint = new Point3d(location.BasePoint.X - scaleFactor,
                                                   location.BasePoint.Y - scaleFactor, 0);
                        var maxPoint = new Point3d(location.BasePoint.X + scaleFactor,
                                                   location.BasePoint.Y + scaleFactor, 0);
                        Active.Editor.ZoomWindow(minPoint, maxPoint);
                    }
                }
            }
        }
    }
}
