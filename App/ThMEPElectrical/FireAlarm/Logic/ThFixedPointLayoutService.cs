using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.FireAlarm.Logic
{
    public abstract class ThFixedPointLayoutService
    {
        protected ThDataQueryService DataQueryWorker { get; set; }

        public ThFixedPointLayoutService(List<ThGeometry> totalData)
        {
            DataQueryWorker = new ThDataQueryService(totalData);
        }
        public abstract List<KeyValuePair<Point3d, Vector3d>> Layout();
    }
}
