using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using GeometryExtensions;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Config;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPElectrical.Service;

using ThMEPElectrical.FireAlarm.Service;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Data
{
    public class ThSmokeDataQueryService
    {
        //input
        private List<ThGeometry> Data { get; set; } = new List<ThGeometry>();
        private List<string> CleanBlkName { get; set; } = new List<string>();
        private List<string> AvoidBlkNameList { get; set; } = new List<string>();

        //class use
        private List<ThGeometry> ArchitectureWalls { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> Shearwalls { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> Columns { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> Holes { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> Rooms { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> AvoidEquipments { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> LayoutArea { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> CleanEquipments { get; set; } = new List<ThGeometry>();
        private List<ThGeometry> Equipments { get; set; } = new List<ThGeometry>();

        //output
        //public List<Polyline> wallList { get; private set; } = new List<Polyline>();
        //public List<Polyline> columnList { get; private set; } = new List<Polyline>();
        //public List<Polyline> layoutList { get; private set; } = new List<Polyline>();
        //public List<Polyline> priorityList { get; private set; } = new List<Polyline>();
        public List<Polyline> frameList { get; private set; } = new List<Polyline>();
        public Dictionary<Polyline, List<Polyline>> frameHoleList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> frameWallList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> frameColumnList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> frameLayoutList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> framePriorityList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();
        public Dictionary<Polyline, List<Polyline>> FrameDetectAreaList { get; private set; } = new Dictionary<Polyline, List<Polyline>>();

        public ThSmokeDataQueryService(List<ThGeometry> data, List<string> cleanBlkName, List<string> avoidBlkNameList)
        {
            Data = data;
            CleanBlkName = cleanBlkName;
            AvoidBlkNameList = avoidBlkNameList;

            PrepareData();
            setAvoidEquipment();
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
            Equipments = QueryC(BuiltInCategory.Distribution.ToString());

        }

        public void setAvoidEquipment()
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


        public void analysisHoles()
        {
            var frameListTemp = new List<Polyline>();
            var holesTemp = Holes.Select(x => x.Boundary as Polyline).ToList();
            frameListTemp.AddRange(Holes.Select(x => x.Boundary as Polyline));
            frameListTemp.AddRange(Rooms.Select(x => x.Boundary as Polyline));
            frameListTemp = frameListTemp.OrderByDescending(x => x.Area).ToList();

            var holeList = new List<Polyline>();

            for (int i = 0; i < frameListTemp.Count; i++)
            {
                if (holeList.Contains(frameListTemp[i]) == false && holesTemp.Contains(frameListTemp[i]) == false)
                {
                    var holes = new List<Polyline>();
                    frameListTemp[i].Closed = true;
                    for (int j = i + 1; j < frameListTemp.Count; j++)
                    {
                        frameListTemp[j].Closed = true;
                        ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(frameListTemp[i], frameListTemp[j]);

                        if (relation.IsContains)
                        {
                            holeList.Add(frameListTemp[j]);
                            holes.Add(frameListTemp[j]);
                        }
                        else if (relation.IsIntersects && relation.IsOverlaps)
                        {
                            var polyCollection = new DBObjectCollection() { frameListTemp[i] };
                            var overlap = frameListTemp[j].Intersection(polyCollection);

                            if (overlap.Count > 0)
                            {
                                var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();

                                if (overlapPoly.Area / frameListTemp[j].Area > 0.6)
                                {
                                    holeList.Add(frameListTemp[j]);
                                    holes.Add(frameListTemp[j]);
                                }
                            }
                        }
                    }
                    frameHoleList.Add(frameListTemp[i], holes);
                    frameList.Add(frameListTemp[i]);
                }
            }
        }

        public void ClassifyData()
        {
            var wallList = new List<Polyline>();
            wallList.AddRange(ArchitectureWalls.Select(x => x.Boundary as Polyline));
            wallList.AddRange(Shearwalls.Select(x => x.Boundary as Polyline));
            var columnList = new List<Polyline>();
            columnList.AddRange(Columns.Select(x => x.Boundary as Polyline));
            var layoutList = new List<Polyline>();
            layoutList.AddRange(LayoutArea.Select(x => x.Boundary as Polyline));
            var priorityList = new List<Polyline>();
            priorityList.AddRange(AvoidEquipments.Select(x => x.Boundary as Polyline));

            frameWallList = classifyData(wallList);
            frameColumnList = classifyData(columnList);
            frameLayoutList = classifyData(layoutList);
            framePriorityList = classifyData(priorityList);
            FrameDetectAreaList = classifyData(new List<Polyline>());
        }
        private Dictionary<Polyline, List<Polyline>> classifyData(List<Polyline> polyList)
        {
            var polyDict = new Dictionary<Polyline, List<Polyline>>();

            for (int i = 0; i < frameList.Count; i++)
            {
                var plInFrame = new List<Polyline>();
                for (int j = 0; j < polyList.Count; j++)
                {
                    polyList[j].Closed = true;
                    ThCADCoreNTSRelate relation = new ThCADCoreNTSRelate(frameList[i], polyList[j]);

                    if (relation.IsContains || relation.IsIntersects)
                    {
                        plInFrame.Add(polyList[j]);
                    }
                }
                polyDict.Add(frameList[i], plInFrame);
            }

            return polyDict;
        }
    }
}
