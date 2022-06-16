using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using TianHua.Electrical.PDS.Engine;
using TianHua.Electrical.PDS.Model;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSFilterBlockExtractService
    {
        public ThPDSFilterBlockExtractService()
        {
            Ignore = new List<Polyline>();
            Attached = new List<Polyline>();
            Terminal = new List<Polyline>();
        }

        public List<Polyline> Ignore;
        public List<Polyline> Attached;
        public List<Polyline> Terminal;

        public void Extract(Database database, List<ThPDSFilterBlockInfo> tableInfo)
        {
            var nameFilter = new List<string>();
            var propertyFilter = new List<string>();
            tableInfo.ForEach(t =>
            {
                if (string.IsNullOrEmpty(t.Properties))
                {
                    nameFilter.Add(t.BlockName);
                }
                else
                {
                    propertyFilter.Add(t.Properties);
                }
            });
            var engine = new ThPDSBlockExtractionEngine
            {
                NameFilter = nameFilter.Distinct().ToList(),
                PropertyFilter = propertyFilter.Distinct().ToList(),
                //DistBoxKey = distBoxKey,
            };
            engine.ExtractFromMS(database);
        }
    }
}
