using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using TianHua.Electrical.PDS.Engine;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSDistributionExtractService
    {
        public List<ThBlockReferenceData> Results { get; set; } = new List<ThBlockReferenceData>();

        public void Extract(Database database)
        {
            var typeFilter = new List<string>
            {
                "AL",
                "AP",
                "ALE",
                "APE",
                "AW",
                "AC",
                "AR",
                "INT",
                "K",
                "FEL",
            };

            var engine = new ThPDSDistributionExtractionEngine();
            engine.ExtractFromMS(database);
            Results = engine.Results
                .Select(o => o.Data as ThBlockReferenceData)
                .Where(o =>
                {
                    var match = false;
                    for (int i = 0; i < typeFilter.Count && !match; i++)
                    {
                        if (o.Attributes["BOX"].IndexOf(typeFilter[i]) == 0)
                        {
                            match = true;
                        }
                    }
                    return match;
                })
                .ToList();
        }
    }
}
