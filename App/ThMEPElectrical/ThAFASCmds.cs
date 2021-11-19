using AcHelper;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using System.Linq;
using ThMEPEngineCore.IO;
using ThMEPElectrical.FireAlarmDistance.Data;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using System.IO;
using Newtonsoft.Json;

namespace ThMEPElectrical
{
    public class ThAFASCmds
    {
        [CommandMethod("TIANHUACAD", "THAFAS", CommandFlags.Modal)]
        public void THAFAS()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var result = Active.Editor.GetEntity("\n请选择对象");
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }

                var frame = acadDatabase.Element<Polyline>(result.ObjectId);
                var factory = new ThAFASDistanceDataSetFactory();
                var ds = factory.Create(acadDatabase.Database, frame.Vertices());

                //
                var partId = ds.Container.Where(o => o.Properties["Category"].ToString() == "FireApart").First().Properties["Id"].ToString();
                ds.Container.RemoveAll(o =>
                {
                    if (o.Properties.ContainsKey("ParentId"))
                    {
                        if (o.Properties["ParentId"] == null)
                        {
                            return true;
                        }
                        if (o.Properties["ParentId"].ToString() != partId && o.Properties["Category"].ToString() != "FireApart")
                        {
                            return true;
                        }
                    }
                    return false;
                });
                ThGeoOutput.Output(ds.Container, Active.DocumentDirectory, Active.DocumentName);

                //ThAFASPlacementEngineMgd engine = new ThAFASPlacementEngineMgd();
                //ThAFASPlacementContextMgd context = new ThAFASPlacementContextMgd()
                //{
                //    StepDistance = 20000,
                //    MountMode = ThAFASPlacementMountModeMgd.Wall,
                //};
                //var features = Export2NTSFeatures(engine.Place(geojson, context));

                //var dxfNames = new string[]
                //{
                //    RXClass.GetClass(typeof(Polyline)).DxfName,
                //};
                //var filter = ThSelectionFilterTool.Build(dxfNames);
                //var psr = Active.Editor.GetSelection(filter);
                //if (psr.Status != PromptStatus.OK)
                //{
                //    return;
                //}

                //var objs = new DBObjectCollection();
                //foreach (var obj in psr.Value.GetObjectIds())
                //{
                //    objs.Add(acadDatabase.Element<Polyline>(obj));
                //}
                //objs = objs.BuildArea();

                //var geos = objs
                //    .OfType<Entity>()
                //    .Select(o => new ThGeometry() { Boundary = o })
                //    .ToList();
                //ThGeoOutput.Output(geos, Active.DocumentDirectory, Active.DocumentName);
            }
        }

        private FeatureCollection Export2NTSFeatures(string geojson)
        {
            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(geojson))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return serializer.Deserialize<FeatureCollection>(jsonReader);
            }
        }
    }
}
