using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore.Algorithm;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Model
{
    class ThFaAreaLayoutResult
    {
        public Dictionary<Point3d, Vector3d> LayoutPts { get; set; } = new Dictionary<Point3d, Vector3d>();
        public List<Polyline> Blind { get; set; } = new List<Polyline>();
        public ThFaAreaLayoutResult()
        {
        }

        public void TransformBack(ThMEPOriginTransformer transformer)
        {
            var layoutPtsBack = new Dictionary<Point3d, Vector3d>();
            foreach (var pt in LayoutPts)
            {
                layoutPtsBack.Add(transformer.Reset(pt.Key), pt.Value);
            }
            LayoutPts = layoutPtsBack;
            Blind.ForEach(x => transformer.Reset(x));
        }
    }
}
