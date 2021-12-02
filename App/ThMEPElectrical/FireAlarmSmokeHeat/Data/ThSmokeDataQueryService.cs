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

namespace ThMEPElectrical.FireAlarmSmokeHeat.Data
{
    public class ThSmokeDataQueryService
    {
        //input
        private List<ThGeometry> Data { get; set; } = new List<ThGeometry>();
        private List<string> CleanBlkName { get; set; } = new List<string>();
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
        public List<ThGeometry> CleanEquipments { get; set; } = new List<ThGeometry>();
        public List<ThGeometry> Equipments { get; set; } = new List<ThGeometry>();


        //output
        //public List<Polyline> wallList { get; private set; } = new List<Polyline>();
        //public List<Polyline> columnList { get; private set; } = new List<Polyline>();
        //public List<Polyline> layoutList { get; private set; } = new List<Polyline>();
        //public List<Polyline> priorityList { get; private set; } = new List<Polyline>();
        public List<Polyline> FrameList { get; private set; } = new List<Polyline>();
        // public List<ThGeometry> FrameGeomList { get; private set; } = new List<ThGeometry>();
        public Dictionary<Polyline, List<Polyline>> FrameHoleList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameWallList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameColumnList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<MPolygon>> FrameLayoutList { get; private set; } = new Dictionary<Polyline, List<MPolygon>>();
        public Dictionary<Polyline, List<Polyline>> FramePriorityList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameDetectAreaList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<ThGeometry, Polyline> RoomFrameDict { get; set; } = new Dictionary<ThGeometry, Polyline>();
        public ThSmokeDataQueryService(List<ThGeometry> data, List<string> cleanBlkName, List<string> avoidBlkNameList)
        {
            Data = data;
            CleanBlkName = cleanBlkName;
            AvoidBlkNameList = avoidBlkNameList;

            PrepareData();
            SetAvoidEquipment();
            CleanData();
        }

        protected void PrepareData()
        {
            Columns = QueryC(BuiltInCategory.Column.ToString());
            Shearwalls = QueryC(BuiltInCategory.ShearWall.ToString());
            ArchitectureWalls = QueryC(BuiltInCategory.ArchitectureWall.ToString());
            Holes = QueryC(BuiltInCategory.Hole.ToString());
            Rooms = QueryC(BuiltInCategory.Room.ToString());
            LayoutArea = QueryC("PlaceCoverage");
            Equipments = QueryC(BuiltInCategory.Equipment.ToString());
            DetectArea = QueryC("DetectionRegion");
        }

        public void SetAvoidEquipment()
        {
            if (AvoidBlkNameList != null)
            {
                AvoidEquipments = Equipments.Where(x => AvoidBlkNameList.Contains(x.Properties["Name"].ToString())).ToList();
            }

        }

        public void CleanData()
        {
            if (CleanBlkName != null)
            {
                CleanEquipments = Equipments.Where(x => CleanBlkName.Contains(x.Properties["Name"].ToString())).ToList();
            }

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
                FramePriorityList[frame] = FramePriorityList[frame].Select(x => x.GetOffsetClosePolyline(priorityExtend)).ToList();
            }
        }
    }
}
