using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;

using ThCADExtension;
using TianHua.Electrical.PDS.Model;
using ProjectGraph = QuikGraph.BidirectionalGraph<TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphNode, TianHua.Electrical.PDS.Project.Module.ThPDSProjectGraphEdge>;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSInfoModifyEngine
    {
        private List<ThPDSNodeMap> NodeMapList;

        private List<ThPDSEdgeMap> EdgeMapList;

        private ProjectGraph ProjectGraph;

        public ThPDSInfoModifyEngine(List<ThPDSNodeMap> nodeMapList, List<ThPDSEdgeMap> edgeMapList, ProjectGraph projectGraph)
        {
            NodeMapList = nodeMapList;
            EdgeMapList = edgeMapList;
            ProjectGraph = projectGraph;
        }

        public void Execute()
        {
            var dm = Application.DocumentManager;
            foreach (Document doc in dm)
            {
                //var fileName = doc.Name.Split('\\').Last();
                //if (FireCompartmentParameter.ChoiseFileNames.Count(file => string.Equals(fileName, file)) != 1)
                //{
                //    continue;
                //}

                using (var docLock = doc.LockDocument())
                using (var acad = AcadDatabase.Use(doc.Database))
                {
                    var referenceDWG = doc.Database.OriginalFileName.Split("\\".ToCharArray()).Last();
                    var nodeMap = NodeMapList.FirstOrDefault(o => o.ReferenceDWG.Equals(referenceDWG));
                    var edgeMap = EdgeMapList.FirstOrDefault(o => o.ReferenceDWG.Equals(referenceDWG));
                    if (nodeMap == null)
                    {
                        return;
                    }

                    nodeMap.NodeMap.ForEach(o =>
                    {
                        o.Value.ForEach(id =>
                        {
                            try
                            {
                                var entity = acad.Element<Entity>(id);
                                acad.ModelSpace.Add(entity.GeometricExtents.ToRectangle());
                            }
                            catch
                            {

                            }
                        });
                    });

                    edgeMap.EdgeMap.ForEach(o =>
                    {
                        o.Value.ForEach(id =>
                        {
                            try
                            {
                                var entity = acad.Element<Entity>(id);
                                acad.ModelSpace.Add(entity.GeometricExtents.ToRectangle());
                            }
                            catch
                            {

                            }
                        });
                    });
                }
            }
        }
    }
}
