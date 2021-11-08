using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
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
        public List<ThGeometry> mainPipe { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> subMainPipe { get; private set; } = new List<ThGeometry>();
        //output
        public List<Polyline> architectureWallList { get; private set; } = new List<Polyline>();
        public List<Polyline> shearWallList { get; private set; } = new List<Polyline>();
        public List<Polyline> columnList { get; private set; } = new List<Polyline>();
        public List<Polyline> roomList { get; private set; } = new List<Polyline>();
        public List<Polyline> mainPipeList { get; private set; } = new List<Polyline>();
        public List<Polyline> subMainPipeList { get; private set; } = new List<Polyline>();
        public ThSprinklerDataQueryService(List<ThGeometry> data)
        {
            Data = data;
       
            PrepareData();
            //CleanData();
        }

        protected void PrepareData()
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

        //public void analysisHoles()
        //{
        //    var holesTemp = Holes.Select(x => x.Boundary as Polyline).ToList();
        //    var allHolesList = new List<Polyline>();

        //    for (int i = 0; i < Rooms.Count; i++)
        //    {
        //        Polyline frame = null;
        //        List<Polyline> holes = new List<Polyline>();
        //        if (Rooms[i].Boundary is MPolygon mPolygon)
        //        {
        //            frame = mPolygon.Shell();
        //            holes.AddRange(mPolygon.Holes());
        //        }
        //        else if (Rooms[i].Boundary is Polyline polyline)
        //        {
        //            frame = polyline;
        //        }


        //        for (int j = 0; j < holesTemp.Count; j++)
        //        {
        //            var jGeom = holesTemp[j];
        //            jGeom.Closed = true;

        //            if (allHolesList.Contains(jGeom) == false)
        //            {
        //                ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(frame, jGeom);
        //                if (relation.IsContains)
        //                {
        //                    allHolesList.Add(jGeom);
        //                    holes.Add(jGeom);
        //                }
        //                else if (relation.IsIntersects && relation.IsOverlaps)
        //                {
        //                    var polyCollection = new DBObjectCollection() { frame };
        //                    var overlap = jGeom.Intersection(polyCollection);

        //                    if (overlap.Count > 0)
        //                    {
        //                        var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();

        //                        if (overlapPoly.Area / jGeom.Area > 0.2)
        //                        {
        //                            allHolesList.Add(jGeom);
        //                            holes.Add(jGeom);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        FrameHoleList.Add(frame, holes);
        //        FrameList.Add(frame);
        //        roomFrameDict.Add(Rooms[i], frame);
        //    }
        //}

        public void ClassifyData()
        {
            architectureWallList = ArchitectureWalls.Select(x => x.Boundary).Cast<Polyline>().ToList();
            shearWallList = Shearwalls.Select(x => x.Boundary).Cast<Polyline>().ToList();
            roomList = Rooms.Select(x => x.Boundary).Cast<Polyline>().ToList();
            columnList = Columns.Select(x => x.Boundary).Cast<Polyline>().ToList();

        }

    }
}
