using System.Data;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;

using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;

namespace ThMEPWSS.SprinklerConnect.Data
{
    public class ThSprinklerDataQueryService
    {
        //input
        private List<ThGeometry> Data { get; set; } = new List<ThGeometry>();

        //class use
        public List<ThGeometry> ArchitectureWalls { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Shearwalls { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Columns { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Holes { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Rooms { get; private set; } = new List<ThGeometry>();

        //output
        public List<Polyline> ArchitectureWallList { get; private set; } = new List<Polyline>();
        public List<Polyline> ShearWallList { get; private set; } = new List<Polyline>();
        public List<Polyline> ColumnList { get; private set; } = new List<Polyline>();
        public List<Polyline> RoomList { get; private set; } = new List<Polyline>();

        public ThSprinklerDataQueryService(List<ThGeometry> data)
        {
            Data = data;

            PrepareData();
            //CleanData();
        }

        private protected void PrepareData()
        {
            Columns = QueryC(BuiltInCategory.Column.ToString());
            Shearwalls = QueryC(BuiltInCategory.ShearWall.ToString());
            ArchitectureWalls = QueryC(BuiltInCategory.ArchitectureWall.ToString());
            Holes = QueryC(BuiltInCategory.Hole.ToString());
            Rooms = QueryC(BuiltInCategory.Room.ToString());
        }

        //public void CleanData()
        //{
        //    if (CleanBlkName != null)
        //    {
        //        CleanEquipments = Equipments.Where(x => CleanBlkName.Contains(x.Properties["Name"].ToString())).ToList();
        //    }

        //    CleanEquipments.ForEach(x =>
        //    {
        //        var handle = x.Properties[ThExtractorPropertyNameManager.HandlerPropertyName].ToString();

        //        var dbTrans = new DBTransaction();
        //        var objId = dbTrans.GetObjectId(handle);
        //        var obj = dbTrans.GetObject(objId, OpenMode.ForWrite, false);
        //        obj.UpgradeOpen();
        //        obj.Erase();
        //        obj.DowngradeOpen();
        //        dbTrans.Commit();
        //        Data.Remove(x);

        //    });
        //}

        private List<ThGeometry> QueryC(string category)
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

        public void ClassifyData()
        {
            ArchitectureWallList = ArchitectureWalls.Select(x => x.Boundary).OfType<Polyline>().ToList();
            ShearWallList = Shearwalls.Select(x => x.Boundary).OfType<Polyline>().ToList();
            RoomList = Rooms.Select(x => x.Boundary).OfType<Polyline>().ToList();
            ColumnList = Columns.Select(x => x.Boundary).OfType<Polyline>().ToList();
        }
    }
}
