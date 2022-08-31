using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.HydrantConnectPipe.Model;

namespace ThMEPWSS.HydrantConnectPipe.Engine
{
    public class ThHydrantPipeMarkRecognitionEngine
    {
        public DBObjectCollection DBobj { get; private set; }
        public void Extract(Point3dCollection selectArea)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var results = acadDatabase.ModelSpace.OfType<BlockReference>().Where(o => IsHYDTPipeLayer(o.Layer) && IsValve(o.GetEffectiveName())).ToCollection();

                var map = new Dictionary<BlockReference, Polyline>();
                results.OfType<BlockReference>().ForEach(b => map.Add(b, b.ToOBB(b.BlockTransform)));

                var obbs = map.Values.ToCollection();
                //var center = selectArea.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(obbs);
                var newPts = transformer.Transform(selectArea);
                transformer.Transform(obbs);

                var spatialIndex = new ThCADCoreNTSSpatialIndex(obbs);
                var querys = spatialIndex.SelectCrossingPolygon(newPts);

                DBobj = map.Where(o => querys.Contains(o.Value)).Select(o => o.Key).ToCollection();
            }
        }
        public List<ThHydrantPipeMark> GetPipeMarks()
        {
            var pipeMarks = new List<ThHydrantPipeMark>();
            foreach (var db in DBobj)
            {
                var pipeMark = new ThHydrantPipeMark();

                var br = db as BlockReference;
                var pt1 = new Point3d(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点1 Y")), 0);
                var pt2 = new Point3d(br.Position.X + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 X")),
                    br.Position.Y + Convert.ToDouble(br.ObjectId.GetDynBlockValue("节点2 Y")), 0);
                pipeMark.StartPoint = pt1;
                pipeMark.EndPoint = pt2;
                pipeMarks.Add(pipeMark);
            }
            return pipeMarks;
        }

        private bool IsHYDTPipeLayer(string layer)
        {
            return layer.ToUpper() == "W-FRPT-NOTE";
        }

        private bool IsValve(string valve)
        {
            return valve.Contains("消火栓环管标记");
        }
    }
}
