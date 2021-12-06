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

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Service;

namespace ThMEPElectrical.FireAlarmDistance.Data
{

    public class ThAFASDistanceDataSet
    {
        //input
        public List<ThGeometry> Data { get; private set; }
        private List<string> CleanBlkName { get; set; } = new List<string>();
        private List<string> AvoidBlkNameList { get; set; } = new List<string>();

        //output
        public List<ThGeometry> Room { get; private set; }
        public List<ThGeometry> AvoidEquipments { get; private set; }

        private List<ThGeometry> CleanEquipments { get; set; }


        public ThAFASDistanceDataSet(List<ThGeometry> geom, List<string> cleanBlkName, List<string> avoidBlkNameList)
        {
            this.Data = geom;
            CleanBlkName = cleanBlkName;
            AvoidBlkNameList = avoidBlkNameList;
        }


        public void ClassifyData()
        {
            Room = QueryCategory(BuiltInCategory.Room.ToString());
            var AllEquipment = QueryCategory(BuiltInCategory.Equipment.ToString());
            CleanEquipments = AllEquipment.Where(x => CleanBlkName.Contains(x.Properties["Name"].ToString())).ToList();
            AvoidEquipments = AllEquipment.Where(x => AvoidBlkNameList.Contains(x.Properties["Name"].ToString())).ToList();
        }
        public List<Polyline> GetRoomBoundary()
        {
            var roomPl = Room.Select(x => x.Boundary as Polyline).ToList();
            return roomPl;
        }

        public void CleanData()
        {
            CleanEquipments.ForEach(x =>
            {
                var handle = x.Properties[ThExtractorPropertyNameManager.HandlerPropertyName].ToString();

                var dbTrans = new DBTransaction();
                var objId = dbTrans.GetObjectId(handle);
                var obj = dbTrans.GetObject(objId, OpenMode.ForWrite, false);
                obj.UpgradeOpen();
                obj.Erase();
                obj.DowngradeOpen();
                dbTrans.Commit();
                Data.Remove(x);

            });


        }


        public void ExtendEquipment(List<string> cleanBlkName, double scale)
        {
            var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, scale);

            for (int i = 0; i < AvoidEquipments.Count; i++)
            {
                if (AvoidEquipments[i].Boundary is Polyline pl)
                {
                    AvoidEquipments[i].Boundary = pl.GetOffsetClosePolyline(priorityExtend);
                }
            }
        }

        public void FilterBeam()
        {
            var beam = QueryCategory(BuiltInCategory.Beam.ToString());
            Data.RemoveAll(x => beam.Contains(x));
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
            var DoorOpening = QueryCategory(BuiltInCategory.DoorOpening.ToString());
            var Hole = QueryCategory(BuiltInCategory.Hole.ToString());
            var StoreyBorder = QueryCategory(BuiltInCategory.StoreyBorder.ToString());
            var FireApart = QueryCategory(BuiltInCategory.FireApart.ToString());
            var PlaceCoverage = QueryCategory("PlaceCoverage");
         
            archWall.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0archWall", 3));
            shearWall.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0shearWall", 0));
            Column.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Column", 1));
            Room.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0room", 2));
            DoorOpening.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DoorOpening", 4));
            Hole.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Hole", 5));
            StoreyBorder.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0StoreyBorder", 6));
            FireApart.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0FireApart", 112));
            PlaceCoverage.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0PlaceCoverage", 6));
            AvoidEquipments.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Equipment", 152));

        }

      
    }
}
