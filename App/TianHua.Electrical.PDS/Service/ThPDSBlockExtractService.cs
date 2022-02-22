using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

using ThCADExtension;
using TianHua.Electrical.PDS.Engine;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSBlockExtractService
    {
        /// <summary>
        /// 标注块
        /// </summary>
        public List<ThBlockReferenceData> MarkBlocks { get; set; } = new List<ThBlockReferenceData>();

        /// <summary>
        /// 负载块
        /// </summary>
        public List<ThBlockReferenceData> LoadBlocks { get; set; } = new List<ThBlockReferenceData>();

        /// <summary>
        /// 配电箱
        /// </summary>
        public List<ThBlockReferenceData> DistBoxBlocks { get; set; } = new List<ThBlockReferenceData>();

        public void Extract(Database database, List<string> nameFilter, List<string> propertyFilter, List<int> distBoxFilter)
        {
            var engine = new ThPDSBlockExtractionEngine
            {
                NameFilter = nameFilter.Distinct().ToList(),
                PropertyFilter = propertyFilter.Distinct().ToList(),
                DistBoxFilter = distBoxFilter,
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
                        return;
                    }
                    else if (block.EffectiveName.Contains("E-BDB006-1"))
                    {
                        DistBoxBlocks.Add(block);
                        return;
                    }

                    var checker = false;
                    block.Attributes.Values.ForEach(o =>
                    {
                        if (!checker)
                        {
                            for (var i = 0; i < propertyFilter.Count; i++)
                            {
                                if (distBoxFilter.Contains(i))
                                {
                                    if (o.IndexOf(propertyFilter[i]) == 0)
                                    {
                                        checker = true;
                                    }
                                }
                            }
                        }
                    });
                    if (checker)
                    {
                        DistBoxBlocks.Add(block);
                    }
                    else
                    {
                        LoadBlocks.Add(block);
                    }
                });
        }
    }
}
