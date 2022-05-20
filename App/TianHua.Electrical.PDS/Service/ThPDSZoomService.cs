using System.IO;
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

namespace TianHua.Electrical.PDS.Service
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
            if (location == null)
            {
                return;
            }
            if(location.BasePoint.EqualsTo(new ThPDSPoint3d(0.01,0.01)))
            {
                Active.Editor.WriteLine("无法Zoom至指定负载");
                return;
            }
            foreach (Document doc in Application.DocumentManager)
            {
                using (var docLock = doc.LockDocument())
                using (var activeDb = AcadDatabase.Use(doc.Database))
                {
                    var referenceDWG = Path.GetFileNameWithoutExtension(doc.Database.Filename);
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
