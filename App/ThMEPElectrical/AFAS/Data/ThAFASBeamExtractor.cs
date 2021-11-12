using System;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.GeojsonExtractor.Model;
using ThMEPEngineCore.GeojsonExtractor.Service;
using ThMEPEngineCore.GeojsonExtractor.Interface;
using ThMEPElectrical.AFAS.Service;
using ThMEPElectrical.AFAS.Interface;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASBeamExtractor : ThExtractorBase, IPrint, IGroup, ISetStorey, ITransformer
    {
        public List<ThIfcBeam> Beams { get; private set; }
        private const string SwitchPropertyName = "Switch";
        private const string UsePropertyName = "Use";
        private const string DistanceToFlorPropertyName = "BottomDistanceToFloor";
        private List<ThStoreyInfo> StoreyInfos { get; set; }
        public List<ThRawIfcBuildingElementData> Db3ExtractResults { get; set; }
        public ThMEPOriginTransformer Transformer { get => transformer; set => transformer = value; }

        public ThAFASBeamExtractor()
        {
            Beams = new List<ThIfcBeam>();
            Category = BuiltInCategory.Beam.ToString();
            ElementLayer = "AI-梁";
            UseDb3Engine = true;
            StoreyInfos = new List<ThStoreyInfo>();
            Db3ExtractResults = new List<ThRawIfcBuildingElementData>();
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
            Db3ExtractResults.ForEach(o => beams.Add(ThIfcLineBeam.Create(o.Data as ThIfcBeamAnnotation)));
            beams.ForEach(o => transformer.Transform(o.Outline));
            var newPts = Transformer.Transform(pts);
            if (newPts.Count > 0)
            {
                var beamSpatialIndex = new ThCADCoreNTSSpatialIndex(
                    beams.Select(o => o.Outline).ToCollection());
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
            return instance.Polys
                .Where(o => o.Area >= SmallAreaTolerance)
                .Select(o => ThIfcLineBeam.Create(o))
                .Cast<ThIfcBeam>()
                .ToList();
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
            return Beams.Select(o => o.Outline).ToList();
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
