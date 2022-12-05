using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Model;
using ThMEPElectrical.DCL.Data;

namespace ThMEPElectrical.DCL.Service
{
    internal class ThLightningProtectLeadWireGeoFactory
    {
        public ThLightningProtectLeadWireGeoFactory()
        {           
        }
        public List<ThGeometry> Work(LightProtectLeadWireStoreyData data)
        {
            var results = new List<ThGeometry>();            
            var storeyGeo = BuildStoreyGeometry(data.StoreyFrameBoundary,data.StoreyId,data.StoreyType,
                data.FloorNumber, data.BasePoint);           
            var specialBeltGeos = BuildSpecialBeltGeometries(data.SpecialBelts, data.StoreyId);
            var dualpurposeBeltGeos = BuildDualpurposeBeltGeometries(data.DualpurposeBelts, data.StoreyId);
            var otherColumnGeos = BuildOtherColumnGeometries(data.OtherColumns, data.StoreyId);
            var otherShearWallGeos = BuildOtherShearWallGeometries(data.OtherShearWalls, data.StoreyId);
            var beamGeos = BuildBeamGeometries(data.Beams, data.StoreyId);

            var archOutlineGeos = new List<ThGeometry>();
            var outerColumnGeos = new List<ThGeometry>();
            var outerShearWallGeos = new List<ThGeometry>();
            data.ArchOutlineAreas.ForEach(area =>
            {
                var shell = area.Shell();                
                var shellId = Guid.NewGuid().ToString();
                var holes = area.Holes();
                var holeIdMap = area.Holes().Select(o => new KeyValuePair<Polyline, string>(o, Guid.NewGuid().ToString())).ToDictionary(x => x.Key, x => x.Value);
                archOutlineGeos.AddRange(BuildArchOutlineGeometries(shell, shellId, holeIdMap, data.StoreyId));

                var columns = data.OuterColumns.ContainsKey(area) ? data.OuterColumns[area].ToCollection() : new DBObjectCollection();
                var shearWalls = data.OuterShearWalls.ContainsKey(area) ? data.OuterShearWalls[area].ToCollection() : new DBObjectCollection();
                outerColumnGeos.AddRange(BuildOuterColumnGeometries(columns, shellId, data.StoreyId));
                outerShearWallGeos.AddRange(BuildOuterShearWallGeometries(shearWalls, shellId, data.StoreyId));
            });      

            results.Add(storeyGeo); // 楼层
            results.AddRange(archOutlineGeos); // 楼层            
            results.AddRange(outerColumnGeos);
            results.AddRange(otherColumnGeos);
            results.AddRange(outerShearWallGeos);
            results.AddRange(otherShearWallGeos);
            results.AddRange(beamGeos);
            results.AddRange(specialBeltGeos);
            results.AddRange(dualpurposeBeltGeos);

            return results;
        }
        #region ---------- Create Geometries -----------
        private ThGeometry BuildStoreyGeometry(Entity boundary,string storeyId,
            string storeyType,string floorNumber,string basePoint,string name= "楼层框线")
        {
            var geometry = new ThGeometry();
            string category = BuiltInCategory.StoreyBorder.ToString();
            geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, storeyId);
            geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
            geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, name);
            geometry.Properties.Add(ThExtractorPropertyNameManager.FloorTypePropertyName, storeyType);
            geometry.Properties.Add(ThExtractorPropertyNameManager.FloorNumberPropertyName, floorNumber);
            geometry.Properties.Add(ThExtractorPropertyNameManager.BasePointPropertyName, basePoint);
            geometry.Boundary = boundary;
            return geometry;
        }

        private List<ThGeometry> BuildArchOutlineGeometries(Polyline shell,string shellId,Dictionary<Polyline,string> holeIdMap, string storeyId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.ArchitectureOutline.ToString();
            var shellGeometry = new ThGeometry();
            shellGeometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, shellId);
            shellGeometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, "");
            shellGeometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
            shellGeometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "建筑轮廓");
            shellGeometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, storeyId);
            shellGeometry.Boundary = shell;
            geos.Add(shellGeometry);

            holeIdMap.ForEach(hole =>
            {
                var holeGeometry = new ThGeometry();
                holeGeometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, hole.Value);
                holeGeometry.Properties.Add(ThExtractorPropertyNameManager.ParentIdPropertyName, shellId);
                holeGeometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                holeGeometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "建筑轮廓");
                holeGeometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, storeyId);
                holeGeometry.Boundary = hole.Key;
                geos.Add(holeGeometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildSpecialBeltGeometries(List<Curve> specialBelts, string storeyId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.LightningReceivingBelt.ToString();
            specialBelts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, Guid.NewGuid().ToString());
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "专设接闪带");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, storeyId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildDualpurposeBeltGeometries(List<Curve> dualpurposeBelts, string storeyId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.LightningReceivingBelt.ToString();
            dualpurposeBelts.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.IdPropertyName, Guid.NewGuid().ToString());
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "兼用接闪带");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, storeyId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildOuterColumnGeometries(DBObjectCollection outerColumns,string belongArchOutlineId,string groupId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.Column.ToString();
            outerColumns.OfType<Entity>().ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "外圈柱");        
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, groupId);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BelongedArchOutlineIdPropertyName, belongArchOutlineId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildOtherColumnGeometries(DBObjectCollection otherColumns,string groupId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.Column.ToString();
            otherColumns.OfType<Entity>().ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "其他柱");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, groupId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildOuterShearWallGeometries(DBObjectCollection outerShearWalls, string belongArchOutlineId, string groupId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.ShearWall.ToString();
            outerShearWalls.OfType<Entity>().ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "外圈剪力墙");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, groupId);
                geometry.Properties.Add(ThExtractorPropertyNameManager.BelongedArchOutlineIdPropertyName, belongArchOutlineId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private List<ThGeometry> BuildOtherShearWallGeometries(DBObjectCollection otherShearWalls,string groupId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.ShearWall.ToString();
            otherShearWalls.OfType<Entity>().ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "其他剪力墙");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, groupId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        private List<ThGeometry> BuildBeamGeometries(DBObjectCollection beams, string groupId)
        {
            var geos = new List<ThGeometry>();
            string category = BuiltInCategory.Beam.ToString();
            beams.OfType<Entity>().ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, category);
                geometry.Properties.Add(ThExtractorPropertyNameManager.NamePropertyName, "梁");
                geometry.Properties.Add(ThExtractorPropertyNameManager.GroupIdPropertyName, groupId);
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }
        #endregion
    }
}
