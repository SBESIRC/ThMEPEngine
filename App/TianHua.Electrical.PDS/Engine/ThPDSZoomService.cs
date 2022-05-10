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
    public class ThPDSZoomService
    {
        public ThPDSZoomService()
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
            ImmediatelyZoom(nodeList[0]);
        }

        /// <summary>
        /// 即时Zoom到边的下级
        /// </summary>
        /// <param name="projectEdge"></param>
        public void ImmediatelyZoom(ThPDSProjectGraphEdge projectEdge)
        {
            ImmediatelyZoom(projectEdge.Target);
        }

        /// <summary>
        /// 即时Zoom到该节点
        /// </summary>
        /// <param name="projectNode"></param>
        public void ImmediatelyZoom(ThPDSProjectGraphNode projectNode)
        {
            ImmediatelyZoom(projectNode.Load.Location);
        }

        /// <summary>
        /// 即时Zoom到某个ThPDSLocation
        /// </summary>
        /// <param name="location"></param>
        public void ImmediatelyZoom(ThPDSLocation location)
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
                        var scaleFactor = 2500.0;
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
