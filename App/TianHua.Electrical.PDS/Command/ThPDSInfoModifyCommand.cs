using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Command;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Command
{
    public class ThPDSInfoModifyCommand : ThMEPBaseCommand, IDisposable
    {
        private List<ThPDSNodeMap> NodeMapList;

        private List<ThPDSEdgeMap> EdgeMapList;

        public ThPDSInfoModifyCommand(List<ThPDSNodeMap> nodeMapList, List<ThPDSEdgeMap> edgeMapList)
        {
            NodeMapList = nodeMapList;
            EdgeMapList = edgeMapList;
        }

        public override void SubExecute()
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
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
