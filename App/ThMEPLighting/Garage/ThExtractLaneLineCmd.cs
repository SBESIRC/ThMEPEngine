using System;
using System.Linq;
using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using AcHelper;
using System.Collections.Generic;
using ThMEPLighting.Garage.Engine;

namespace ThMEPLighting.Garage
{
    public class ThExtractLaneLineCmd : ThMEPBaseCommand, IDisposable
    {
        private List<string> Layers { get; set; }
        public ThExtractLaneLineCmd(List<string> layers)
        {
            Layers = layers;
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var acdb = AcadDatabase.Active())
            {
                var lanelines = Extract();

            }
        }

        private DBObjectCollection Extract()
        {
            using (var acdb = AcadDatabase.Active())
            {
                var results = new DBObjectCollection();
                var extraction = new ThLaneLineExtractionEngine();
                extraction.CheckQualifiedLayer = CheckLayerValid;
                extraction.Extract(acdb.Database);
                return results;
            }
        }

        private bool CheckLayerValid(Entity entity)
        {
            return Layers.Where(o => string.Compare(o, entity.Layer)==0).Any();
        }
    }
}
