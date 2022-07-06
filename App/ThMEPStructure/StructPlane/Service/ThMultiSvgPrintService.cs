using System.Linq;
using System.Collections.Generic;
using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.StructPlane.Print;
using ThMEPEngineCore.Model;
using NFox.Cad;

namespace ThMEPStructure.StructPlane.Service
{
    internal class ThMultiSvgPrintService
    {
        private Database AcadDb { get; set; }
        private double FloorSpacing { get; set; } = 100000;
        private List<string> SvgFiles { get; set; }
        public ThMultiSvgPrintService(Database database, List<string> svgFiles)
        {
            AcadDb = database;
            SvgFiles = svgFiles;
        }
        public void Print()
        {
            var printers = PrintToCad();
            Layout(printers.Select(o => o.ObjIds).ToList());
            InsertBasePoint();
        }

        private void Layout(List<ObjectIdCollection> floorObjIds)
        {
            using (var acadDb = AcadDatabase.Use(AcadDb))
            {
                for (int i = 0; i < floorObjIds.Count; i++)
                {
                    if (i == 0)
                    {
                        continue;
                    }
                    var dir = new Vector3d(0, i * FloorSpacing, 0);
                    var mt = Matrix3d.Displacement(dir);
                    floorObjIds[i].OfType<ObjectId>().ForEach(o =>
                    {
                        var entity = acadDb.Element<Entity>(o, true);
                        entity.TransformBy(mt);
                    });
                }
            }
        }

        private void InsertBasePoint()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                if (acadDb.Blocks.Contains(ThPrintBlockManager.BasePointBlkName) &&
                    acadDb.Layers.Contains(ThPrintLayerManager.DefpointsLayerName))
                {
                    DbHelper.EnsureLayerOn(ThPrintLayerManager.DefpointsLayerName);
                    acadDb.ModelSpace.ObjectId.InsertBlockReference(
                                       ThPrintLayerManager.DefpointsLayerName,
                                       ThPrintBlockManager.BasePointBlkName,
                                       Point3d.Origin,
                                       new Scale3d(1.0),
                                       0.0);
                }
            }
        }

        private Extents2d ToExtents2d(ObjectIdCollection objIds)
        {
            var extents = new Extents2d();
            double minX = double.MaxValue, minY = double.MaxValue,
                maxX = double.MinValue, maxY = double.MinValue;
            using (var acadDb = AcadDatabase.Use(AcadDb))
            {
                objIds.OfType<ObjectId>().ForEach(o =>
                {
                    var entity = acadDb.Element<Entity>(o);
                    if (entity != null && !entity.IsErased && entity.GeometricExtents != null)
                    {
                        if (entity.GeometricExtents.MinPoint.X < minX)
                        {
                            minX = entity.GeometricExtents.MinPoint.X;
                        }
                        if (entity.GeometricExtents.MinPoint.Y < minY)
                        {
                            minY = entity.GeometricExtents.MinPoint.Y;
                        }
                        if (entity.GeometricExtents.MaxPoint.X > maxX)
                        {
                            maxX = entity.GeometricExtents.MaxPoint.X;
                        }
                        if (entity.GeometricExtents.MaxPoint.Y > maxY)
                        {
                            maxY = entity.GeometricExtents.MaxPoint.Y;
                        }
                    }
                });
                extents = new Extents2d(minX, minY, maxX, maxY);
            }
            return extents;
        }

        private List<ThSvgEntityPrintService> PrintToCad()
        {
            var results = new List<ThSvgEntityPrintService>();
            SvgFiles.ForEach(svgFile =>
            {
                var svg = new ThStructureSVGReader();
                svg.ReadFromFile(svgFile);

                // 处理
                // 对剪力墙造洞
                var buildAreaSevice = new ThWallBuildAreaService();
                var newGeos = buildAreaSevice.BuildArea(svg.Geos);

                // 对梁线要合并
                var beamGeos = newGeos.GetBeamGeos();
                var passGeos = newGeos.Except(beamGeos).ToList();
                var belowObjs = GetBelowObjs(passGeos);
                var newBeamGeos = Handle(beamGeos, belowObjs);
                passGeos.AddRange(newBeamGeos);

                var prinService = new ThSvgEntityPrintService(
                    passGeos, svg.FloorInfos, svg.DocProperties, "1:100");
                prinService.Print(AcadDb);
                results.Add(prinService);
            });
            return results;
        }

        private List<ThGeometry> Handle(List<ThGeometry> beams,DBObjectCollection polygons)
        {
            using (var hander = new ThBeamLineHandler(polygons))
            {
                var results = new List<ThGeometry>();
                var beamGroups = GroupBeam(beams);
                beamGroups.ForEach(g =>
                {
                    var beamLines = g.Select(o => o.Boundary).ToCollection();
                    // 后面如果有弧梁，则不要传入进去
                    var newBeamLines = hander.Handle(beamLines);
                    newBeamLines.OfType<Line>().ForEach(l => results.Add(ThGeometry.Create(l,g.First().Properties)));
                });
                return results;
            }
        }

        private List<List<ThGeometry>> GroupBeam(List<ThGeometry> beamGeos)
        {
            var results = new List<List<ThGeometry>>();
            var groups = beamGeos.GroupBy(o => o.Properties.GetCategory() + o.Properties.GetLineType());
            foreach(var group in groups)
            {
                results.Add(group.ToList());
            }
            return results;
        }

        private DBObjectCollection GetBelowObjs(List<ThGeometry> geos)
        {
            var polygons = new DBObjectCollection();
            var belowColumns = geos.GetBelowColumnGeos();
            var belowShearWalls = geos.GetBelowShearwallGeos();
            belowColumns.ForEach(o => polygons.Add(o.Boundary));
            belowShearWalls.ForEach(o => polygons.Add(o.Boundary));
            return polygons;
        }
    }
}
