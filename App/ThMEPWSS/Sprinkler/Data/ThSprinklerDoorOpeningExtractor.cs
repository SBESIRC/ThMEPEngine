﻿using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using ThMEPEngineCore.GeojsonExtractor;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;

namespace ThMEPWSS.Sprinkler.Data
{
    public class ThSprinklerDoorOpeningExtractor : ThExtractorBase, IPrint, ITransformer
    {
        private List<ThIfcRoom> Rooms { get; set; }
        private List<Polyline> Holes { get; set; }
        public List<ThIfcDoor> Doors { get; private set; }
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        private Dictionary<Entity, List<string>> FireDoorNeibourIds { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThBuildingElementVisitorManager VisitorManager { get; set; }

        public ThSprinklerDoorOpeningExtractor()
        {
            Rooms = new List<ThIfcRoom>();
            Doors = new List<ThIfcDoor>();
            Holes = new List<Polyline>();
            StoreyInfos = new List<ThStoreyInfo>();
            Category = BuiltInCategory.DoorOpening.ToString();
            FireDoorNeibourIds = new Dictionary<Entity, List<string>>();
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            Doors = ExtractDb3Door(database, pts);
            //var localDoors = ExtractMsDoor(database, pts);
            //对Clean的结果进一步过虑
            //for (int i = 0; i < localDoors.Count; i++)
            //{
            //    localDoors[i].Outline = ThSprinklerCleanEntityService.Buffer(localDoors[i].Outline as Polyline, 25);
            //}

            //处理重叠
            //var conflictService = new ThSprinklerHandleConflictService();
            //Doors = conflictService.Union(db3Doors, localDoors);

            var objs = Doors.Select(o => o.Outline).ToCollection().FilterSmallArea(SmallAreaTolerance);
            Doors = Doors.Where(o => objs.Contains(o.Outline)).ToList();
            var bufferService = new ThNTSBufferService();
            for (int i = 0; i < Doors.Count; i++)
            {
                Doors[i].Outline = bufferService.Buffer(Doors[i].Outline, 15);
            }
        }
        #region ----------提取DB3门-----------
        private DBObjectCollection ExtractDb3Column(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var columnEngine = new ThDB3ColumnRecognitionEngine();
            columnEngine.Recognize(VisitorManager.DB3ColumnVisitor.Results, newPts);
            return columnEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractColumn(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var columnEngine = new ThColumnRecognitionEngine();
            columnEngine.Recognize(VisitorManager.ColumnVisitor.Results, newPts);
            return columnEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractDb3ArchitectureWall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var wallEngine = new ThDB3ArchWallRecognitionEngine();
            wallEngine.Recognize(VisitorManager.DB3ArchWallVisitor.Results, newPts);
            return wallEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractDb3Curtainwall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var curtainWallEngine = new ThDB3CurtainWallRecognitionEngine();
            curtainWallEngine.Recognize(VisitorManager.DB3CurtainWallVisitor.Results, newPts);
            return curtainWallEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractDb3Shearwall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var shearWallEngine = new ThDB3ShearWallRecognitionEngine();
            shearWallEngine.Recognize(VisitorManager.DB3ShearWallVisitor.Results, newPts);
            return shearWallEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractShearwall(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var shearWallEngine = new ThShearWallRecognitionEngine();
            shearWallEngine.Recognize(VisitorManager.ShearWallVisitor.Results, newPts);
            return shearWallEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        private DBObjectCollection ExtractDb3Window(Point3dCollection pts)
        {
            //提取了DB3中的墙，并移动到原点
            var newPts = Transformer.Transform(pts);
            var windowEngine = new ThDB3WindowRecognitionEngine();
            windowEngine.Recognize(VisitorManager.DB3WindowVisitor.Results, newPts);
            return windowEngine.Elements.Select(o => o.Outline).ToCollection();
        }

        /// <summary>
        /// 提取门依赖的数据，是在图纸原位的
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private Dictionary<BuiltInCategory, DBObjectCollection> DoorDependElements(Point3dCollection pts)
        {
            //都在原点
            var dict = new Dictionary<BuiltInCategory, DBObjectCollection>();
            var columns = ExtractColumn(pts);
            var shearWalls = ExtractShearwall(pts);
            var db3Windows = ExtractDb3Window(pts);
            var db3Columns = ExtractDb3Column(pts);
            var db3ShearWalls = ExtractDb3Shearwall(pts);
            var db3CurtainWalls = ExtractDb3Curtainwall(pts);
            var db3ArchitectureWalls = ExtractDb3ArchitectureWall(pts);

            dict.Add(BuiltInCategory.Column, db3Columns.Union(columns));
            dict.Add(BuiltInCategory.ArchitectureWall, db3ArchitectureWalls);
            dict.Add(BuiltInCategory.CurtainWall, db3CurtainWalls);
            dict.Add(BuiltInCategory.ShearWall, db3ShearWalls.Union(shearWalls));
            dict.Add(BuiltInCategory.Window, db3Windows);
            return dict;
        }

        private List<ThIfcDoor> ExtractDb3Door(Database database, Point3dCollection pts)
        {
            // 构件索引服务
            //ThSpatialIndexCacheService.Instance.Add(new List<BuiltInCategory>
            //{
            //    BuiltInCategory.ArchitectureWall,
            //    BuiltInCategory.Column,
            //    BuiltInCategory.CurtainWall,
            //    BuiltInCategory.ShearWall,
            //    BuiltInCategory.Window
            //});            
            //ThSpatialIndexCacheService.Instance.Build(database, pts);
            var dict = DoorDependElements(pts);
            ThSpatialIndexCacheService.Instance.Transformer = new ThMEPOriginTransformer()
            {
                Displacement = Matrix3d.Identity,
            };
            ThSpatialIndexCacheService.Instance.Build(dict);

            var doorDatas = new List<ThRawIfcBuildingElementData>();
            doorDatas.AddRange(VisitorManager.DB3DoorMarkVisitor.Results);
            doorDatas.AddRange(VisitorManager.DB3DoorStoneVisitor.Results);

            var doorEngine = new ThDB3DoorRecognitionEngine();
            var newPts = Transformer.Transform(pts);
            doorEngine.Recognize(doorDatas, newPts);
            return doorEngine.Elements.Cast<ThIfcDoor>().ToList();
        }

        #endregion

        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            var exteriorDoorService = new ThSprinklerIsExteriorDoorService(this.Rooms.Select(o => o.Boundary).ToList(), this.Holes);

            Doors.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, Category);
                var parentId = BuildString(GroupOwner, o.Outline);
                if (string.IsNullOrEmpty(parentId))
                {
                    var storeyInfo = Query(o.Outline);
                    parentId = storeyInfo.Id;
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, parentId);
                if (FireDoorNeibourIds.ContainsKey(o.Outline))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.NeibourFireApartIdsPropertyName, string.Join(",", FireDoorNeibourIds[o.Outline]));
                }
                geometry.Properties.Add(ThExtractorPropertyNameManager.UseagePropertyName, IsFireDoor(o) ? "防火门" : "");
                if (exteriorDoorService.IsExteriorDoor(o.Outline as Polyline))
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.TagPropertyName, "外门");
                }
                else
                {
                    geometry.Properties.Add(ThExtractorPropertyNameManager.TagPropertyName, "");
                }
                geometry.Boundary = o.Outline;
                geos.Add(geometry);
            });
            return geos;
        }

        private bool IsFireDoor(ThIfcDoor door)
        {
            //ToDO
            return door.Spec.ToUpper().Contains("FM");
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity)); ;
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var doorIds = new ObjectIdList();
                Doors.ForEach(o =>
                {
                    o.Outline.ColorIndex = ColorIndex;
                    o.Outline.SetDatabaseDefaults();
                    doorIds.Add(db.ModelSpace.Add(o.Outline));
                });
                if (doorIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), doorIds);
                }
            }
        }

        public override List<Entity> GetEntities()
        {
            return Doors.Select(o => o.Outline).ToList();
        }

        public override void SetRooms(List<ThIfcRoom> rooms)
        {
            this.Rooms = rooms;
        }
        
        public void Transform()
        {
            Doors.ForEach(o => Transformer.Transform(o.Outline));
        }

        public void Reset()
        {
            Doors.ForEach(o => Transformer.Reset(o.Outline));
        }
    }
}
