using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model.Common;
using ThMEPTCH.CAD;
using ThMEPTCH.Model;
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
        List<FloorCurveEntity> cadCurveEntitys = new List<FloorCurveEntity>();
        Dictionary<Entity, FloorCurveEntity> cadEntityDic = new Dictionary<Entity, FloorCurveEntity>();
        double ralingHeight =1200;
        double slabThickness = 100;
        public ThDWGToIFCService(string dbPath)
        {
            if(!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
                archDBData = new TCHArchDBData(dbPath);
        }

        public List<TArchEntity> GetArchEntities()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var engine = new ThTCHBuildingElementExtractionEngine();
                engine.ExtractFromMS(acdb.Database);
                return engine.Results.Select(o => o.Data as TArchEntity).ToList();
            }
        }
        
        public List<THStructureEntity> GetDBEntities()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var building = new ThDBStructureElementBuilding();
                var structureElements = building.BuildingFromMS(acdb.Database);

                return structureElements;
                //return engine.Elements.Results.Select(o => o.Data as THStructureEntity).ToList();
            }
        }

        public ThTCHProject DWGToProject(bool isMemoryStory,bool railingToRegion)
        {
            string prjId = "";
            string prjName = "测试项目";
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                prjName = Active.DocumentName;
                prjId = Active.Document.UnmanagedObject.ToString();
            }
            var thPrj = new ThTCHProject();
            thPrj.Uuid = prjId;
            thPrj.ProjectName = prjName;
            var thSite = new ThTCHSite();
            thSite.Uuid = prjId + "site";
            var thBuilding = new ThTCHBuilding();
            thBuilding.Uuid = prjId + "Building";
            var floorOrigin = GetFloorBlockPolylines();
            var allEntitys = null != archDBData? archDBData.AllTArchEntitys(): GetArchEntities();
            var allDBEntitys = null != archDBData ? new List<THStructureEntity>() : GetDBEntities();
            InitFloorDBEntity(allEntitys, allDBEntitys);
            var entityConvert = new TCHDBEntityConvert(prjId);
            foreach (var floor in floorOrigin)
            {
                var floorEntitys = FloorEntitys(floor.FloorOutLine, out List<FloorCurveEntity> curveEntities);
                var moveVector = Point3d.Origin - floor.FloorOrigin;
                Matrix3d matrix = Matrix3d.Displacement(moveVector);
                var thisFloorWalls = floorEntitys.OfType<WallEntity>().Select(c => c.DBArchEntiy).Cast<TArchWall>().ToList();
                var thisFloorDoors = floorEntitys.OfType<DoorEntity>().Select(c => c.DBArchEntiy).Cast<TArchDoor>().ToList();
                var thisFloorWindows = floorEntitys.OfType<WindowEntity>().Select(c => c.DBArchEntiy).Cast<TArchWindow>().ToList();
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
                var thisRailingEntitys = new Dictionary<Polyline, ThTCHRailing>();
                var railingColls = new DBObjectCollection();
                foreach (var item in curveEntities)
                {
                    if (item.EntitySystem.Contains("楼板"))
                    {
                        if (item.FloorEntity != null && item.FloorEntity is SlabPolyline slab1)
                        {
                            var slab = CreateSlab(slab1, matrix);
                            slab.Uuid = prjId + item.Id;
                            allSlabs.Add(slab);
                        }
                    }
                    else if (item.EntitySystem.Contains("栏杆"))
                    {
                        if (item.EntityCurve is Polyline polyline)
                        {
                            var pLine = polyline.GetTransformedCopy(matrix) as Polyline;
                            railingColls.Add(pLine);
                            var railing = CreateRailing(pLine);
                            if (railingToRegion)
                            {
                                var centerline = railing.Outline as Polyline;
                                var outlines = centerline.BufferFlatPL(railing.Width / 2.0);
                                railing.Outline = outlines[0] as Polyline;
                            }
                            railing.Height = ralingHeight;
                            railing.Uuid = prjId + item.Id;
                            thisRailingEntitys.Add(pLine, railing);
                        }
                    }
                }
                //用墙的索引找栏杆没有找到
                var railingSpatialIndex = new ThCADCoreNTSSpatialIndex(railingColls);
                List<Polyline> hisPLines = new List<Polyline>();
                foreach (var wall in walls)
                {
                    if (wall.Height < 10 || wall.Height > 2000)
                        continue;
                    if (hisPLines.Count == thisRailingEntitys.Count)
                        break;
                    var crossRailings = railingSpatialIndex.SelectCrossingPolygon(wall.Outline).OfType<Polyline>().ToList();
                    if (crossRailings.Count < 1)
                        continue;
                    foreach (var polyline in crossRailings)
                    {
                        if (hisPLines.Any(c => c == polyline))
                            continue;
                        var railing = thisRailingEntitys[polyline];
                        (railing.Outline as Polyline).Elevation = (wall.Outline as Polyline).Elevation + wall.Height;
                        railing.ZOffSet = wall.Height;
                        railing.Height = 800;
                    }
                }
                floor.FloorEntitys.AddRange(allSlabs);
                floor.FloorEntitys.AddRange(thisRailingEntitys.Select(c => c.Value).ToList());
            }

            var floorData = GetBlockElevtionValue(floorOrigin);
            foreach (var floor in floorData)
            {
                var levelEntitys = floorOrigin.Find(c => c.FloorName == floor.FloorName);
                if (levelEntitys == null)
                    continue;
                var buildingStorey = new ThTCHBuildingStorey();
                buildingStorey.Uuid = prjId + floor.Num.ToString()+"F";
                buildingStorey.Number = floor.Num.ToString();
                buildingStorey.Height = floor.LevelHeight;
                buildingStorey.Elevation = floor.Elevation;
                buildingStorey.Useage = floor.FloorName;
                buildingStorey.Origin = new Point3d(0, 0, floor.Elevation);

                ThTCHBuildingStorey memoryStory = null;
                if (isMemoryStory)
                {
                    foreach (var item in thBuilding.Storeys)
                    {
                        if (!string.IsNullOrEmpty(item.MemoryStoreyId) || item.Useage != floor.FloorName)
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
                        if (Math.Abs(copyItem.Height) < 10)
                            copyItem.Height = floor.LevelHeight;
                        walls.Add(copyItem);
                    }
                    var columns = new List<ThTCHColumn>();
                    foreach (var item in levelEntitys.FloorEntitys.OfType<ThTCHColumn>().ToList())
                    {
                        var copyItem = item.Clone() as ThTCHColumn;
                        if (Math.Abs(copyItem.Height) < 10)
                            copyItem.Height = floor.LevelHeight;
                        columns.Add(copyItem);
                    }
                    var beams = new List<ThTCHBeam>();
                    foreach (var item in levelEntitys.FloorEntitys.OfType<ThTCHBeam>().ToList())
                    {
                        var copyItem = item.Clone() as ThTCHBeam;
                        if (Math.Abs(copyItem.Height) > 10)
                        {
                            (copyItem.Outline as Polyline).Elevation = floor.LevelHeight + item.ZOffSet - item.Height;
                            copyItem.Height = item.Height;
                            copyItem.ZOffSet = floor.LevelHeight + item.ZOffSet - item.Height;
                        }
                        beams.Add(copyItem);
                    }

                    var slabs = levelEntitys.FloorEntitys.OfType<ThTCHSlab>().ToList();
                    buildingStorey.Slabs.AddRange(slabs);
                    var railings = levelEntitys.FloorEntitys.OfType<ThTCHRailing>().ToList();
                    buildingStorey.Railings.AddRange(railings);
                    buildingStorey.Walls.AddRange(walls);
                    buildingStorey.Columns.AddRange(columns);
                    buildingStorey.Beams.AddRange(beams);
                }
                thBuilding.Storeys.Add(buildingStorey);
            }
            //spatialIndex = null;
            //entitySpatialIndex = null;
            //entityBases = null;
            //entityDic = null;
            //cadCurveEntitys = null;
            //cadEntityDic = null;
            //archDBData = null;
            thSite.Building = thBuilding;
            thPrj.Site = thSite;
            return thPrj;
        }
        private void CalcFloorSlab(List<Polyline> slabPolylines,List<DBText> slabTexts,List<MText> slabMTexts) 
        {
            slabPolylines = slabPolylines.OrderBy(c => c.Area).ToList();
            var allSlabs = new List<SlabPolyline>();
            var textColl = new DBObjectCollection();
           
            foreach (var item in slabTexts) 
            {
                textColl.Add(item);
            }
            foreach (var item in slabMTexts)
                textColl.Add(item);
            var slabTextSpIndex = new ThCADCoreNTSSpatialIndex(textColl);
            var hisIds = new List<ObjectId>();
            foreach (var item in slabPolylines)
            {
                var addSlab = new SlabPolyline(item, slabThickness);
                var insertText = slabTextSpIndex.SelectCrossingPolygon(item);
                var insertText1 = slabTextSpIndex.SelectWindowPolygon(item);
                if (insertText.Count < 1)
                {
                    allSlabs.Add(addSlab);
                    continue;
                }
                double height = 0.0;
                foreach (var obj in insertText)
                {
                    if (obj is DBText text)
                    {
                        if (hisIds.Any(c => c == text.Id))
                            continue;
                        hisIds.Add(text.Id);
                        double.TryParse(text.TextString, out height);
                        break;
                    }
                    else if (obj is MText mText) 
                    {
                        if (hisIds.Any(c => c == mText.Id))
                            continue;
                        hisIds.Add(mText.Id);
                        double.TryParse(mText.Contents, out height);
                        break;
                    }
                }
                addSlab.LowerPlateHeight = height;
                allSlabs.Add(addSlab);
            }
            allSlabs = allSlabs.OrderByDescending(c => c.OutPolyline.Area).ToList();

            List<int> hisIndex = new List<int>();
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
                        innerSlab.IsOpening = true;
                    else
                    {
                    }
                    hisIndex.Add(j);
                    slab.InnerSlabOpenings.Add(innerSlab);
                }
            }
            var resSlab = new List<SlabPolyline>();
            for (int i = 0; i < allSlabs.Count; i++)
            {
                if (hisIndex.Any(c => c == i))
                    continue;
                resSlab.Add(allSlabs[i]);
            }
            foreach (var keyValue in resSlab)
            {
                var floorCurveEntity = new FloorCurveEntity((ulong)keyValue.OutPolyline.Handle.Value,keyValue.OutPolyline, "楼板");
                floorCurveEntity.FloorEntity = keyValue;
                cadCurveEntitys.Add(floorCurveEntity);
            }
        }
        private List<FloorBlock> GetFloorBlockPolylines() 
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var slabTexts = new List<DBText>();
                var slabMTexts = new List<MText>();
                var slabPLines = new List<Polyline>();
                var floorBlocks = new List<BlockReference>();
                var originBlocks = new List<BlockReference>();
                foreach (var entity in acdb.ModelSpace.OfType<Entity>())
                {
                    if (entity is Polyline p)
                    {
                        if (p.Layer == "栏杆")
                        {
                            cadCurveEntitys.Add(new FloorCurveEntity((ulong)p.Handle.Value, p, "栏杆"));
                        }
                        else if (p.Layer == "楼板")
                        {
                            slabPLines.Add(p);
                        }
                        else if (p.Layer == "降板")
                        {
                            slabPLines.Add(p);
                        }
                    }
                    else if (entity is DBText dBText)
                    {
                        if (dBText.Layer == "降板")
                        {
                            slabTexts.Add(dBText);
                        }
                    }
                    else if (entity is MText mText)
                    {
                        if (mText.Layer == "降板")
                        {
                            slabMTexts.Add(mText);
                        }
                    }
                    else if (entity is BlockReference block)
                    {
                        var name = ThMEPXRefService.OriginalFromXref(block.GetEffectiveName());
                        if (name.ToLower().StartsWith("thape") && name.EndsWith("inner"))
                        {
                            floorBlocks.Add(block);
                        }
                        else if (name == "BASEPOINT")
                        {
                            originBlocks.Add(block);
                        }
                    }
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
                CalcFloorSlab(slabPLines, slabTexts, slabMTexts);

                return floorOrigins;
            }
        }
        Polyline GetFloorBlockOutLine(BlockReference floor)
        {
            var rect = floor.GeometricExtents;
            var minPt = rect.MinPoint;
            var maxPt = rect.MaxPoint;
            var pt1 = new Point3d(minPt.X, minPt.Y, 0);
            var pt2 = new Point3d(maxPt.X, minPt.Y, 0);
            var pt3 = new Point3d(maxPt.X, maxPt.Y, 0);
            var pt4 = new Point3d(minPt.X, maxPt.Y, 0);
            Polyline outPLine = new Polyline();
            outPLine.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            outPLine.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);
            outPLine.Closed = true;
            return outPLine;
        }
        List<LevelElevation> GetBlockElevtionValue(List<FloorBlock> floorBlocks)
        {
            var res = new List<LevelElevation>();
            if (floorBlocks.Count == 2 && floorBlocks.Any(o => o.FloorName == "1F") && floorBlocks.Any(o => o.FloorName == "构架"))
            {
                res.Add(new LevelElevation { Num = "1", Elevation = -4900, LevelHeight = 4860, FloorName = "1F" });
                res.Add(new LevelElevation { Num = "35", Elevation = 98500, LevelHeight = 4400, FloorName = "构架" });
                return res;
            }
            double firstFloorHeight = 5300;
            double levelHeight = 3150;
            double startElevtion = 0.0;
            foreach (var floor in floorBlocks)
            {
                var name = floor.FloorName;
                var floorCalculator = new FloorCalculator(name);
                var floorStrs = floorCalculator.Floors;
                foreach (var str in floorStrs)
                {
                    int floorNum = 0;
                    string result = System.Text.RegularExpressions.Regex.Replace(str, @"[^0-9]+", "");
                    if (int.TryParse(result, out floorNum))
                    {
                        res.Add(new LevelElevation { Num = floorNum.ToString(), Elevation = 0, LevelHeight = levelHeight, FloorName = name });
                    }
                }
            }
            if (res.Count < 1)
            {
                return res;
            }
            res = res.OrderBy(c => c.Num).ToList();
            res.First().Elevation = startElevtion;
            res.First().LevelHeight = firstFloorHeight;
            var elevtion = startElevtion + firstFloorHeight;
            for (int i = 1; i < res.Count; i++) 
            {
                var level = res[i];
                level.Elevation = elevtion;
                level.LevelHeight = levelHeight;
                elevtion += levelHeight;
            }

            //res.Add(new LevelElevtion { Num = 1, Elevtion = 0, LevelHeight = 5300, FloorName = "BZ1" });
            //res.Add(new LevelElevtion { Num = 2, Elevtion = 5300, LevelHeight = 3150, FloorName = "BZ2" });
            //res.Add(new LevelElevtion { Num = 3, Elevtion = 8450, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 4, Elevtion = 11600, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 5, Elevtion = 14750, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 6, Elevtion = 17900, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 7, Elevtion = 21050, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 8, Elevtion = 24200, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 9, Elevtion = 27350, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 10, Elevtion = 30500, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 11, Elevtion = 33650, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 12, Elevtion = 36800, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 13, Elevtion = 39950, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 14, Elevtion = 43100, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 15, Elevtion = 46250, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 16, Elevtion = 49400, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 17, Elevtion = 52550, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 18, Elevtion = 55700, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 19, Elevtion = 58850, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 20, Elevtion = 62000, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 21, Elevtion = 65150, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 22, Elevtion = 68300, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 23, Elevtion = 71450, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 24, Elevtion = 74600, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 25, Elevtion = 77750, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 26, Elevtion = 80900, LevelHeight = 3150, FloorName = "BZ3" });
            //res.Add(new LevelElevtion { Num = 27, Elevtion = 84050, LevelHeight = 3150, FloorName = "BZ4" });

            return res;
        }
        void InitFloorDBEntity(List<TArchEntity> allTArchEntitys, List<THStructureEntity> allDBEntitys) 
        {
            var addTArchColl = new DBObjectCollection();
            foreach (var item in allTArchEntitys)
            {
                var thEntity = DBToTHEntityCommon.DBArchToTHArch(item, new Vector3d(0, 0, 0));
                if (thEntity == null)
                    continue;
                entityBases.Add(thEntity);
                if (null == thEntity.OutLine)
                    continue;
                addTArchColl.Add(thEntity.OutLine);
                entityDic.Add(thEntity.OutLine, thEntity);
            }
            spatialIndex = new ThCADCoreNTSSpatialIndex(addTArchColl);
            addTArchColl.Clear();
            foreach (var item in cadCurveEntitys) 
            {
                addTArchColl.Add(item.EntityCurve);
                cadEntityDic.Add(item.EntityCurve, item);
            }

            var addDBColl = new DBObjectCollection();
            foreach (var item in allDBEntitys)
            {
                addDBColl.Add(item.Outline);
                dbEntityDic.Add(item.Outline, item);
            }
            entitySpatialIndex = new ThCADCoreNTSSpatialIndex(addTArchColl);
            dbEntitySpatialIndex = new ThCADCoreNTSSpatialIndex(addDBColl);
        }
        List<THArchEntityBase> FloorEntitys(Polyline outPLine,out List<FloorCurveEntity> curveEntities) 
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

        private ThTCHSlab CreateSlab(SlabPolyline slabPolyline, Matrix3d matrix)
        {
            var outPLine = slabPolyline.OutPolyline.GetTransformedCopy(matrix) as Polyline;
            var slab = new ThTCHSlab(outPLine, slabPolyline.Thickness, Vector3d.ZAxis);
            foreach (var item in slabPolyline.InnerSlabOpenings) 
            {
                var innerPLine = item.OutPolyline.GetTransformedCopy(matrix) as Polyline;
                if (!item.IsOpening)
                {
                    slab.Descendings.Add(new ThTCHSlabDescendingData()
                    {
                        Outline = innerPLine,
                        IsDescending = true,
                        DescendingHeight = Math.Abs(item.LowerPlateHeight),
                        DescendingThickness = item.Thickness,
                        DescendingWrapThickness = 50,
                    });
                }
                else 
                {
                    slab.Descendings.Add(new ThTCHSlabDescendingData()
                    {
                        Outline = innerPLine,
                    });
                }
            }
            return slab;
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
            var pl = beam.Outline.Clone() as Polyline;
            pl.TransformBy(matrix);
            //pl.Closed = false;
            pl.Elevation = 0.0;
            var newBeam = new ThTCHBeam(pl, -1);
            newBeam.Uuid = projectId + beam.Uuid;
            newBeam.ZOffSet = beam.RelativeBG;
            newBeam.Height = beam.Height;
            return newBeam;
        }

        ThTCHSlab BeamEntityToTCHSlab (string projectId, THStructureSlab slab, Matrix3d matrix)
        {
            var pl = slab.Outline.Clone() as Polyline;
            pl.TransformBy(matrix);
            //pl.Closed = false;
            pl.Elevation = (4400 + slab.RelativeBG);
            var newSlab = new ThTCHSlab(pl, slab.Height,Vector3d.ZAxis);
            newSlab.Uuid = projectId + slab.Uuid;
            return newSlab;
        }

    }
    class FloorBlock
    {
        public Polyline FloorOutLine { get; }
        public Point3d FloorOrigin { get; }
        public string FloorName { get; }
        public List<object> FloorEntitys { get; }

        public FloorBlock(string floorName,Polyline outLine,Point3d point) 
        {
            FloorName = floorName;
            FloorOutLine = outLine;
            FloorOrigin = point;
            FloorEntitys = new List<object>();
            
        }
    }
    class FloorCurveEntity 
    {
        public ulong Id { get; }
        public Entity EntityCurve { get; }
        public string EntitySystem { get; }
        public object FloorEntity { get; set; }
        public FloorCurveEntity(ulong id,Entity curve,string system) 
        {
            EntityCurve = curve;
            EntitySystem = system;
            Id = id;
        }
    }
    class LevelElevation
    {
        /// <summary>
        /// 楼层编号
        /// </summary>
        public string Num { get; set; }
        /// <summary>
        /// 楼层标高
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// 层高
        /// </summary>
        public double LevelHeight { get; set; }
        /// <summary>
        /// 楼层框名称（图纸中楼层框内的字段）
        /// </summary>
        public string FloorName { get; set; }
    }
    class SlabPolyline 
    {
        public Polyline OutPolyline { get; }
        public bool IsOpening { get; set; }
        public double Thickness { get; set; }
        public double LowerPlateHeight { get; set; }
        public List<SlabPolyline> InnerSlabOpenings { get; }
        public SlabPolyline(Polyline polyline, double thickness) 
        {
            OutPolyline = polyline;
            Thickness = thickness;
            LowerPlateHeight = 0.0;
            InnerSlabOpenings = new List<SlabPolyline>();
            IsOpening = false;
        }
    }
}
