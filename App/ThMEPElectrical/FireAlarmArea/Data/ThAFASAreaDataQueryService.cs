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
        public List<ThGeometry> LayoutArea { get; private set; } = new List<ThGeometry>();
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
            LayoutArea = ThAFASUtils.QueryCategory(Data, "PlaceCoverage");
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
            FrameLayoutList = ClassifyLayoutArea(LayoutArea);
            FramePriorityList = ClassifyData(AvoidEquipments);
            FrameDetectAreaList = ClassifyData(DetectArea);
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
            ArchitectureWalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0archWall", 3));
            Shearwalls.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0shearWall", 0));
            Columns.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Column", 1));
            Rooms.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0room", 2));
            DoorOpenings.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DoorOpening", 4));
            Holes.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Hole", 5));
            LayoutArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0PlaceCoverage", 200));
            DetectArea.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0DetectArea", 96));
            AvoidEquipments.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Equipment", 152));
            Windows.ForEach(x => DrawUtils.ShowGeometry(x.Boundary, "l0Window", 4));
        }
    }
}
