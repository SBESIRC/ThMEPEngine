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
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.LaneLine;
using NetTopologySuite.Geometries;

using ThMEPElectrical.FireAlarmSmokeHeat.Service;
using ThMEPElectrical.FireAlarm.Service;

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
        public void ExtendEquipment(List<string> cleanBlkName, double scale)
        {
            var priorityExtend = ThFaAreaLayoutParamterCalculationService.GetPriorityExtendValue(cleanBlkName, scale);
            var equipment = QueryCategory(BuiltInCategory.Equipment.ToString());
            for (int i = 0; i < equipment.Count; i++)
            {
                if (equipment[i].Boundary is Polyline pl)
                {
                    equipment[i].Boundary = pl.GetOffsetClosePolyline(priorityExtend);
                }
            }
        }

        public void FilterBeam()
        {
            var beam = QueryCategory(BuiltInCategory.Beam.ToString());
            Data .RemoveAll (x=> beam.Contains (x));
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

        public void print()
        {
            var archWall = QueryCategory(BuiltInCategory.ArchitectureWall.ToString());
            var shearWall = QueryCategory(BuiltInCategory.ShearWall.ToString());
            var Column = QueryCategory(BuiltInCategory.Column.ToString());
            var room = QueryCategory(BuiltInCategory.Room.ToString());
            var DoorOpening = QueryCategory(BuiltInCategory.DoorOpening.ToString());
            var Hole = QueryCategory(BuiltInCategory.Hole.ToString());
            var StoreyBorder = QueryCategory(BuiltInCategory.StoreyBorder.ToString());
            var FireApart = QueryCategory(BuiltInCategory.FireApart.ToString());
            var PlaceCoverage = QueryCategory("PlaceCoverage");
            var Equipment = QueryCategory(BuiltInCategory.Equipment.ToString());


            archWall.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0archWall", 3));
            shearWall.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0shearWall", 0));
            Column.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Column", 1));
            room.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0room", 2));
            DoorOpening.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DoorOpening", 4));
            Hole.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Hole", 5));
            StoreyBorder.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0StoreyBorder", 6));
            FireApart.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0FireApart", 112));
            PlaceCoverage.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0PlaceCoverage", 6));
            Equipment.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Equipment", 152));



        }

    }
}
