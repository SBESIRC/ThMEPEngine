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
using ThMEPEngineCore.Diagnostics;
using ThMEPElectrical.AFAS.Utils;

namespace ThMEPElectrical.FireAlarmArea.Data
{
    public class ThAFASAreaDataQueryService
    {
        //input
        private List<ThGeometry> Data { get; set; } = new List<ThGeometry>();
        //private List<string> CleanBlkName { get; set; } = new List<string>();
        private List<string> AvoidBlkNameList { get; set; } = new List<string>();

        //class use
        public List<ThGeometry> ArchitectureWalls { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Shearwalls { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Columns { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Holes { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> Rooms { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> PlaceArea { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> DetectArea { get; private set; } = new List<ThGeometry>();
        public List<ThGeometry> AvoidEquipments { get; set; } = new List<ThGeometry>();
        //public List<ThGeometry> CleanEquipments { get; set; } = new List<ThGeometry>();
        // public List<ThGeometry> Equipments { get; set; } = new List<ThGeometry>();
        public List<ThGeometry> DoorOpenings { get; set; } = new List<ThGeometry>();
        public List<ThGeometry> Windows { get; set; } = new List<ThGeometry>();


        //output
        public List<Polyline> FrameList { get; private set; } = new List<Polyline>();
        public Dictionary<Polyline, List<Polyline>> FrameHoleList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameWallList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameColumnList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<MPolygon>> FrameLayoutList { get; private set; } = new Dictionary<Polyline, List<MPolygon>>();
        public Dictionary<Polyline, List<Polyline>> FramePriorityList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameDetectAreaList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<ThGeometry, Polyline> RoomFrameDict { get; set; } = new Dictionary<ThGeometry, Polyline>();

        //public ThAFASAreaDataQueryService(List<ThGeometry> data, List<string> cleanBlkName, List<string> avoidBlkNameList)
        //{
        //    Data = data;
        //    //CleanBlkName = cleanBlkName;
        //    AvoidBlkNameList = avoidBlkNameList;

        //    PrepareData();
        //    //CleanPreviousEquipment();
        //}

        public ThAFASAreaDataQueryService(List<ThGeometry> data, List<string> avoidBlkNameList)
        {
            Data = data;
            //CleanBlkName = cleanBlkName;
            AvoidBlkNameList = avoidBlkNameList;

            PrepareData();
            //CleanPreviousEquipment();
        }

        private void PrepareData()
        {
            Columns = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Column.ToString());
            Shearwalls = ThAFASUtils.QueryCategory(Data, BuiltInCategory.ShearWall.ToString());
            ArchitectureWalls = ThAFASUtils.QueryCategory(Data, BuiltInCategory.ArchitectureWall.ToString());
            Holes = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Hole.ToString());
            Rooms = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Room.ToString());
            PlaceArea = ThAFASUtils.QueryCategory(Data, "PlaceCoverage");
            DetectArea = ThAFASUtils.QueryCategory(Data, "DetectionRegion");
            DoorOpenings = ThAFASUtils.QueryCategory(Data, BuiltInCategory.DoorOpening.ToString());
            Windows = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Window.ToString());
            var allEquipments = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Equipment.ToString());
            AvoidEquipments = allEquipments.Where(x => AvoidBlkNameList.Contains(x.Properties["Name"].ToString())).ToList();
            //CleanEquipments = allEquipments.Where(x => CleanBlkName.Contains(x.Properties["Name"].ToString())).ToList();
        }

        //public void CleanPreviousEquipment()
        //{
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

        public void AnalysisHoles()
        {
            var holesTemp = Holes.Select(x => x.Boundary as Polyline).ToList();
            var allHolesList = new List<Polyline>();

            for (int i = 0; i < Rooms.Count; i++)
            {
                Polyline frame = null;
                List<Polyline> holes = new List<Polyline>();
                if (Rooms[i].Boundary is MPolygon mPolygon)
                {
                    frame = mPolygon.Shell();
                    holes.AddRange(mPolygon.Holes());
                }
                else if (Rooms[i].Boundary is Polyline polyline)
                {
                    frame = polyline;
                }


                for (int j = 0; j < holesTemp.Count; j++)
                {
                    var jGeom = holesTemp[j];
                    jGeom.Closed = true;

                    if (allHolesList.Contains(jGeom) == false)
                    {
                        ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(frame, jGeom);
                        if (relation.IsContains)
                        {
                            allHolesList.Add(jGeom);
                            holes.Add(jGeom);
                        }
                        else if (relation.IsIntersects && relation.IsOverlaps)
                        {
                            var polyCollection = new DBObjectCollection() { frame };
                            var overlap = jGeom.Intersection(polyCollection);

                            if (overlap.Count > 0)
                            {
                                var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();

                                if (overlapPoly.Area / jGeom.Area > 0.2)
                                {
                                    allHolesList.Add(jGeom);
                                    holes.Add(jGeom);
                                }
                            }
                        }
                    }
                }
                FrameHoleList.Add(frame, holes);
                FrameList.Add(frame);
                RoomFrameDict.Add(Rooms[i], frame);
            }
        }

        public void ClassifyData()
        {
            var wallList = new List<ThGeometry>();
            wallList.AddRange(ArchitectureWalls);
            wallList.AddRange(Shearwalls);

            FrameWallList = ClassifyData(wallList);
            FrameColumnList = ClassifyData(Columns);
            FrameLayoutList = ClassifyLayoutArea(PlaceArea);
            FramePriorityList = ClassifyData(AvoidEquipments);
            FrameDetectAreaList = ClassifyData(DetectArea);
        }


        public void AddMRoomDict()
        {
            for (int i = 0; i < Rooms.Count; i++)
            {
                Polyline frame = null;
                List<Polyline> holes = new List<Polyline>();
                if (Rooms[i].Boundary is MPolygon mPolygon)
                {
                    frame = mPolygon.Shell();
                    holes.AddRange(mPolygon.Holes());
                }
                else if (Rooms[i].Boundary is Polyline polyline)
                {
                    frame = polyline;
                }

                FrameHoleList.Add(frame, holes);
                FrameList.Add(frame);
                RoomFrameDict.Add(Rooms[i], frame);
            }
        }

        public void ClassifyDataNew()
        {
            var geomDict = new Dictionary<Entity, ThGeometry>();
            var objs = new DBObjectCollection();
            ArchitectureWalls.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });
            Shearwalls.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });
            Columns.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });
            Holes.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });
            PlaceArea.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });
            AvoidEquipments.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });
            DetectArea.ForEach(x => { geomDict[x.Boundary] = x; objs.Add(x.Boundary); });

            var spetialIdx = new ThCADCoreNTSSpatialIndex(objs);
            for (int i = 0; i < Rooms.Count; i++)
            {
                var room = Rooms[i];

                FrameWallList[RoomFrameDict[room]] = new List<Polyline> { };
                FrameColumnList[RoomFrameDict[room]] = new List<Polyline> { };
                FrameLayoutList[RoomFrameDict[room]] = new List<MPolygon> { };
                FrameDetectAreaList[RoomFrameDict[room]] = new List<Polyline> { };
                FramePriorityList[RoomFrameDict[room]] = new List<Polyline> { };

                var filterobjs = spetialIdx.SelectCrossingPolygon(Rooms[i].Boundary);
                for (int j = 0; j < filterobjs.Count; j++)
                {
                    var obj = filterobjs[j];
                    geomDict.TryGetValue(obj as Entity, out var geom);
                    if (geom != null)
                    {
                        Polyline geomPl = null;
                        MPolygon geomMpl = null;

                        if (geom.Boundary is Polyline pl)
                        {
                            geomMpl = ThMPolygonTool.CreateMPolygon(pl);
                            geomPl = pl;
                        }
                        else if (geom.Boundary is MPolygon mpl)
                        {
                            geomMpl = mpl;
                            geomPl = mpl.Shell();
                        }
                        var catogary = geom.Properties[ThExtractorPropertyNameManager.CategoryPropertyName].ToString();

                        if (catogary == BuiltInCategory.ArchitectureWall.ToString() ||
                            catogary == BuiltInCategory.ShearWall.ToString())
                        {
                            FrameWallList[RoomFrameDict[room]].Add(geomPl);
                        }
                        else if (catogary == BuiltInCategory.Column.ToString())
                        {
                            FrameColumnList[RoomFrameDict[room]].Add(geomPl);
                        }
                        else if (catogary == BuiltInCategory.Hole.ToString())
                        {
                            FrameHoleList[RoomFrameDict[room]].Add(geomPl);
                        }
                        else if (catogary == "PlaceCoverage")
                        {
                            FrameLayoutList[RoomFrameDict[room]].Add(geomMpl);
                        }
                        else if (catogary == "DetectionRegion")
                        {
                            FrameDetectAreaList[RoomFrameDict[room]].Add(geomPl);
                        }
                        else if (catogary == BuiltInCategory.Equipment.ToString())
                        {
                            FramePriorityList[RoomFrameDict[room]].Add(geomPl);
                        }
                    }
                }
            }
        }

        private Dictionary<Polyline, List<Polyline>> ClassifyData(List<ThGeometry> polyList)
        {
            var polyDict = new Dictionary<Polyline, List<Polyline>>();

            for (int i = 0; i < FrameList.Count; i++)
            {
                var plInFrame = new List<Polyline>();
                for (int j = 0; j < polyList.Count; j++)
                {
                    var pl = new Polyline();
                    if (polyList[j].Boundary is Polyline)
                    {
                        pl = polyList[j].Boundary as Polyline;
                    }
                    else if (polyList[j].Boundary is MPolygon mpl)
                    {
                        pl = mpl.Shell();
                    }
                    pl.Closed = true;
                    ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(FrameList[i], pl);

                    if (relation.IsContains || relation.IsIntersects)
                    {
                        plInFrame.Add(pl);
                    }
                }
                polyDict.Add(FrameList[i], plInFrame);
            }

            return polyDict;
        }

        private Dictionary<Polyline, List<MPolygon>> ClassifyLayoutArea(List<ThGeometry> polyList)
        {
            var polyDict = new Dictionary<Polyline, List<MPolygon>>();

            for (int i = 0; i < FrameList.Count; i++)
            {
                var plInFrame = new List<MPolygon>();
                for (int j = 0; j < polyList.Count; j++)
                {
                    var pl = new Polyline();
                    if (polyList[j].Boundary is Polyline)
                    {
                        pl = polyList[j].Boundary as Polyline;
                    }
                    else if (polyList[j].Boundary is MPolygon mpl)
                    {
                        pl = mpl.Shell();
                    }
                    ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(FrameList[i], pl);

                    if (relation.IsContains || relation.IsIntersects)
                    {
                        if (polyList[j].Boundary is Polyline plFrame)
                        {
                            plInFrame.Add(ThMPolygonTool.CreateMPolygon(plFrame));
                        }
                        else if (polyList[j].Boundary is MPolygon mpl)
                        {
                            plInFrame.Add(mpl);
                        }
                    }
                }
                polyDict.Add(FrameList[i], plInFrame);
            }

            return polyDict;
        }

        public void ExtendPriority(double priorityExtend)
        {
            foreach (var frame in FrameList)
            {
                FramePriorityList[frame] = ThAFASUtils.ExtendPriority(FramePriorityList[frame], priorityExtend);
            }
        }

        public void Print()
        {
            ArchitectureWalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0archWall", 1));
            Shearwalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0shearWall", 3));
            Columns.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Column", 1));
            Windows.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Window", 4));
            Rooms.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0room", 30));
            var beam = ThAFASUtils.QueryCategory(Data, BuiltInCategory.Beam.ToString());
            beam.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0beam", 190));
            DoorOpenings.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DoorOpening", 4));
            Holes.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Hole", 150));
            PlaceArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0PlaceCoverage", 6));
            DetectArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DetectArea", 91));
            AvoidEquipments.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Equipment", 152));
        }
    }
}
