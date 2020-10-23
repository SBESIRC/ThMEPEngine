using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.BeamInfo;
using ThMEPEngineCore.BeamInfo.Model;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThRawBeamRecognitionEngine : ThBuildingElementRecognitionEngine
    {
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
                thDisBeamInfo.CalBeamStruc(beamCollection).Cast<LineBeam>().ForEach(o =>
                {
                    Elements.Add(ThIfcLineBeam.Create(o.BeamBoundary));
                });

                //获取BeamText
                //using (var beamTextDbExtension = new ThStructureBeamTextDbExtension(Active.Database))
                //{
                //    // 获取图纸中的梁标注
                //    beamTextDbExtension.BuildElementTexts();
                //    // 为梁标注文字建立空间索引
                //    DBObjectCollection dbtexts = new DBObjectCollection();
                //    beamTextDbExtension.BeamTexts.ForEach(o => dbtexts.Add(o));
                //    var dbTextSpatialIndex = ThSpatialIndexService.CreateTextSpatialIndex(dbtexts);
                //    foreach (var beam in beams)
                //    {
                //        ThBeamMarkingService thBeamMarkingService = null;
                //        if (beam is LineBeam lineBeam)
                //        {
                //            thBeamMarkingService = new ThLineBeamMarkingService(lineBeam);
                //        }
                //        else if (beam is ArcBeam arcBeam)
                //        {
                //            thBeamMarkingService = new ThArcBeamMarkingService(arcBeam);
                //        }
                //        List<DBText> findDbText = thBeamMarkingService.Match(dbTextSpatialIndex);
                //        if (findDbText.Count > 0)
                //        {
                //            Elements.Add(CreateIfcBeam(beam, findDbText[0].TextString));
                //        }
                //        else
                //        {
                //            Elements.Add(CreateIfcBeam(beam));
                //        }
                //    }
                //}
            }
        }
    }
}