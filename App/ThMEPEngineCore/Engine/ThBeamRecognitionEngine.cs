using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.BeamInfo.Model;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using TianHua.AutoCAD.Utility.ExtensionTools;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngine : ThModelRecognitionEngine, IDisposable
    {
        public ThBeamRecognitionEngine()
        {
        }

        public void Dispose()
        {
            //ToDo
        }

        public override void Recognize(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var beamDbExtension = new ThStructureBeamDbExtension(database))
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
                            Elements.Add(CreateIfcBeam(beam, findDbText[0].TextString));
                            //acadDatabase.ModelSpace.Add(ThPolylineExtension.CreateRectangle(findDbText[0].GeometricExtents));
                        }
                        else
                        {
                            Elements.Add(CreateIfcBeam(beam));
                        }
                    }
                }

                //释放获取的梁断
                beamCollection.Dispose();
            }
        }

        private ThIfcBeam CreateIfcBeam(Beam beam, string spec = "")
        {
            ThIfcBeam thIfcBeam;
            if (beam is LineBeam)
            {
                thIfcBeam = new ThIfcLineBeam()
                {
                    Direction = beam.BeamNormal
                };
            }
            else if (beam is ArcBeam)
            {
                thIfcBeam = new ThIfcArcBeam();
            }
            else
            {
                thIfcBeam = new ThIfcBeam();
            }
            thIfcBeam.Uuid = Guid.NewGuid().ToString();
            thIfcBeam.StartPoint = beam.StartPoint;
            thIfcBeam.EndPoint = beam.EndPoint;
            thIfcBeam.Outline = beam.BeamBoundary.Clone() as Entity;
            if (!string.IsNullOrEmpty(spec))
            {
                List<double> datas = ThStructureUtils.GetDoubleValues(spec);
                if (datas.Count == 2)
                {
                    thIfcBeam.Width = datas[0];
                    thIfcBeam.Height = datas[1];
                }
            }
            return thIfcBeam;
        }
    }
}
