using AcHelper;
using Linq2Acad;
using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.BeamInfo.Model;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPEngineCore.Engine
{
    public class ThBeamRecognitionEngine : ThBuildingElementRecognitionEngine, IDisposable
    {
        public ThBeamRecognitionEngine()
        {
        }

        public void Dispose()
        {
        }

        public override void Recognize(Database database, Point3dCollection polygon)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var beamDbExtension = new ThStructureBeamDbExtension(database))
            {
                //获取梁线
                beamDbExtension.BuildElementCurves();
                List<Curve> curves = new List<Curve>();
                if (polygon.Count > 0)
                {
                    DBObjectCollection dbObjs = new DBObjectCollection();
                    beamDbExtension.BeamCurves.ForEach(o => dbObjs.Add(o));
                    ThCADCoreNTSSpatialIndex beamCurveSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                    foreach (var filterObj in beamCurveSpatialIndex.SelectCrossingPolygon(polygon))
                    {
                        curves.Add(filterObj as Curve);
                    }
                }
                else
                {
                    curves = beamDbExtension.BeamCurves;
                }
                //获取梁段
                DBObjectCollection beamCollection = new DBObjectCollection();
                curves.ForEach(o => beamCollection.Add(o));
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
                    foreach (var beam in beams)
                    {
                        ThBeamMarkingService thBeamMarkingService=null;
                        if (beam is LineBeam lineBeam)
                        {
                            thBeamMarkingService = new ThLineBeamMarkingService(lineBeam);
                        }
                        else if(beam is ArcBeam arcBeam)
                        {
                            thBeamMarkingService = new ThArcBeamMarkingService(arcBeam);
                        }
                        List<DBText> findDbText = thBeamMarkingService.Match(dbTextSpatialIndex);
                        if (findDbText.Count > 0)
                        {
                            Elements.Add(CreateIfcBeam(beam, findDbText[0].TextString));
                        }
                        else
                        {
                            Elements.Add(CreateIfcBeam(beam));
                        }
                    }
                }
            }
        }

        public void Split(
            ThColumnRecognitionEngine thColumnRecognitionEngine,
            ThShearWallRecognitionEngine thShearWallRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            ThSplitBeamEngine thSplitBeams = new ThSplitBeamEngine(
                this,
                thColumnRecognitionEngine,
                thShearWallRecognitionEngine,
                thSpatialIndexManager);
            thSplitBeams.Split();
            Elements = thSplitBeams.BeamElements;
        }

        public void Snap(
            ThColumnRecognitionEngine thColumnRecognitionEngine,
            ThShearWallRecognitionEngine thShearWallRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            ThSnapBeamEngine thSnapBeams = new ThSnapBeamEngine(
                this,
                thColumnRecognitionEngine,
                thShearWallRecognitionEngine,
                thSpatialIndexManager);
            thSnapBeams.Snap();
            Elements = thSnapBeams.BeamElements;
        }

        public void Merge(ThSpatialIndexManager thSpatialIndexManager)
        {
            ThMergeBeamEngine thMergeBeams = new ThMergeBeamEngine(
                this,
                thSpatialIndexManager);
            thMergeBeams.Merge();
            Elements = thMergeBeams.BeamElements;

        }
        public void Measure(ThSpatialIndexManager spatialIndexManager)
        {
            List<ThIfcElement> duplicateCollection = new List<ThIfcElement>();
            ValidElements.ForEach(m =>
            {
                if (!duplicateCollection.Where(n => n.Uuid == m.Uuid).Any())
                {
                    DBObjectCollection dbObjs = spatialIndexManager.BeamSpatialIndex.SelectCrossingPolygon(m.Outline as Polyline);
                    Polyline baseOutline = m.Outline as Polyline;
                    foreach (DBObject dbObj in dbObjs)
                    {
                        ThIfcElement thIfcElement = FilterByOutline(dbObj);
                        if (thIfcElement.Uuid != m.Uuid)
                        {
                            double measure = baseOutline.SimilarityMeasure(thIfcElement.Outline as Polyline);
                            if (measure >= ThMEPEngineCoreCommon.SIMILARITYMEASURETOLERANCE)
                            {
                                duplicateCollection.Add(thIfcElement);
                            }
                        }
                    }
                }
            });
            ValidElements = ValidElements.Where(m => !duplicateCollection.Where(n => n.Uuid == m.Uuid).Any()).ToList();
        }

        private ThIfcBeam CreateIfcBeam(Beam beam, string spec = "")
        {
            ThIfcBeam thIfcBeam=null;
            if (beam is LineBeam)
            {
                thIfcBeam = new ThIfcLineBeam()
                {
                    Normal = beam.BeamNormal
                };
            }
            else if (beam is ArcBeam)
            {
                thIfcBeam = new ThIfcArcBeam();
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
