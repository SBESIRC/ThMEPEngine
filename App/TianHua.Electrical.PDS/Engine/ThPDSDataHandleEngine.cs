using System.Collections.Generic;

using Linq2Acad;

using ThCADExtension;
using ThMEPElectrical.BlockConvert;
using TianHua.Electrical.PDS.Model;
using TianHua.Electrical.PDS.Service;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThPDSDataHandleEngine
    {
        public void DataHandle()
        {
            using (var acad = AcadDatabase.Active())
            {
                var pdsLoads = new List<ThPDSLoad>();
                var markExtractor = new ThCircuitMarkExtractionEngine();
                markExtractor.ExtractFromMS(acad.Database);

                var loadExtractService = new ThPDSLoadExtractService();
                loadExtractService.Extract(acad.Database);
                loadExtractService.LoadBlocks.ForEach(load =>
                {
                    if (load.EffectiveName.Contains("电动机及负载标注"))
                    {
                        var pdsLoadID = new ThPDSID
                        {
                            LoadID = ThBConvertUtils.LoadSN(load),
                            Description = ThBConvertUtils.LoadUsage(load),
                        };
                        var location = new ThPDSLocation
                        {
                            ReferenceDWG = load.Database,
                            BasePoint = load.Position,
                        };

                        var pdsLoad = new ThPDSLoad
                        {
                            ID = pdsLoadID,
                            Location = location,
                        };
                    }
                });


                var distributionExtractService = new ThPDSDistributionExtractService();
                var distributions = distributionExtractService.Extract(acad.Database);

            }

        }
    }
}
