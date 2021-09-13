using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPWSS.Hydrant.Data;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDataSetFactory
    {
        public ThMEPDataSet Create(Database database, Point3dCollection collection)
        {
            // 获取原材料
            GetElements(database, collection);

            // 加工原材料
            return BuildDataSet();
        }

        /// <summary>
        /// 获取建筑元素
        /// </summary>
        private void GetElements(Database database, Point3dCollection collection)
        {
            using (var acad = AcadDatabase.Active())
            {
                var extractors = new List<ThExtractorBase>()
                {
                    
                    new ThRoomExtractor()
                    {
                        UseDb3Engine=true,
                    },
                    new ThHydrantArchitectureWallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        ElementLayer = "",
                    },
                    new ThHydrantShearwallExtractor()
                    {
                        UseDb3Engine=true,
                        IsolateSwitch=true,
                        ElementLayer = "",
                    },
                    new ThHydrantDoorOpeningExtractor()
                    {
                        UseDb3Engine=false,
                        ElementLayer = "AI-Door,AI-门,门",
                    },
                    new ThColumnExtractor()
                    {
                        UseDb3Engine = true,
                        IsolateSwitch = true,
                        ElementLayer = "",
                    }
                };
                extractors.ForEach(e => e.Extract(acad.Database, collection));

            }
        }

        /// <summary>
        /// 创建数据集
        /// </summary>
        private ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet();
        }
    }
}
