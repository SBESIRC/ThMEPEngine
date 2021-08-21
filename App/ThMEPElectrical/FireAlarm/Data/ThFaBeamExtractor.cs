using System;
using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using System.Linq;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPElectrical.FireAlarm.Service;
using NFox.Cad;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPElectrical.FireAlarm.Interface;
using ThMEPEngineCore.Algorithm;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;

namespace FireAlarm.Data
{
    class ThFaBeamExtractor : ThExtractorBase, IPrint, IGroup, ISetStorey,ITransformer
    {
        public List<ThIfcBeam> Beams { get; private set; }
        private const string SwitchPropertyName = "Switch";
        private const string UsePropertyName = "Use";
        private const string DistanceToFlorPropertyName = "BottomDistanceToFloor";
        private List<ThStoreyInfo> StoreyInfos { get; set; }

        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThFaBeamExtractor()
        {
            Beams = new List<ThIfcBeam>();
            Category = BuiltInCategory.Beam.ToString();
            ElementLayer = "AI-梁";
            UseDb3Engine = true;
            StoreyInfos = new List<ThStoreyInfo>();
        }
        public override List<ThGeometry> BuildGeometries()
        {
            var geos = new List<ThGeometry>();
            Beams.ForEach(o =>
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
                geometry.Properties.Add(DistanceToFlorPropertyName, GetDistancd(o.DistanceToFloor));
                geometry.Boundary = o.Outline;
                geos.Add(geometry);
            });

            return geos;
        }

        public override void Extract(Database database, Point3dCollection pts)
        {
            //优先保留DB的
            //保留用户绘制的没有产生冲突的
            //如果梁识别引擎增加了MakeValid,Normalize的动作，对于很远的梁识别有问题
            var db3Beams = ExtractDb3Beam(database, pts);
            var localBeams = ExtractMsBeam(database, pts);

            //对Clean的结果进一步过虑
            for (int i = 0; i < localBeams.Count; i++)
            {
                localBeams[i].Outline = ThCleanEntityService.Buffer(localBeams[i].Outline as Polyline, 25);
            }

            //处理重叠
            var conflictService = new ThHandleConflictService(
                db3Beams.Select(o => o.Outline).ToList(),
                localBeams.Select(o => o.Outline).ToList());
            conflictService.Handle();

            Beams.AddRange(db3Beams.Where(o => conflictService.Results.Contains(o.Outline)).ToList());
            var originBeamEntites = Beams.Select(o => o.Outline).ToList();
            Beams.AddRange(conflictService.Results
                .Where(o => !originBeamEntites.Contains(o))
                .Select(o => ThIfcLineBeam.Create(o as Polyline))
                .ToList());
            var objs = Beams.Select(o => o.Outline).ToCollection().FilterSmallArea(SmallAreaTolerance);
            Beams = Beams.Where(o => objs.Contains(o.Outline)).ToList();
        }
        private List<ThIfcBeam> ExtractDb3Beam(Database database, Point3dCollection pts)
        {
            var beams = new List<ThIfcBeam>();
            var engine = new ThDB3BeamExtractionEngine();
            engine.Extract(database);
            engine.Results.ForEach(o => beams.Add(ThIfcLineBeam.Create(o.Data as ThIfcBeamAnnotation)));
            beams.ForEach(o => transformer.Transform(o.Outline));
            if (pts.Count > 0)
            {
                var newPts = new Point3dCollection();
                pts.Cast<Point3d>().ForEach(o =>
                {
                    var pt = new Point3d(o.X, o.Y, o.Z);
                    transformer.Transform(ref pt);
                    newPts.Add(pt);
                });
                var beamSpatialIndex = new ThCADCoreNTSSpatialIndex(beams.Select(o=>o.Outline).ToCollection());
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolyline(newPts);
                var queryObjs = beamSpatialIndex.SelectCrossingPolygon(pline);
                beams = beams.Where(o => queryObjs.Contains(o.Outline)).ToList();
            }
            return beams;
        }
        private List<ThIfcBeam> ExtractMsBeam(Database database, Point3dCollection pts)
        {
            var localBeams = new List<ThIfcBeam>();
            var instance = new ThExtractPolylineService()
            {
                ElementLayer = this.ElementLayer,
            };
            instance.Extract(database, pts);
            instance.Polys.ForEach(o => Transformer.Transform(o));
            localBeams = instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => ThIfcLineBeam.Create(o))
                .Cast<ThIfcBeam>()
                .ToList();

            return localBeams;
        }
        public void Group(Dictionary<Entity, string> groupId)
        {
            Beams.ForEach(o => GroupOwner.Add(o.Outline, FindCurveGroupIds(groupId, o.Outline)));
        }

        public void Print(Database database)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var beamIds = new ObjectIdList();
                Beams.ForEach(o =>
                {
                    o.Outline.ColorIndex = ColorIndex;
                    o.Outline.SetDatabaseDefaults();
                    beamIds.Add(db.ModelSpace.Add(o.Outline));
                });
                if (beamIds.Count > 0)
                {
                    GroupTools.CreateGroup(db.Database, Guid.NewGuid().ToString(), beamIds);
                }
            }
        }

        public void Set(List<ThStoreyInfo> storeyInfos)
        {
            StoreyInfos = storeyInfos;
        }

        public ThStoreyInfo Query(Entity entity)
        {
            var results = StoreyInfos.Where(o => o.Boundary.IsContains(entity));
            return results.Count() > 0 ? results.First() : new ThStoreyInfo();
        }

        public override List<Entity> GetEntities()
        {
            return Beams.Select(o=>o.Outline).ToList();
        }
        private double GetDistancd(double distance)
        {
            if (distance > 0)
                return 0;
            else
                return -distance;
        }

        public void Transform()
        {
            Beams.ForEach(o => Transformer.Transform(o.Outline));
        }

        public void Reset()
        {
            Beams.ForEach(o => Transformer.Reset(o.Outline));
        }
    }
}
