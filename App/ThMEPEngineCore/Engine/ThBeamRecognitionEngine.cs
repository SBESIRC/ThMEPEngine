using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using TianHua.AutoCAD.Utility.ExtensionTools;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngine : ThModelRecognitionEngine, IDisposable
    {
        public override List<ThIfcElement> Elements { get; set ; }
        public ThBeamRecognitionEngine()
        {
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (var beamDbExtension = new ThStructureBeamDbExtension(Active.Database))
            {
                //获取梁线
                beamDbExtension.BuildElementCurves();

                //获取梁段
                DBObjectCollection beamCollection = new DBObjectCollection();
                beamDbExtension.BeamCurves.ForEach(o => beamCollection.Add(o));
                ThDistinguishBeamInfo thDisBeamInfo = new ThDistinguishBeamInfo();
                var beams = thDisBeamInfo.CalBeamStruc(beamCollection);

                //获取BeamText
                using (var beamTextDbExtension = new ThStructureBeamTextDbExtension(Active.Database))
                {
                    // 获取图纸中的梁标注
                    beamTextDbExtension.BuildElementTexts();

                    // 为梁标注文字建立空间索引
                    DBObjectCollection dbtexts = new DBObjectCollection();
                    beamTextDbExtension.BeamTexts.ForEach(o => dbtexts.Add(o));
                    var dbTextSpatialIndex = ThSpatialIndexService.CreateTextSpatialIndex(dbtexts);
                    foreach(var beam in beams)
                    {
                        ThBeamMarkingService thBeamMarkingService = new ThBeamMarkingService(beam);
                        List<DBText> findDbText = thBeamMarkingService.Match(dbTextSpatialIndex);
                        if (findDbText.Count == 1)
                        {
                            acadDatabase.ModelSpace.Add(ThPolylineExtension.CreateRectangle(findDbText[0].GeometricExtents));
                        }
                    }
                }
            }
        }
    }
}
