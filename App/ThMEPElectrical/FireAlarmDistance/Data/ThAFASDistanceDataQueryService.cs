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
using ThMEPEngineCore.Config;
using NetTopologySuite.Geometries;

using ThMEPElectrical.AFAS;
using ThMEPElectrical.AFAS.Utils;
using ThMEPElectrical.FireAlarmArea.Service;

namespace ThMEPElectrical.FireAlarmDistance.Data
{

    public class ThAFASDistanceDataQueryService
    {
        //input
        public List<ThGeometry> Data { get; private set; }
        //private List<string> CleanBlkName { get; set; } = new List<string>();
        private List<string> AvoidBlkNameList { get; set; } = new List<string>();
        private List<RoomTableTree> RoomConfigTree;
        //output
        public List<ThGeometry> Rooms { get; private set; }
        public List<ThGeometry> AvoidEquipments { get; private set; }

        private List<ThGeometry> CleanEquipments { get; set; }

        public List<ThGeometry> DoorOpenings { get; set; } = new List<ThGeometry>();
        public List<ThGeometry> Windows { get; set; } = new List<ThGeometry>();


        public ThAFASDistanceDataQueryService(List<ThGeometry> geom, List<string> avoidBlkNameList)
        {
            string roomConfigUrl = ThCADCommon.RoomConfigPath();
            RoomConfigTree = ThAFASRoomUtils.ReadRoomConfigTable(roomConfigUrl);

            this.Data = geom;
            //CleanBlkName = cleanBlkName;
            AvoidBlkNameList = avoidBlkNameList;

            PrepareData();
        }


        private void PrepareData()
        {
            Rooms = ThAFASUtils.QueryCategory(Data,BuiltInCategory.Room.ToString());
            DoorOpenings = ThAFASUtils.QueryCategory(Data,BuiltInCategory.DoorOpening.ToString());
            Windows = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Window.ToString());
            var allEquipments = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Equipment.ToString());
            //CleanEquipments = allEquipments.Where(x => CleanBlkName.Contains(x.Properties["Name"].ToString())).ToList();
            AvoidEquipments = allEquipments.Where(x => AvoidBlkNameList.Contains(x.Properties["Name"].ToString())).ToList();
        }

        public List<Polyline> GetRoomBoundary()
        {
            var roomPl = new List<Polyline>();

  for (int i =0;i<Rooms .Count;i++)
            {
                if (Rooms [i].Boundary is Polyline pl )

                {
                    roomPl.Add(pl);
                }        
                else if (Rooms[i].Boundary is MPolygon mpl)
                {
                    roomPl.Add(mpl.Shell());
                    DrawUtils.ShowGeometry(mpl, "l0mroom");
                }
            }





            return roomPl;
        }

        public void CleanPreviousEquipment()
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

        public void ProcessRoomPlacementLabel(string layoutType)
        {
            AddRoomPlacementLabel(layoutType);
            RemoveRoom();
        }

        private void RemoveRoom()
        {
            var roomClean = new List<ThGeometry>();
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Properties.TryGetValue(ThExtractorPropertyNameManager.PlacementPropertyName, out var placementLable))
                {
                    if (placementLable != null && placementLable.ToString() == ThFaDistCommon.LayoutTagDict[ThFaDistCommon.LayoutTagRemove])
                    {
                        roomClean.Add(Rooms[i]);
                    }
                }
            }

            Data.RemoveAll(x => roomClean.Contains(x));
            Rooms.RemoveAll(x => roomClean.Contains(x));

        }
        private void AddRoomPlacementLabel(string layoutType)
        {
            foreach (var room in Rooms)
            {
                var roomName = room.Properties[ThExtractorPropertyNameManager.NamePropertyName].ToString();
                var roomTag = RoomConfigTreeService.getRoomTag(RoomConfigTree, roomName);
                var layoutTag = roomTag.Where(x => x.Contains(layoutType)).FirstOrDefault();
                var added = false;

                if (layoutTag != null)
                {
                    layoutTag = layoutTag.Replace(layoutType, "");

                    if (layoutTag != null && layoutTag != "" && ThFaDistCommon.LayoutTagDict.TryGetValue(layoutTag, out var labelInGeom))
                    {
                        added = true;
                        room.Properties.Add(ThExtractorPropertyNameManager.PlacementPropertyName, labelInGeom);
                    }
                }
                if (added == false)
                {
                    var labelInGeom = ThFaDistCommon.LayoutTagDict[ThFaDistCommon.LayoutTagRemove];
                    room.Properties.Add(ThExtractorPropertyNameManager.PlacementPropertyName, labelInGeom);
                }
            }
        }

        public void ExtendPriority(List<string> cleanBlkName, double scale)
        {
            var priorityExtend = ThAFASUtils.GetPriorityExtendValue(cleanBlkName, scale);

            //for (int i = 0; i < AvoidEquipments.Count; i++)
            //{
            //    if (AvoidEquipments[i].Boundary is Polyline pl)
            //    {
            //        AvoidEquipments[i].Boundary = pl.GetOffsetClosePolyline(priorityExtend);
            //    }
            //}

            ThAFASUtils.ExtendPriority(AvoidEquipments, priorityExtend);
        }

        public void FilterBeam()
        {
            var beam = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Beam.ToString());
            Data.RemoveAll(x => beam.Contains(x));
        }

        //private List<ThGeometry> QueryCategory(string category)
        //{
        //    var result = new List<ThGeometry>();
        //    foreach (ThGeometry geo in Data)
        //    {
        //        if (geo.Properties[ThExtractorPropertyNameManager.CategoryPropertyName].ToString() == category)
        //        {
        //            result.Add(geo);
        //        }
        //    }
        //    return result;
        //}

        public void Print()
        {
            var Storeys = ThAFASUtils.QueryCategory(Data, BuiltInCategory.StoreyBorder.ToString());
            var FireAparts = ThAFASUtils.QueryCategory(Data, BuiltInCategory.FireApart.ToString());
            var ArchitectureWalls = ThAFASUtils.QueryCategory(Data, BuiltInCategory.ArchitectureWall.ToString());
            var Shearwalls = ThAFASUtils.QueryCategory(Data, BuiltInCategory.ShearWall.ToString());
            var Columns = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Column.ToString());
            var beam = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Beam.ToString());
            var FireProofs = ThAFASUtils.QueryCategory(Data, BuiltInCategory.FireproofShutter.ToString());
            var Railings = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Railing.ToString());
            var Holes = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Hole.ToString());
            var CenterLine = ThAFASUtils.QueryCategory(Data, BuiltInCategory.CenterLine.ToString());
            var PlaceArea = ThAFASUtils.QueryCategory(Data, "PlaceCoverage");

            Storeys.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Storeys", 2));
            FireAparts.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0FireApart", 112));
            ArchitectureWalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0archWall", 1));
            Shearwalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0shearWall", 3));
            Columns.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Column", 1));
            Windows.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Window", 4));
            Rooms.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0room", 30));
            beam.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0beam", 190));
            DoorOpenings.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DoorOpening", 4));
            FireProofs.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0FireProofs", 4));
            Railings.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Railings", 4));
            Holes.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Hole", 150));
            CenterLine.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Centerline", 230));
            PlaceArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0PlaceCoverage", 6));
            AvoidEquipments.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Equipment", 152));

        }


    }
}
