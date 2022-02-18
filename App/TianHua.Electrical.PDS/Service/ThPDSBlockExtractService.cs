using System.IO;
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
        readonly static string LoadConfigUrl = Path.Combine(ThCADCommon.SupportPath(), "平面关注对象.xlsx");

        /// <summary>
        /// 标注块
        /// </summary>
        public List<ThBlockReferenceData> MarkBlocks = new List<ThBlockReferenceData>();

        /// <summary>
        /// 负载块
        /// </summary>
        public List<ThBlockReferenceData> LoadBlocks = new List<ThBlockReferenceData>();

        /// <summary>
        /// 配电箱
        /// </summary>
        public List<ThBlockReferenceData> DistBoxBlocks = new List<ThBlockReferenceData>();

        /// <summary>
        /// 配电箱序列
        /// </summary>
        public List<int> DistBoxFilter = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        public void Extract(Database database)
        {
            var nameFilter = new List<string>();
            var propertyFilter = new List<string>();
            var nameService = new ThLoadNameService();
            var tableInfo = nameService.Acquire(LoadConfigUrl);
            tableInfo.ForEach(o =>
            {
                if (string.IsNullOrEmpty(o.Properties))
                {
                    nameFilter.Add(o.BlockName);
                }
                else
                {
                    propertyFilter.Add(o.Properties);
                }
            });

            var engine = new ThPDSBlockExtractionEngine
            {
                NameFilter = nameFilter.Distinct().ToList(),
                PropertyFilter = propertyFilter.Distinct().ToList(),
                DistBoxFilter = DistBoxFilter,
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
                                if (DistBoxFilter.Contains(i))
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
