using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;


namespace ThMEPElectrical.FireAlarmDistance.Data
{

    public class ThAFASDistanceDataSet
    {
        public List<ThGeometry> Data { get; private set; }

        public ThAFASDistanceDataSet(List<ThGeometry> geom)
        {
            this.Data = geom;
        }
        public List<Polyline> GetRoom()
        {
            var roomGeom = QueryCategory(BuiltInCategory.Room.ToString());
            var roomPl = roomGeom.Select(x => x.Boundary as Polyline).ToList();
            return roomPl;
        }
        public List<ThGeometry> GetRoomGeom()
        {
            var roomGeom = QueryCategory(BuiltInCategory.Room.ToString());
            return roomGeom;
        }
        private List<ThGeometry> QueryCategory(string category)
        {
            var result = new List<ThGeometry>();
            foreach (ThGeometry geo in Data)
            {
                if (geo.Properties[ThExtractorPropertyNameManager.CategoryPropertyName].ToString() == category)
                {
                    result.Add(geo);
                }
            }
            return result;
        }

    }
}
