using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;

namespace ThMEPHVAC.IndoorFanLayout.DataEngine
{
    class ThIndoorFanData
    {
        string loadLayerName = "AI-负荷通风标注";
        ThMEPOriginTransformer _originTransformer;
        public ThIndoorFanData(ThMEPOriginTransformer originTransformer)
        {
            _originTransformer = originTransformer;
        }
        public List<Curve> GetAllAxisCurves()
        {
            var retAxisCurves = new List<Curve>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var axisEngine = new ThAXISLineRecognitionEngine();
                axisEngine.Recognize(acdb.Database, new Point3dCollection());
                foreach (var item in axisEngine.Elements)
                {
                    if (item == null || item.Outline == null)
                        continue;
                    if (item.Outline is Curve curve)
                    {
                        var copy = (Curve)curve.Clone();
                        if (null != _originTransformer)
                            _originTransformer.Transform(copy);
                        retAxisCurves.Add(copy);
                    }
                }
            }
            return retAxisCurves;
        }
        public List<Table> GetAllRoomLoadTable() 
        {
            var loadTables = new List<Table>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var tables = acdb.ModelSpace.OfType<Table>().ToList();
                foreach(var table in tables) 
                {
                    if (table.Layer != loadLayerName)
                        continue;
                    var copy = (Table)table.Clone();
                    if (null != _originTransformer)
                        _originTransformer.Transform(copy);
                    loadTables.Add(copy);
                }
            }
            return loadTables;
        }
        public List<Curve> GetAllLeadLine() 
        {
            var leadCurves = new List<Curve>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var curves = acdb.ModelSpace.OfType<Curve>().ToList();
                foreach (var curve in curves)
                {
                    if (curve.Layer != loadLayerName)
                        continue;
                    if (curve is Line || curve is Polyline) 
                    {
                        var copy = (Curve)curve.Clone();
                        if (null != _originTransformer)
                            _originTransformer.Transform(copy);
                        leadCurves.Add(copy);
                    }
                }
            }
            return leadCurves;
        }
    }
}
