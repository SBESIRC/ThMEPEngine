using System.IO;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

using ThCADExtension;
using TianHua.Electrical.PDS.Engine;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSLoadExtractService
    {
        readonly static string LoadConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "平面关注对象.xlsx");
        public List<ThBlockReferenceData> MarkBlocks = new List<ThBlockReferenceData>();
        public List<ThBlockReferenceData> LoadBlocks = new List<ThBlockReferenceData>();

        public void Extract(Database database)
        {
            var nameFilter = new List<string>
            {
                "E-BDB052",
                "E-BDB051",
                "E-BDB054",
                "电动机及负载标注",
                "电动机及负载标注2",
                "水泵标注",
                "E-BDB001",
                "E-电力平面-负荷明细",
            };

            var nameService = new ThLoadNameService();
            var engine = new ThPDSLoadExtractionEngine
            {
                NameFilter = nameFilter,
                //NameFilter = nameService.Acquire(LoadConfigUrl),
            };
            engine.ExtractFromMS(database);
            engine.Results.Select(o => o.Data as ThBlockReferenceData)
                .ForEach(block =>
                {
                    if (block.EffectiveName.IndexOf("负载标注") == 0
                        || block.EffectiveName.Contains("水泵标注")
                        || block.EffectiveName.Contains("E-电力平面-负荷明细"))
                    {
                        MarkBlocks.Add(block);
                    }
                    else
                    {
                        LoadBlocks.Add(block);
                    }
                });
        }
    }
}
