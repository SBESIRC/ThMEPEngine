using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPTCH.CAD;
using ThMEPTCH.Model;
using ThMEPTCH.PropertyServices.PropertyEnums;
using ThMEPTCH.PropertyServices.PropertyModels;
using ThMEPTCH.TCHArchDataConvert;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;
using ThMEPTCH.TCHArchDataConvert.THArchEntity;
using ThMEPTCH.TCHArchDataConvert.THStructureEntity;

namespace ThMEPTCH.Services
{
    public class ThDWGToIFCService
    {
        private TCHArchDBData archDBData;
        ThCADCoreNTSSpatialIndex spatialIndex;
        ThCADCoreNTSSpatialIndex entitySpatialIndex;
        ThCADCoreNTSSpatialIndex dbEntitySpatialIndex;//new
        List<THArchEntityBase> entityBases = new List<THArchEntityBase>();
        Dictionary<MPolygon, THArchEntityBase> entityDic = new Dictionary<MPolygon, THArchEntityBase>();
        Dictionary<Polyline, THStructureEntity> dbEntityDic = new Dictionary<Polyline, THStructureEntity>();//new
        List<FloorCurveEntity> cadCurveEntities = new List<FloorCurveEntity>();
        Dictionary<Entity, FloorCurveEntity> cadEntityDic = new Dictionary<Entity, FloorCurveEntity>();
        double ralingHeight = 1200;
        double slabThickness = 100;
        public ThDWGToIFCService(string dbPath)
        {
            if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                archDBData = new TCHArchDBData(dbPath);
        }

        public List<TArchEntity> GetArchEntities()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var engine = new ThTCHBuildingElementExtractionEngine();
                engine.Extract(acdb.Database);
                engine.ExtractFromMS(acdb.Database);
                return engine.Results.Select(o => o.Data as TArchEntity).ToList();
            }
        }

        public List<THStructureEntity> GetDBStructureEntities()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var building = new ThDBStructureElementBuilding();
                var structureElements = building.BuildingFromMS(acdb.Database);

                return structureElements;
                //return engine.Elements.Results.Select(o => o.Data as THStructureEntity).ToList();
            }
        }

        private Dictionary<string, List<ThEditStoreyInfo>> GetStoreyJsonFile(string dwgFullName)
        {
            var path = Path.GetDirectoryName(dwgFullName);
            var fileName = Path.GetFileNameWithoutExtension(dwgFullName);
            var jsonPath = Path.Combine(path, fileName + ".StoreyInfo.json");
            return ThIfcStoreyParseTool.DeSerialize(jsonPath);
        }

        public ThTCHProject DWGToProject(bool isMemoryStory, bool railingToRegion, bool isSelectFloor = false, bool IsStructure = false)
        {
            string prjId = "";
            string prjName = "测试项目";
            var jsonConfig = new Dictionary<string, List<ThEditStoreyInfo>>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                prjName = Active.DocumentName;
                prjId = Active.Document.UnmanagedObject.ToString();
                jsonConfig = GetStoreyJsonFile(Active.Document.Name);
            }
            if (jsonConfig.Count == 0)
            {
                Active.Database.GetEditor().WriteMessage("未找到项目文件对应的楼层配置，请检查。");
                return null;
            }
            var thPrj = new ThTCHProject();
            thPrj.Uuid = prjId;
            thPrj.ProjectName = prjName;
            var thSite = new ThTCHSite();
            thSite.Uuid = prjId + "site";
            var thBuilding = new ThTCHBuilding();
            thBuilding.Uuid = prjId + "Building";
            var floorOrigin = GetFloorBlockPolylines(isSelectFloor);
            if (floorOrigin.Count < 1)
            {
                return null;
            }
            LoadCustomElements();
            var allEntities = null != archDBData ? archDBData.AllTArchEntitys() : GetArchEntities();
            var allDBEntities = null != archDBData ? new List<THStructureEntity>() : GetDBStructureEntities();
            InitFloorDBEntity(allEntities, allDBEntities);
            var entityConvert = new TCHDBEntityConvert(prjId);
            foreach (var floor in floorOrigin)
            {
                var floorEntitys = FloorEntitys(floor.FloorOutLine, out List<FloorCurveEntity> curveEntities);
                var moveVector = Point3d.Origin - floor.FloorOrigin;
                Matrix3d matrix = Matrix3d.Displacement(moveVector);
                var thisFloorWalls = floorEntitys.OfType<WallEntity>().Select(c => c.DBArchEntity).Cast<TArchWall>().ToList();
                var thisFloorDoors = floorEntitys.OfType<DoorEntity>().Select(c => c.DBArchEntity).Cast<TArchDoor>().ToList();
                var thisFloorWindows = floorEntitys.OfType<WindowEntity>().Select(c => c.DBArchEntity).Cast<TArchWindow>().ToList();
                var walls = entityConvert.WallDoorWindowRelation(thisFloorWalls, thisFloorDoors, thisFloorWindows, moveVector);

                //structure
                var floorDbEntitys = FloorDBEntitys(floor.FloorOutLine);
                var structureWalls = floorDbEntitys.OfType<THStructureWall>();
                walls.AddRange(structureWalls.Select(o => WallEntityToTCHWall(thPrj.Uuid, o, matrix)));
                var columns = new List<ThTCHColumn>();
                var structureColumnls = floorDbEntitys.OfType<THStructureColumn>();
                columns.AddRange(structureColumnls.Select(o => ColumnEntityToTCHColumn(thPrj.Uuid, o, matrix)));
                var beams = new List<ThTCHBeam>();
                var structureBeams = floorDbEntitys.OfType<THStructureBeam>();
                beams.AddRange(structureBeams.Select(o => BeamEntityToTCHBeam(thPrj.Uuid, o, matrix)));
                var slabs = new List<ThTCHSlab>();
                var structureSlabs = floorDbEntitys.OfType<THStructureSlab>();
                slabs.AddRange(structureSlabs.Select(o => BeamEntityToTCHSlab(thPrj.Uuid, o, matrix)));
                floor.FloorEntitys.AddRange(columns);
                floor.FloorEntitys.AddRange(beams);
                floor.FloorEntitys.AddRange(slabs);
                floor.FloorEntitys.AddRange(walls);

                var allSlabs = new List<ThTCHSlab>();
                var thisRailingEntities = new Dictionary<Polyline, ThTCHRailing>();
                var railingColls = new DBObjectCollection();
                var thisOpeningEntities = new Dictionary<Polyline, ThTCHOpening>();
                var openingColls = new DBObjectCollection();
                foreach (var item in curveEntities)
                {
                    if (item.EntitySystem.Contains("楼板"))
                    {
                        if (item.FloorEntity != null && item.FloorEntity is SlabPolyline slab1)
                        {
                            var struAndArchSlab = CreateStruAndArchSlab(slab1, matrix);
                            var n = 1;
                            struAndArchSlab.ForEach(slab =>
                            {
                                if (item.Property is SlabProperty slabProp)
                                {
                                    slab.ZOffSet += slabProp.TopElevation;
                                    slab.EnumMaterial = slabProp.EnumMaterial.GetDescription();
                                }
                                slab.Uuid = prjId + item.Id + n;
                                n++;
                                allSlabs.Add(slab);
                            });
                        }
                    }
                    else if (item.EntitySystem.Contains("栏杆"))
                    {
                        if (item.EntityCurve is Polyline polyline)
                        {
                            var pLine = polyline.GetTransformedCopy(matrix) as Polyline;
                            railingColls.Add(pLine);
                            var railing = CreateRailing(pLine);
                            var prop = item.Property as RailingProperty;
                            if (railingToRegion)
                            {
                                var centerline = railing.Outline as Polyline;
                                var outlines = centerline.BufferFlatPL(railing.Width / 2.0);
                                railing.Outline = outlines[0] as Polyline;
                            }
                            railing.Height = prop.Height;
                            railing.Width = prop.Thickness;
                            railing.ZOffSet = prop.BottomElevation;
                            railing.Uuid = prjId + item.Id;
                            thisRailingEntities.Add(pLine, railing);
                        }
                    }
                    else if (item.EntitySystem.Contains("墙洞"))
                    {
                        if (item.EntityCurve is Polyline polyline)
                        {
                            var pLine = polyline.GetTransformedCopy(matrix) as Polyline;
                            openingColls.Add(pLine);
                            var opening = CreateOpening(pLine);
                            var prop = item.Property as HoleProperty;
                            opening.Height = prop.Height;
                            opening.ShowDimension = prop.ShowDimension;
                            opening.Hidden = prop.Hidden;
                            opening.BottomElevation = prop.BottomElevation;
                            opening.NumberPrefix = prop.NumberPrefix;
                            opening.NumberPostfix = prop.NumberPostfix;
                            opening.ElevationDisplay = prop.ElevationDisplay;
                            opening.Uuid = prjId + item.Id;
                            thisOpeningEntities.Add(pLine, opening);
                        }
                    }
                }
                //用墙的索引找栏杆没有找到
                var railingSpatialIndex = new ThCADCoreNTSSpatialIndex(railingColls);
                var openingSpatialIndex = new ThCADCoreNTSSpatialIndex(openingColls);
                var hisPLines = new List<Polyline>();
                foreach (var wall in walls)
                {
                    if (wall.Height < 10 || wall.Height > 2000)
                        continue;
                    if (hisPLines.Count == thisRailingEntities.Count)
                        break;

                    // 栏杆
                    var crossRailings = railingSpatialIndex.SelectCrossingPolygon(wall.Outline).OfType<Polyline>().ToList();
                    foreach (var polyline in crossRailings)
                    {
                        if (hisPLines.Any(c => c == polyline))
                            continue;
                        hisPLines.Add(polyline);
                        var railing = thisRailingEntities[polyline];
                        (railing.Outline as Polyline).Elevation = (wall.Outline as Polyline).Elevation + wall.Height;
                        railing.ZOffSet = wall.Height;
                        railing.Height = 800;
                    }
                }

                foreach (var wall in walls)
                {
                    // 墙洞
                    var crossOpenings = openingSpatialIndex.SelectCrossingPolygon(wall.Outline).OfType<Polyline>().ToList();
                    foreach (var polyline in crossOpenings)
                    {
                        if (!(wall.Outline as Polyline).Contains(polyline.GetCenter()))
                        {
                            continue;
                        }

                        var opening = thisOpeningEntities[polyline];
                        wall.Openings.Add(opening);
                        if (!polyline.Closed)
                        {
                            // 更新外轮廓
                            var newOutline = (opening.Outline as Polyline).Buffer(wall.Width / 2 + 100.0).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                            if (!newOutline.IsNull())
                            {
                                opening.Outline = newOutline;
                            }
                        }
                    }
                }

                floor.FloorEntitys.AddRange(allSlabs);
                floor.FloorEntitys.AddRange(thisRailingEntities.Select(c => c.Value).ToList());
            }

            var floorData = GetBlockElevtionValue(floorOrigin, jsonConfig);
            //var floorData = GetBlockElevtionValue(floorOrigin);
            if (floorData.Count < 1)
            {
                Active.Database.GetEditor().WriteMessage("未能找到相对应的楼层数据信息，请检查。");
                return null;
            }
            var PreviousHeight = floorData.FirstOrDefault().Elevation;
            foreach (var floor in floorData)
            {
                var levelEntitys = floorOrigin.Find(c => c.FloorName == floor.FloorName);
                if (levelEntitys == null)
                    continue;
                bool isTopFloor = Math.Abs(PreviousHeight - floor.Elevation) > 500;
                var buildingStorey = new ThTCHBuildingStorey();
                buildingStorey.Uuid = prjId + floor.Num.ToString() + "F";
                buildingStorey.Number = floor.Num.ToString();
                buildingStorey.Height = floor.LevelHeight;
                buildingStorey.Elevation = floor.Elevation;
                buildingStorey.Usage = floor.FloorName;
                buildingStorey.Origin = new Point3d(0, 0, floor.Elevation);
                buildingStorey.Properties.Add("FloorNo", floor.Num.ToString());
                buildingStorey.Properties.Add("Height", floor.LevelHeight.ToString());
                buildingStorey.Properties.Add("StdFlrNo", floor.Num.ToString());
                PreviousHeight = floor.Elevation + floor.LevelHeight;
                ThTCHBuildingStorey memoryStory = null;
                if (isMemoryStory)
                {
                    foreach (var item in thBuilding.Storeys)
                    {
                        if (!string.IsNullOrEmpty(item.MemoryStoreyId) || item.Usage != floor.FloorName)
                            continue;
                        if (Math.Abs(item.Height - floor.LevelHeight) < 1)
                        {
                            memoryStory = item;
                        }
                    }
                }
                if (null != memoryStory)
                {
                    buildingStorey.MemoryStoreyId = memoryStory.Uuid;
                    buildingStorey.MemoryMatrix3d = Matrix3d.Displacement(buildingStorey.Origin - memoryStory.Origin);
                }
                else
                {
                    var walls = new List<ThTCHWall>();
                    foreach (var item in levelEntitys.FloorEntitys.OfType<ThTCHWall>().ToList())
                    {
                        var copyItem = item.Clone() as ThTCHWall;
                        copyItem.Uuid += buildingStorey.Number;
                        if (Math.Abs(copyItem.Height) < 10)
                            copyItem.Height = floor.LevelHeight;
                        foreach (var door in copyItem.Doors)
                        {
                            door.Uuid += buildingStorey.Number;
                        }
                        foreach (var window in copyItem.Windows)
                        {
                            window.Uuid += buildingStorey.Number;
                        }
                        foreach (var opening in copyItem.Openings)
                        {
                            opening.Uuid += buildingStorey.Number;
                        }
                        walls.Add(copyItem);
                    }
                    var columns = new List<ThTCHColumn>();
                    foreach (var item in levelEntitys.FloorEntitys.OfType<ThTCHColumn>().ToList())
                    {
                        var copyItem = item.Clone() as ThTCHColumn;
                        copyItem.Uuid += buildingStorey.Number;
                        if (Math.Abs(copyItem.Height) < 10)
                            copyItem.Height = floor.LevelHeight;
                        columns.Add(copyItem);
                    }
                    var beams = new List<ThTCHBeam>();
                    foreach (var item in levelEntitys.FloorEntitys.OfType<ThTCHBeam>().ToList())
                    {
                        var copyItem = item.Clone() as ThTCHBeam;
                        copyItem.Uuid += buildingStorey.Number;
                        if (Math.Abs(copyItem.Height) > 10)
                        {
                            copyItem.ZOffSet = floor.LevelHeight + item.ZOffSet - item.Height;
                            copyItem.Origin = copyItem.Origin + Vector3d.ZAxis.MultiplyBy(copyItem.ZOffSet);
                        }
                        beams.Add(copyItem);
                    }
                    if (isTopFloor && beams.Count > 0)
                    {
                        var beamDic = beams.ToDictionary(key => CreatBeamOutLine(key), value => value);
                        var beamSpatialIndex = new ThCADCoreNTSSpatialIndex(beamDic.Keys.ToCollection());
                        foreach (var wall in walls)
                        {
                            var pl = (wall.Outline as Polyline).Buffer(20)[0] as Polyline;
                            var objs = beamSpatialIndex.SelectFence(pl);
                            if (objs.Count > 0)
                                wall.Height = Math.Min(wall.Height, objs.Cast<Polyline>().Max(o => beamDic[o].ZOffSet + beamDic[o].Height));
                        }
                        foreach (var column in columns)
                        {
                            var pl = (column.Outline as Polyline).Buffer(20)[0] as Polyline;
                            var objs = beamSpatialIndex.SelectFence(pl);
                            if (objs.Count > 0)
                                column.Height = Math.Min(column.Height, objs.Cast<Polyline>().Max(o => beamDic[o].ZOffSet + beamDic[o].Height));
                        }
                    }
                    var slabs = levelEntitys.FloorEntitys.OfType<ThTCHSlab>().ToList();
                    foreach (var item in slabs)
                    {
                        var copyItem = item.Clone() as ThTCHSlab;
                        copyItem.Uuid += buildingStorey.Number;
                        if (IsStructure)
                            (copyItem.Outline as Polyline).Elevation += buildingStorey.Height;
                        buildingStorey.Slabs.Add(copyItem);
                    }
                    //buildingStorey.Slabs.AddRange(slabs);
                    var railings = levelEntitys.FloorEntitys.OfType<ThTCHRailing>().ToList();
                    foreach (var railing in railings)
                    {
                        var copyItem = railing.Clone() as ThTCHRailing;
                        copyItem.Uuid += buildingStorey.Number;
                        buildingStorey.Railings.Add(copyItem);
                    }
                    //buildingStorey.Railings.AddRange(railings);
                    buildingStorey.Walls.AddRange(walls);
                    buildingStorey.Columns.AddRange(columns);
                    buildingStorey.Beams.AddRange(beams);
                }
                thBuilding.Storeys.Add(buildingStorey);
            }
            thSite.Building = thBuilding;
            thPrj.Site = thSite;
            return thPrj;
        }

        private Polyline CreatBeamOutLine(ThTCHBeam beam)
        {
            var result = new Polyline();
            var vector = beam.XVector.GetPerpendicularVector().GetNormal();
            result.AddVertexAt(0, (beam.Origin - beam.XVector * beam.Length / 2 - vector * beam.Width / 2).ToPoint2D(), 0, 0, 0);
            result.AddVertexAt(0, (beam.Origin + beam.XVector * beam.Length / 2 - vector * beam.Width / 2).ToPoint2D(), 0, 0, 0);
            result.AddVertexAt(0, (beam.Origin + beam.XVector * beam.Length / 2 + vector * beam.Width / 2).ToPoint2D(), 0, 0, 0);
            result.AddVertexAt(0, (beam.Origin - beam.XVector * beam.Length / 2 + vector * beam.Width / 2).ToPoint2D(), 0, 0, 0);
            result.Closed = true;
            return result;
        }

        public ThTCHProjectData DWGToProjectData(bool isMemoryStory, bool railingToRegion, bool isSelectFloor = false)
        {
            string prjId = "";
            string prjName = "测试项目";
            var jsonConfig = new Dictionary<string, List<ThEditStoreyInfo>>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                prjName = Active.DocumentName;
                prjId = Active.Document.UnmanagedObject.ToString();
                jsonConfig = GetStoreyJsonFile(Active.Document.Name);
            }
            if (jsonConfig.Count == 0)
            {
                Active.Database.GetEditor().WriteMessage("未找到项目文件对应的楼层配置，请检查。");
                return null;
            }
            var thPrj = new ThTCHProjectData();
            thPrj.Root = new ThTCHRootData()
            {
                GlobalId = prjId,
                Name = prjName,
                Description = "ThTCHProjectData"
            };
            var thSite = new ThTCHSiteData();
            thSite.Root = new ThTCHRootData();
            thSite.Root.GlobalId = prjId + "site";
            var thTCHBuildingData = new ThTCHBuildingData();
            thTCHBuildingData.Root = new ThTCHRootData();
            thTCHBuildingData.Root.GlobalId = prjId + "Building";
            var floorOrigin = GetFloorBlockPolylines(isSelectFloor);
            if (floorOrigin.Count < 1)
            {
                return null;
            }
            LoadCustomElements();
            var allEntitys = null != archDBData ? archDBData.AllTArchEntitys() : GetArchEntities();
            var allDBEntitys = null != archDBData ? new List<THStructureEntity>() : GetDBStructureEntities();
            InitFloorDBEntity(allEntitys, allDBEntitys);
            var entityConvert = new TCHDBEntityConvert(prjId);
            foreach (var floor in floorOrigin)
            {
                var floorEntitys = FloorEntitys(floor.FloorOutLine, out List<FloorCurveEntity> curveEntities);
                var moveVector = Point3d.Origin - floor.FloorOrigin;
                Matrix3d matrix = Matrix3d.Displacement(moveVector);
                var thisFloorWalls = floorEntitys.OfType<WallEntity>().Select(c => c.DBArchEntity).Cast<TArchWall>().ToList();
                var thisFloorDoors = floorEntitys.OfType<DoorEntity>().Select(c => c.DBArchEntity).Cast<TArchDoor>().ToList();
                var thisFloorWindows = floorEntitys.OfType<WindowEntity>().Select(c => c.DBArchEntity).Cast<TArchWindow>().ToList();
                var walls = entityConvert.WallDataDoorWindowRelation(thisFloorWalls, thisFloorDoors, thisFloorWindows, moveVector);
                floor.FloorEntitys.AddRange(walls);

                var allSlabs = new List<ThTCHSlabData>();
                var thisRailingEntitys = new Dictionary<Polyline, ThTCHRailingData>();
                var railingColls = new DBObjectCollection();
                var thisOpeningEntities = new Dictionary<Polyline, ThTCHOpeningData>();
                var openingColls = new DBObjectCollection();
                foreach (var item in curveEntities)
                {
                    if (item.EntitySystem.Contains("楼板"))
                    {
                        if (item.FloorEntity != null && item.FloorEntity is SlabPolyline slab1)
                        {
                            var struAndArchSlab = CreateStruAndArchSlabData(slab1, matrix);
                            var n = 1;
                            struAndArchSlab.ForEach(slab =>
                            {
                                if (item.Property is SlabProperty slabProp)
                                {
                                    slab.BuildElement.Outline.ZOffSet(slabProp.TopElevation);
                                    slab.BuildElement.EnumMaterial = slabProp.EnumMaterial.GetDescription();
                                }
                                slab.BuildElement.Root.GlobalId = prjId + item.Id + n;
                                n++;
                                allSlabs.Add(slab);
                            });
                        }
                    }
                    else if (item.EntitySystem.Contains("栏杆"))
                    {
                        if (item.EntityCurve is Polyline polyline)
                        {
                            var pLine = polyline.GetTransformedCopy(matrix) as Polyline;
                            railingColls.Add(pLine);
                            var railing = CreateRailingData(pLine);
                            railing.BuildElement.Height = ralingHeight;
                            railing.BuildElement.Root.GlobalId = prjId + item.Id;
                            thisRailingEntitys.Add(pLine, railing);
                        }
                    }
                    else if (item.EntitySystem.Contains("墙洞"))
                    {
                        if (item.EntityCurve is Polyline polyline)
                        {
                            var pLine = polyline.GetTransformedCopy(matrix) as Polyline;
                            openingColls.Add(pLine);
                            var opening = CreateOpeningData(pLine);
                            var prop = item.Property as HoleProperty;
                            opening.BuildElement.Height = prop.Height;
                            opening.BuildElement.Root.GlobalId = prjId + item.Id;
                            opening.BuildElement.Properties.Add(new ThTCHProperty() { Key = "ShowDimension", Value = prop.ShowDimension.ToString() });
                            opening.BuildElement.Properties.Add(new ThTCHProperty() { Key = "Hidden", Value = prop.Hidden.ToString() });
                            opening.BuildElement.Properties.Add(new ThTCHProperty() { Key = "BottomElevation", Value = prop.BottomElevation.ToString() });
                            opening.BuildElement.Properties.Add(new ThTCHProperty() { Key = "NumberPrefix", Value = prop.NumberPrefix.ToString() });
                            opening.BuildElement.Properties.Add(new ThTCHProperty() { Key = "NumberPostfix", Value = prop.NumberPostfix.ToString() });
                            opening.BuildElement.Properties.Add(new ThTCHProperty() { Key = "ElevationDisplay", Value = prop.ElevationDisplay.ToString() });
                            thisOpeningEntities.Add(pLine, opening);
                        }
                    }
                }
                //用墙的索引找栏杆没有找到
                var railingSpatialIndex = new ThCADCoreNTSSpatialIndex(railingColls);
                var openingSpatialIndex = new ThCADCoreNTSSpatialIndex(openingColls);
                List<Polyline> hisPLines = new List<Polyline>();
                foreach (var wall in walls)
                {
                    if (wall.BuildElement.Height < 10 || wall.BuildElement.Height > 2000)
                        continue;
                    if (hisPLines.Count == thisRailingEntitys.Count)
                        break;
                    // 栏杆
                    var crossRailings = railingSpatialIndex.SelectCrossingPolygon(wall.BuildElement.Outline.ToPolyline()).OfType<Polyline>().ToList();
                    foreach (var polyline in crossRailings)
                    {
                        if (hisPLines.Any(c => c == polyline)) 
                            continue;
                        hisPLines.Add(polyline);
                        var railing = thisRailingEntitys[polyline];
                        railing.BuildElement.Outline.ZOffSet(wall.BuildElement.Height);
                        railing.BuildElement.Height = 800;
                    }
                }
                foreach (var wall in walls)
                {
                    // 墙洞
                    var wallOutLine = wall.BuildElement.Outline.ToPolyline();
                    var crossOpenings = openingSpatialIndex.SelectCrossingPolygon(wallOutLine).OfType<Polyline>().ToList();
                    foreach (var polyline in crossOpenings)
                    {
                        if (!wallOutLine.Contains(polyline.GetCenter()))
                        {
                            continue;
                        }
                        var opening = thisOpeningEntities[polyline];
                        wall.Openings.Add(opening);
                        if (!polyline.Closed)
                        {
                            // 更新外轮廓
                            var newOutline = opening.BuildElement.Outline.ToPolyline().Buffer(wall.BuildElement.Width / 2 + 100.0).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                            if (!newOutline.IsNull())
                            {
                                opening.BuildElement.Outline = newOutline.ToTCHPolyline();
                            }
                        }
                    }
                }
                floor.FloorEntitys.AddRange(allSlabs);
                floor.FloorEntitys.AddRange(thisRailingEntitys.Select(c => c.Value).ToList());
            }

            var floorData = GetBlockElevtionValue(floorOrigin, jsonConfig);
            foreach (var floor in floorData)
            {
                var levelEntitys = floorOrigin.Find(c => c.FloorName == floor.FloorName);
                if (levelEntitys == null)
                    continue;
                var buildingStorey = new ThTCHBuildingStoreyData();
                buildingStorey.BuildElement = new ThTCHBuiltElementData();
                buildingStorey.BuildElement.Root = new ThTCHRootData();
                buildingStorey.BuildElement.Root = new ThTCHRootData();
                buildingStorey.BuildElement.Root.GlobalId = prjId + floor.Num.ToString();
                buildingStorey.BuildElement.Root.Name = floor.Num.ToString();
                buildingStorey.BuildElement.Root.Description = "ThDefinition" + floor.FloorName;
                buildingStorey.Number = floor.Num.ToString();
                buildingStorey.Height = floor.LevelHeight;
                buildingStorey.Elevation = floor.Elevation;
                buildingStorey.Usage = floor.FloorName;
                buildingStorey.Origin = new ThTCHPoint3d() { X = 0, Y = 0, Z = floor.Elevation };
                buildingStorey.BuildElement.Properties.Add(new ThTCHProperty { Key =  "FloorNo", Value = floor.Num.ToString()});
                buildingStorey.BuildElement.Properties.Add(new ThTCHProperty { Key =  "Height", Value = floor.LevelHeight.ToString()});
                buildingStorey.BuildElement.Properties.Add(new ThTCHProperty { Key =  "StdFlrNo", Value = floor.Num.ToString()});
                ThTCHBuildingStoreyData memoryStory = null;
                if (isMemoryStory)
                {
                    foreach (var item in thTCHBuildingData.Storeys)
                    {
                        if (!string.IsNullOrEmpty(item.MemoryStoreyId) || item.Usage != floor.FloorName)
                            continue;
                        if (Math.Abs(item.Height - floor.LevelHeight) < 1)
                        {
                            memoryStory = item;
                        }
                    }
                }
                if (null != memoryStory)
                {
                    buildingStorey.MemoryStoreyId = memoryStory.BuildElement.Root.GlobalId;
                    buildingStorey.MemoryMatrix3D = Matrix3d.Displacement(buildingStorey.Origin.ToPoint3d() - memoryStory.Origin.ToPoint3d()).ToTCHMatrix3d();
                }

                else
                {
                    var walls = new List<ThTCHWallData>();
                    foreach (var item in levelEntitys.FloorEntitys.OfType<ThTCHWallData>().ToList())
                    {
                        var copyItem = item.Clone();
                        if (Math.Abs(copyItem.BuildElement.Height) < 10)
                            copyItem.BuildElement.Height = floor.LevelHeight;
                        foreach (var door in copyItem.Doors)
                        {
                            door.BuildElement.Root.GlobalId += buildingStorey.Number;
                        }
                        foreach (var window in copyItem.Windows)
                        {
                            window.BuildElement.Root.GlobalId += buildingStorey.Number;
                        }
                        foreach (var opening in copyItem.Openings)
                        {
                            opening.BuildElement.Root.GlobalId += buildingStorey.Number;
                        }
                        walls.Add(copyItem);
                    }

                    var slabs = levelEntitys.FloorEntitys.OfType<ThTCHSlabData>().ToList();
                    foreach (var item in slabs)
                    {
                        var copyItem = item.Clone();
                        copyItem.BuildElement.Root.GlobalId += buildingStorey.Number;
                        buildingStorey.Slabs.Add(copyItem);
                    }

                    var railings = levelEntitys.FloorEntitys.OfType<ThTCHRailingData>().ToList();
                    foreach (var railing in railings)
                    {
                        var copyItem = railing.Clone();
                        copyItem.BuildElement.Root.GlobalId += buildingStorey.Number;
                        buildingStorey.Railings.Add(copyItem);
                    }

                    buildingStorey.Walls.AddRange(walls);
                }
                thTCHBuildingData.Storeys.Add(buildingStorey);
            }
            thSite.Buildings.Add(thTCHBuildingData);
            thPrj.Site = thSite;
            return thPrj;
        }

        private List<FloorCurveEntity> BuildFloorSlab(List<FloorCurveEntity> data, DBObjectCollection textColl)
        {
            var dicData = new Dictionary<Polyline, FloorCurveEntity>();
            foreach (var item in data)
            {
                dicData.Add(item.EntityCurve as Polyline, item);
            }
            var slabPolylines = dicData.Keys.OrderBy(o => o.Area).ToList();
            var allSlabs = new List<SlabPolyline>();
            var hisCoordinates = new List<Point3d>();
            var slabTextSpIndex = new ThCADCoreNTSSpatialIndex(textColl);
            var maxHeight = 0.0;
            foreach (var item in slabPolylines)
            {
                var itemTemp = dicData[item];
                var addSlab = new SlabPolyline(item);
                if (itemTemp.Property is SlabProperty slabProp)
                {
                    addSlab.StructureThickness = slabProp.Thickness;
                    addSlab.SurfaceThickness = slabProp.SurfaceThickness;
                    addSlab.OutPolyline.Elevation = slabProp.TopElevation;
                }
                else if (itemTemp.Property is DescendingProperty desProp)
                {
                    addSlab.StructureThickness = desProp.StructureThickness;
                    addSlab.SurfaceThickness = desProp.SurfaceThickness;
                    addSlab.StructureWrapThickness = desProp.StructureWrapThickness;
                    addSlab.WrapSurfaceThickness = desProp.WrapSurfaceThickness;
                }
                var insertText = slabTextSpIndex.SelectCrossingPolygon(item);
                var insertText1 = slabTextSpIndex.SelectWindowPolygon(item);
                if (insertText.Count < 1)
                {
                    allSlabs.Add(addSlab);
                    continue;
                }
                var height = 0.0;
                foreach (var obj in insertText)
                {
                    if (obj is DBText text)
                    {
                        if (hisCoordinates.Any(c => c == text.Position))
                            continue;
                        hisCoordinates.Add(text.Position);
                        double.TryParse(text.TextString, out height);
                        break;
                    }
                    else if (obj is MText mText)
                    {
                        if (hisCoordinates.Any(c => c == mText.Location))
                            continue;
                        hisCoordinates.Add(mText.Location);
                        double.TryParse(mText.Contents, out height);
                        break;
                    }
                }
                addSlab.LowerPlateHeight = height;
                maxHeight = maxHeight <= height ? maxHeight : height;
                allSlabs.Add(addSlab);
            }
            allSlabs = allSlabs.OrderByDescending(c => c.OutPolyline.Area).ToList();

            var hisIndex = new List<int>();
            for (int i = 0; i < allSlabs.Count; i++)
            {
                if (hisIndex.Any(c => c == i))
                    continue;
                var slab = allSlabs[i];
                var outPLine = slab.OutPolyline;
                for (int j = i + 1; j < allSlabs.Count; j++)
                {
                    var innerSlab = allSlabs[j];
                    var pLine = innerSlab.OutPolyline;
                    if (pLine.Area < 10)
                    {
                        hisIndex.Add(j);
                        continue;
                    }
                    if (hisIndex.Any(c => c == j))
                        continue;
                    var insert = outPLine.GeometryIntersection(pLine);
                    if (insert == null || insert.Count < 1)
                        continue;
                    var insertPLines = insert.OfType<Polyline>();
                    var area = insertPLines.Sum(c => c.Area);
                    if (area / pLine.Area < 0.8)
                        continue;
                    if (Math.Abs(innerSlab.LowerPlateHeight) < 1)
                    {
                        innerSlab.IsOpening = true;
                        innerSlab.LowerPlateHeight = maxHeight;
                    }
                    else
                    {
                    }
                    hisIndex.Add(j);
                    slab.InnerSlabOpenings.Add(innerSlab);
                }
            }

            var results = new List<FloorCurveEntity>();
            for (int i = 0; i < allSlabs.Count; i++)
            {
                if (hisIndex.Any(c => c == i))
                    continue;

                var slab = allSlabs[i];
                var item = data.Where(o => o.EntityCurve == slab.OutPolyline).FirstOrDefault();
                if (item != null)
                {
                    item.FloorEntity = slab;
                    results.Add(item);
                }
            }
            return results;
        }

        private void LoadCustomElements()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var visitors = new ThBuildingElementExtractionVisitor[]
                {
                    new THDBFFLExtractionVisitor()
                    {
                        LayerFilter = new List<string> { "TH-楼板", "TH-降板" },
                    },
                    new THDBRailingExtractionVisitor()
                    {
                        LayerFilter = new List<string> { "TH-栏杆" },
                    },
                    new THDBHoleExtractionVisitor()
                    {
                        LayerFilter = new List<string> { "TH-墙洞" },
                    },
                };
                var extractor = new ThBuildingElementExtractor();
                extractor.Accept(visitors);
                extractor.Extract(acdb.Database);
                extractor.ExtractFromMS(acdb.Database);

                var annoVisitors = new ThAnnotationElementExtractionVisitor[]
                {
                    new THDBFFLMarkExtractionVisitor()
                    {
                        LayerFilter = new List<string> { "TH-降板" },
                    }
                };
                var annoExtractor = new ThAnnotationElementExtractor();
                annoExtractor.Accept(annoVisitors);
                annoExtractor.Extract(acdb.Database);
                annoExtractor.ExtractFromMS(acdb.Database);

                // 获取栏杆数据
                cadCurveEntities.AddRange(visitors[1].Results.Select(o => o.Data).OfType<FloorCurveEntity>());

                // 获取墙洞数据
                cadCurveEntities.AddRange(visitors[2].Results.Select(o => o.Data).OfType<FloorCurveEntity>());

                // 获取楼板（包括降板数据）
                var slabs = visitors[0].Results.Select(o => o.Data).OfType<FloorCurveEntity>().ToList();
                var marks = annoVisitors[0].Results.Select(o => o.Geometry).ToCollection();
                cadCurveEntities.AddRange(BuildFloorSlab(slabs, marks));
            }
        }

        private List<FloorBlock> GetFloorBlockPolylines(bool isSelectFloor)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var floorBlocks = new List<BlockReference>();
                var originBlocks = new List<BlockReference>();
                if (isSelectFloor)
                {
                    //选择区域
                    Active.Editor.WriteLine("\n请选择楼层块");
                    var result = Active.Editor.GetSelection();
                    if (result.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                    {
                        return new List<FloorBlock>();
                    }
                    foreach (ObjectId obj in result.Value.GetObjectIds())
                    {
                        Entity e = acdb.Element<Entity>(obj);
                        if (e is BlockReference b && !b.BlockTableRecord.IsNull)
                        {
                            var name = ThMEPXRefService.OriginalFromXref(b.GetEffectiveName());
                            if (name.ToLower().StartsWith("thape") && name.EndsWith("inner"))
                            {
                                floorBlocks.Add(b);
                            }
                            else if (name == "BASEPOINT")
                            {
                                originBlocks.Add(b);
                            }
                        }
                    }
                }
                else
                {
                    acdb.ModelSpace.OfType<BlockReference>()
                        .Where(o => !o.BlockTableRecord.IsNull)
                        .ForEach(o =>
                        {
                            var name = ThMEPXRefService.OriginalFromXref(o.GetEffectiveName());
                            if (name.ToLower().StartsWith("thape") && name.EndsWith("inner"))
                            {
                                floorBlocks.Add(o);
                            }
                            else if (name == "BASEPOINT")
                            {
                                originBlocks.Add(o);
                            }
                        });
                }
                var floorOrigins = new List<FloorBlock>();
                foreach (var floor in floorBlocks)
                {
                    string floorName = "";
                    var visAttrs = BlockTools.GetAttributesInBlockReference(floor.Id, true);
                    foreach (var attr in visAttrs)
                    {
                        if (attr.Key.Equals("内框名称"))
                        {
                            floorName = attr.Value;
                            break;
                        }
                    }
                    var floorOutLine = GetFloorBlockOutLine(floor);
                    foreach (var basePoint in originBlocks)
                    {
                        var point = basePoint.Position;
                        point = new Point3d(point.X, point.Y, 0);
                        if (floorOutLine.Contains(point))
                        {
                            floorOrigins.Add(new FloorBlock(floorName, floorOutLine, point));
                            break;
                        }
                    }
                }
                return floorOrigins;
            }
        }

        Polyline GetFloorBlockOutLine(BlockReference floor)
        {
            return floor.GeometricExtents.ToRectangle();
        }

        List<LevelElevation> GetBlockElevtionValue(List<FloorBlock> floorBlocks, Dictionary<string, List<ThEditStoreyInfo>> jsonConfigs)
        {
            var res = new List<LevelElevation>();
            var storeyConfig = jsonConfigs.First().Value;//经过确认，暂时认为只有一栋楼不考虑多楼情况，支取First
            foreach (var floor in floorBlocks)
            {
                var name = floor.FloorName;
                var configs = storeyConfig.Where(o => o.PaperName.Equals(name));
                configs.ForEach(config =>
                {
                    var heigth = config.Height;
                    var storeyName = config.StoreyName;
                    var elevation = config.Bottom_Elevation;
                    res.Add(new LevelElevation { Num = storeyName, Elevation = elevation, LevelHeight = heigth, FloorName = name });
                });
            }
            return res.OrderBy(o => o.Elevation).ToList();
        }

        void InitFloorDBEntity(List<TArchEntity> allTArchEntities, List<THStructureEntity> allDBEntities)
        {
            var addTArchColl = new DBObjectCollection();
            foreach (var item in allTArchEntities)
            {
                var thEntity = DBToTHEntityCommon.DBArchToTHArch(item);
                if (thEntity == null)
                    continue;
                entityBases.Add(thEntity);
                if (null == thEntity.Outline)
                    continue;
                addTArchColl.Add(thEntity.Outline);
                entityDic.Add(thEntity.Outline, thEntity);
            }
            spatialIndex = new ThCADCoreNTSSpatialIndex(addTArchColl);
            addTArchColl.Clear();
            foreach (var item in cadCurveEntities)
            {
                addTArchColl.Add(item.EntityCurve);
                cadEntityDic.Add(item.EntityCurve, item);
            }

            var addDBColl = new DBObjectCollection();
            foreach (var item in allDBEntities)
            {
                addDBColl.Add(item.Outline);
                dbEntityDic.Add(item.Outline, item);
            }
            entitySpatialIndex = new ThCADCoreNTSSpatialIndex(addTArchColl);
            dbEntitySpatialIndex = new ThCADCoreNTSSpatialIndex(addDBColl);
        }
        List<THArchEntityBase> FloorEntitys(Polyline outPLine, out List<FloorCurveEntity> curveEntities)
        {
            curveEntities = new List<FloorCurveEntity>();
            var resList = new List<THArchEntityBase>();
            var crossPLines = spatialIndex.SelectCrossingPolygon(outPLine).Cast<MPolygon>().ToList();
            foreach (var pline in crossPLines)
            {
                resList.Add(entityDic[pline]);
            }
            var crossCurves = entitySpatialIndex.SelectCrossingPolygon(outPLine).OfType<Entity>().ToList();
            foreach (var pline in crossCurves)
            {
                curveEntities.Add(cadEntityDic[pline]);
            }
            return resList;
        }

        List<THStructureEntity> FloorDBEntitys(Polyline outPLine)
        {
            var resList = new List<THStructureEntity>();
            var crossPLines = dbEntitySpatialIndex.SelectCrossingPolygon(outPLine).Cast<Polyline>().ToList();
            foreach (var pline in crossPLines)
            {
                resList.Add(dbEntityDic[pline]);
            }
            return resList;
        }

        private ThTCHRailing CreateRailing(Polyline pline)
        {
            return new ThTCHRailing()
            {
                Height = 1200,
                Width = 60,
                Outline = pline,
                ExtrudedDirection = Vector3d.ZAxis,
            };
        }

        private ThTCHOpening CreateOpening(Polyline pline)
        {
            return new ThTCHOpening()
            {
                Outline = pline,
                ExtrudedDirection = Vector3d.ZAxis,
            };
        }

        private ThTCHOpeningData CreateOpeningData(Polyline pline)
        {
            var opening = new ThTCHOpeningData();
            opening.BuildElement = new ThTCHBuiltElementData();
            opening.BuildElement.Root = new ThTCHRootData();
            opening.BuildElement.Outline = pline.ToTCHPolyline();
            return opening;
        }

        private ThTCHRailingData CreateRailingData(Polyline pline)
        {
            var centerline = pline;
            var width = 60;
            var outlines = centerline.BufferFlatPL(width / 2.0)[0] as Polyline;
            var railing = new ThTCHRailingData();
            railing.BuildElement = new ThTCHBuiltElementData();
            railing.BuildElement.Root = new ThTCHRootData();
            railing.BuildElement.Height = 1200;
            railing.BuildElement.Width = width;
            railing.BuildElement.Outline = outlines.ToTCHPolyline();
            return railing;
        }

        private List<ThTCHSlab> CreateStruAndArchSlab(SlabPolyline slabPolyline, Matrix3d matrix)
        {
            var slabs = new List<ThTCHSlab>();
            var outPLine = slabPolyline.OutPolyline.GetTransformedCopy(matrix) as Polyline;
            outPLine = ThMEPFrameService.Normalize(outPLine);
            var structureSlab = new ThTCHSlab(outPLine, slabPolyline.StructureThickness, Vector3d.ZAxis);
            structureSlab.ZOffSet = -slabPolyline.SurfaceThickness;
            var architectureSlab = new ThTCHSlab(outPLine, slabPolyline.SurfaceThickness, Vector3d.ZAxis);
            var outPLineColl = new DBObjectCollection { outPLine };
            foreach (var item in slabPolyline.InnerSlabOpenings)
            {
                // 降板
                var innerPLine = item.OutPolyline.GetTransformedCopy(matrix) as Polyline;
                // 裁剪外围部分
                innerPLine = innerPLine.Intersection(outPLineColl).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                innerPLine = ThMEPFrameService.Normalize(innerPLine);
                if (innerPLine.IsNull())
                {
                    continue;
                }

                // 结构降板
                if (!item.IsOpening)
                {
                    // 降板外轮廓
                    var outlineBuffer = innerPLine.Buffer(item.StructureWrapThickness).OfType<Polyline>()
                        .OrderByDescending(p => p.Area).FirstOrDefault();
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    outlineBuffer = outlineBuffer.Intersection(outPLineColl).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                    outlineBuffer = ThMEPFrameService.Normalize(outlineBuffer);
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    structureSlab.Descendings.Add(new ThTCHDescending()
                    {
                        Outline = innerPLine,
                        OutlineBuffer = outlineBuffer,
                        IsDescending = true,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                        DescendingThickness = item.StructureThickness,
                        DescendingWrapThickness = item.StructureWrapThickness,
                    });
                }
                else
                {
                    structureSlab.Descendings.Add(new ThTCHDescending()
                    {
                        Outline = innerPLine,
                        IsDescending = false,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                    });
                }

                // 建筑降板
                if (!item.IsOpening)
                {
                    // 降板内轮廓
                    var outlineBuffer = innerPLine.Buffer(-item.WrapSurfaceThickness).OfType<Polyline>()
                        .OrderByDescending(p => p.Area).FirstOrDefault();
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    outlineBuffer = outlineBuffer.Intersection(outPLineColl).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                    outlineBuffer = ThMEPFrameService.Normalize(outlineBuffer);
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    architectureSlab.Descendings.Add(new ThTCHDescending()
                    {
                        Outline = outlineBuffer,
                        OutlineBuffer = innerPLine,
                        IsDescending = true,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                        DescendingThickness = item.SurfaceThickness,
                        DescendingWrapThickness = item.WrapSurfaceThickness,
                    });
                }
                else
                {
                    architectureSlab.Descendings.Add(new ThTCHDescending()
                    {
                        Outline = innerPLine,
                        IsDescending = false,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                    });
                }
            }
            slabs.Add(structureSlab);
            slabs.Add(architectureSlab);
            return slabs;
        }

        private List<ThTCHSlabData> CreateStruAndArchSlabData(SlabPolyline slabPolyline, Matrix3d matrix)
        {
            var slabs = new List<ThTCHSlabData>();
            var outPLine = slabPolyline.OutPolyline.GetTransformedCopy(matrix) as Polyline;
            
            var structureSlab = new ThTCHSlabData();
            structureSlab.BuildElement = new ThTCHBuiltElementData();
            structureSlab.BuildElement.Root = new ThTCHRootData();
            structureSlab.BuildElement.Outline = outPLine.ToTCHPolyline();
            structureSlab.BuildElement.Height = slabPolyline.StructureThickness;
            structureSlab.BuildElement.EnumMaterial = EnumSlabMaterial.ReinforcedConcrete.GetDescription();
            structureSlab.BuildElement.Outline.ZOffSet(-slabPolyline.SurfaceThickness);

            var architectureSlab = new ThTCHSlabData();
            architectureSlab.BuildElement = new ThTCHBuiltElementData();
            architectureSlab.BuildElement.Root = new ThTCHRootData();
            architectureSlab.BuildElement.Outline = outPLine.ToTCHPolyline();
            architectureSlab.BuildElement.Height = slabPolyline.SurfaceThickness;
            architectureSlab.BuildElement.EnumMaterial = EnumSlabMaterial.ReinforcedConcrete.GetDescription();

            var outPLineColl = new DBObjectCollection { outPLine };
            foreach (var item in slabPolyline.InnerSlabOpenings)
            {
                // 降板
                var innerPLine = item.OutPolyline.GetTransformedCopy(matrix) as Polyline;
                // 裁剪外围部分
                innerPLine = innerPLine.Intersection(outPLineColl).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                if (innerPLine.IsNull())
                {
                    continue;
                }

                // 结构降板
                if (!item.IsOpening)
                {
                    // 降板外轮廓
                    var outlineBuffer = innerPLine.Buffer(item.StructureWrapThickness).OfType<Polyline>()
                        .OrderByDescending(p => p.Area).FirstOrDefault();
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    outlineBuffer = outlineBuffer.Intersection(outPLineColl).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                    outlineBuffer = ThMEPFrameService.Normalize(outlineBuffer);
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    structureSlab.Descendings.Add(new ThTCHDescendingData()
                    {
                        Outline = innerPLine.ToTCHPolyline(),
                        OutlineBuffer = outlineBuffer.ToTCHPolyline(),
                        IsDescending = true,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                        DescendingThickness = item.StructureThickness,
                        DescendingWrapThickness = item.StructureWrapThickness,
                    });
                }
                else
                {
                    structureSlab.Descendings.Add(new ThTCHDescendingData()
                    {
                        Outline = innerPLine.ToTCHPolyline(),
                        IsDescending = false,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                    });
                }

                // 建筑降板
                if (!item.IsOpening)
                {
                    // 降板内轮廓
                    var outlineBuffer = innerPLine.Buffer(-item.SurfaceThickness).OfType<Polyline>()
                        .OrderByDescending(p => p.Area).FirstOrDefault();
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    outlineBuffer = outlineBuffer.Intersection(outPLineColl).OfType<Polyline>().OrderByDescending(p => p.Area).FirstOrDefault();
                    outlineBuffer = ThMEPFrameService.Normalize(outlineBuffer);
                    if (outlineBuffer.IsNull())
                    {
                        continue;
                    }
                    architectureSlab.Descendings.Add(new ThTCHDescendingData()
                    {
                        Outline = outlineBuffer.ToTCHPolyline(),
                        OutlineBuffer = innerPLine.ToTCHPolyline(),
                        IsDescending = true,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                        DescendingThickness = item.SurfaceThickness,
                        DescendingWrapThickness = item.WrapSurfaceThickness,
                    });
                }
                else
                {
                    architectureSlab.Descendings.Add(new ThTCHDescendingData()
                    {
                        Outline = innerPLine.ToTCHPolyline(),
                        IsDescending = false,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                    });
                }
            }
            slabs.Add(structureSlab);
            slabs.Add(architectureSlab);
            return slabs;
        }

        ThTCHWallData WallEntityToTCHWallData(string projectId, THStructureWall wall, Matrix3d matrix)
        {
            var pl = wall.Outline.Clone() as Polyline;
            pl.TransformBy(matrix);
            //pl.Closed = false;
            pl.Elevation = 0.0;
            var newWall = new ThTCHWallData();
            newWall.BuildElement = new ThTCHBuiltElementData()
            {
                Outline = pl.ToTCHPolyline(),
                Root = new ThTCHRootData() { GlobalId = projectId + wall.Uuid },
            };
            return newWall;
        }

        ThTCHWall WallEntityToTCHWall(string projectId, THStructureWall wall, Matrix3d matrix)
        {
            var pl = wall.Outline.Clone() as Polyline;
            pl.TransformBy(matrix);
            //pl.Closed = false;
            pl.Elevation = 0.0;
            var newWall = new ThTCHWall(pl, -1);
            newWall.Uuid = projectId + wall.Uuid;
            return newWall;
        }

        ThTCHColumn ColumnEntityToTCHColumn(string projectId, THStructureColumn column, Matrix3d matrix)
        {
            var pl = column.Outline.Clone() as Polyline;
            pl.TransformBy(matrix);
            //pl.Closed = false;
            pl.Elevation = 0.0;
            var newColumn = new ThTCHColumn(pl, -1);
            newColumn.Uuid = projectId + column.Uuid;
            return newColumn;
        }
        ThTCHBeam BeamEntityToTCHBeam(string projectId, THStructureBeam beam, Matrix3d matrix)
        {
            var origin = beam.Origin.TransformBy(matrix);
            var newBeam = new ThTCHBeam(beam.Width, beam.Length, beam.Height, beam.XVector, origin);
            newBeam.Uuid = projectId + beam.Uuid;
            newBeam.ZOffSet = beam.RelativeBG;
            return newBeam;
        }

        ThTCHSlab BeamEntityToTCHSlab(string projectId, THStructureSlab slab, Matrix3d matrix)
        {
            var pl = slab.Outline.Clone() as Polyline;
            pl.TransformBy(matrix);
            //pl.Closed = false;
            pl.Elevation = slab.RelativeBG;
            var newSlab = new ThTCHSlab(pl, slab.Height, Vector3d.ZAxis);
            newSlab.Uuid = projectId + slab.Uuid;
            return newSlab;
        }

    }
}
