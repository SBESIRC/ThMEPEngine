using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;
using ThMEPEngineCore.Config;

using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;


namespace ThMEPElectrical.FireAlarmSmokeHeat.Model
{
    class ThFaAreaLayoutResult
    {
        public Dictionary<Point3d, Vector3d> layoutPts { get; set; } = new Dictionary<Point3d, Vector3d>();
        public List<Polyline> blind { get; set; } = new List<Polyline>();
        public ThFaAreaLayoutResult()
        {
        }

        public void transformBack(ThMEPOriginTransformer transformer)
        {
            var layoutPtsBack = new Dictionary<Point3d, Vector3d>();
            foreach (var pt in layoutPts)
            {
                layoutPtsBack.Add(transformer.Reset(pt.Key), pt.Value);
            }
            layoutPts = layoutPtsBack;
            blind.ForEach(x => transformer.Reset(x));
        }
    }
}
