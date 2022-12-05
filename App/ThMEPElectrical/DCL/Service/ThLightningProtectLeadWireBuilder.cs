#if (ACAD2016 || ACAD2018)
using CLI;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Service;
using ThMEPElectrical.DCL.Data;
#endif

using System;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPElectrical.DCL.Service
{
    /// <summary>
    /// Lightning Protection Down Conductors Test Data(防雷保护引下线)
    /// </summary>
    public class ThLightningProtectLeadWireBuilder:IDisposable
    {
        private List<ThEStoreys> _estoreys = new List<ThEStoreys>();
        private int _levelIndex = 3;
        public ThLightningProtectLeadWireBuilder(List<ThEStoreys> estoreys, int levelIndex)
        {
            _estoreys = estoreys;
            _levelIndex = levelIndex;
        }
        public void Dispose()
        {

        }
#if (ACAD2016 || ACAD2018)
        public void Build()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var dataSetFactory = new ThDclDataSetFactory(_estoreys);
                var dataSet = dataSetFactory.Create(acadDb.Database, new Point3dCollection());
                dataSetFactory.PrintOuterColumns(2);
                dataSetFactory.PrintOtherColumns(3);
                dataSetFactory.PrintOuterShearWalls(4);
                dataSetFactory.PrintOtherShearWalls(6);
                dataSetFactory.PrintArchOutlines(7);

#if DEBUG
                // 对接浙大算法
                string geoContent = ThGeoOutput.Output(dataSet.Container);
                var dclLayoutEngine = new ThDCLayoutEngineMgd();
                var data = new ThDCDataMgd();
                data.ReadFromContent(geoContent);
                var param = new ThDCParamMgd(_levelIndex);
                var result = dclLayoutEngine.Run(data, param);
                var parseResults = ThDclResultParseService.Parse(result);
                var printService = new ThDclPrintService(acadDb.Database, "AI-DCL");
                printService.Print(parseResults);
#endif
            }
        }
#endif
    }
}
