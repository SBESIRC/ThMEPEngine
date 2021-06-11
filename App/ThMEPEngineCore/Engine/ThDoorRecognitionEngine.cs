﻿using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThDoorExtractionEngine : ThBuildingElementExtractionEngine
    {
        public List<string> DoorMarkLayerFilter { get; set; }
        public List<string> DoorStoneLayerFilter { get; set; }
        public ThDoorExtractionEngine()
        {
            DoorMarkLayerFilter = new List<string>();
            DoorStoneLayerFilter = new List<string>();
        }
        public override void Extract(Database database)
        {
            Init(database);
            var doorMarkVisitor = new ThDoorMarkExtractionVisitor()
            {
                LayerFilter = this.DoorMarkLayerFilter,
            };
            var doorStoneVisitor = new ThDoorStoneExtractionVisitor()
            {
                LayerFilter = this.DoorStoneLayerFilter,
            };
            var extractor = new ThBuildingElementExtractor();
            extractor.Accept(doorMarkVisitor);
            extractor.Accept(doorStoneVisitor);
            extractor.Extract(database);
            Results.AddRange(doorMarkVisitor.Results);
            Results.AddRange(doorStoneVisitor.Results);
        }
        private void Init(Database database)
        {
            if (DoorMarkLayerFilter.Count == 0)
            {
                DoorMarkLayerFilter = ThDoorMarkLayerManager.XrefLayers(database);
            }
            if (DoorStoneLayerFilter.Count == 0)
            {
                DoorStoneLayerFilter = ThDoorStoneLayerManager.XrefLayers(database);
            }
        }
    }

    public class ThDoorRecognitionEngine : ThBuildingElementRecognitionEngine
    {
        public override void Recognize(Database database, Point3dCollection polygon)
        {
            // 构件索引服务
            ThSpatialIndexCacheService.Instance.Add(new List<BuiltInCategory>
            {
                BuiltInCategory.ArchitectureWall,
                BuiltInCategory.Column,
                BuiltInCategory.CurtainWall,
                BuiltInCategory.ShearWall,
                BuiltInCategory.Window
            });
            ThSpatialIndexCacheService.Instance.Build(database, polygon);

            // 识别门
            var engine = new ThDoorExtractionEngine();
            engine.Extract(database);
            Recognize(engine.Results, polygon);
        }
        public override void Recognize(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            // 搜集门垛
            var doorStones = GetDoorStones(datas.Where(o => o is ThRawDoorStone).ToList(), polygon);
            // 搜集门标注
            var doorMarks = GetDoorMarks(datas.Where(o => o is ThRawDoorMark).ToList(), polygon);
            // 搜集门标注范围内的门垛
            var buildService = new ThCollectDoorStoneService(
                doorStones.Select(o => o.Geometry).ToCollection(),
                doorMarks.Select(o => o.Data as Entity).ToCollection());
            buildService.Build();

            // 创建门
            var buildDoor = new ThBuildDoorService();
            buildDoor.Build(buildService.Results);
            buildDoor.Outlines.ForEach(o => Elements.Add(new ThIfcDoor { Outline = o }));
        }
        private List<ThRawIfcBuildingElementData> GetDoorStones(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                datas.ForEach(o => dbObjs.Add(o.Geometry));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(polygon);
                return datas.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                return datas;
            }
        }
        private List<ThRawIfcBuildingElementData> GetDoorMarks(List<ThRawIfcBuildingElementData> datas, Point3dCollection polygon)
        {
            if (polygon.Count > 0)
            {
                var dbObjs = new DBObjectCollection();
                datas.ForEach(o => dbObjs.Add(o.Geometry));
                var spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
                var filterObjs = spatialIndex.SelectCrossingPolygon(polygon);
                return datas.Where(o => filterObjs.Contains(o.Geometry)).ToList();
            }
            else
            {
                return datas;
            }
        }
    }
}