﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.HydrantConnectPipe.Engine
{
    public class ThOtherLineRecognitionEngine
    {
        public DBObjectCollection Dbjs { get; private set; }
        public void Extract(Point3dCollection selectArea)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var results = acadDatabase.ModelSpace.OfType<Entity>().Where(o => IsHydrantLineLayer(o.Layer));

                var spatialIndex = new ThCADCoreNTSSpatialIndex(results.ToCollection())
                {
                    AllowDuplicate = true,
                };
                Dbjs = spatialIndex.SelectCrossingPolygon(selectArea);
            }
        }
        private bool IsHydrantLineLayer(string layer)
        {
            if (layer.ToUpper().Contains("W-") && layer.ToUpper().Contains("-PIPE") && (!layer.ToUpper().Contains("HYDT")))
            {
                return true;
            }
            return false;
        }
    }
}
