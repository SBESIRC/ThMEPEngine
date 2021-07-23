using System;
using System.Collections.Generic;
using System.Linq;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDToGJsonService
    {
        public static List<ThGeometry> buildArchiGeometry(List<ThExtractorBase> archiExtractor)
        {
            var areaGeom = new List<ThGeometry>();
            archiExtractor.ForEach(o => areaGeom.AddRange(o.BuildGeometries()));

            return areaGeom;
        }

        public static List<ThGeometry> buildCoolPtGeometry(List<ThTerminalToilate> toilateList)
        {
            List<ThGeometry> geom = new List<ThGeometry>();

            toilateList.ForEach(toilate =>
            {
                for (int i = 0; i < toilate.SupplyCoolOnWall.Count(); i++)
                {
                    var pt = toilate.SupplyCoolOnWall[i];

                    if (pt != Point3d.Origin)
                    {
                        var geometry = new ThGeometry();
                        var id = i == 0 ? toilate.Uuid : toilate.Uuid + ThDrainageSDCommon.GJSecPtSuffix;

                        geometry.Properties.Add(ThDrainageSDCommon.ProId, id);
                        geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.WaterSupplyPoint.ToString());
                        geometry.Properties.Add(ThDrainageSDCommon.ProAreaId, toilate.AreaId);
                        geometry.Properties.Add(ThDrainageSDCommon.ProGroupId, toilate.GroupId);
                        geometry.Properties.Add(ThDrainageSDCommon.ProDirection, new double[] { toilate.Dir.X, toilate.Dir.Y });

                        geometry.Boundary = new DBPoint(pt);

                        geom.Add(geometry);
                    }
                }
            }
            );

            return geom;
        }

        public static List<ThGeometry> buildVirtualCoolPtGeomary(List<ThToilateGJson> virtualPtList)
        {
            List<ThGeometry> geom = new List<ThGeometry>();

            virtualPtList.ForEach(pt =>
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(ThDrainageSDCommon.ProId, pt.Id);
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.WaterSupplyPoint.ToString());
                    geometry.Properties.Add(ThDrainageSDCommon.ProAreaId, pt.AreaId);
                    geometry.Properties.Add(ThDrainageSDCommon.ProGroupId, pt.GroupId);
                    geometry.Properties.Add(ThDrainageSDCommon.ProDirection, new double[] { pt.Direction.X, pt.Direction.Y });
                    geometry.Boundary = new DBPoint(pt.Pt);

                    geom.Add(geometry);
                });

            return geom;
        }

        public static List<ThGeometry> buildVirtualColumn(List<Polyline> virtualColumn, string areaId)
        {
            List<ThGeometry> columnGeom = new List<ThGeometry>();

            virtualColumn.ForEach(vcl =>
              {
                  var geometry = new ThGeometry();

                  geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Column.ToString());
                  geometry.Properties.Add(ThDrainageSDCommon.ProAreaId, areaId);

                  geometry.Properties.Add(ThExtractorPropertyNameManager.IsolatePropertyName, false);
                  geometry.Boundary = vcl;

                  columnGeom.Add(geometry);
              });

            return columnGeom;
        }

        //public static string getAreaId(List<ThExtractorBase> archiExtractor)
        //{
        //    string areaId = "";

        //    var extractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageSDRegionExtractor)) as ThDrainageSDRegionExtractor;

        //    if (extractor != null && extractor.Region.Count > 0)
        //    {
        //        var pl = extractor.Region[0].Geometry as Polyline;
        //        var areaPolylineToIdDic = extractor.ToiletGroupId;
        //        areaId = areaPolylineToIdDic[pl];
        //    }

        //    return areaId;
        //}

        public static List<Line> updateToilateModel(List<ThToilateGJson> tGJList, List<ThTerminalToilate> toilateList)
        {
            List<Line> subLink = new List<Line>();

            foreach (var gj in tGJList)
            {
                var toilate = toilateList.Where(toilate => gj.Id.Contains(toilate.Uuid)).FirstOrDefault();
                if (toilate != null)
                {
                    //Point3d subLinkEndPt = gj.Pt + gj.Direction * DrainageSDCommon.SublinkLength;

                    Point3d subLinkEndPt = gj.Pt + toilate.Dir * ThDrainageSDCommon.LengthSublink;

                    toilate.SupplyCoolOnBranch.Add(subLinkEndPt);
                    //toilate.Dir = gj.Direction;
                    toilate.AreaId = gj.AreaId;
                    toilate.GroupId = gj.GroupId;

                    subLink.Add(new Line(gj.Pt, subLinkEndPt));
                }
            }

            return subLink;
        }

        public static List<ThToilateGJson> toVirtualPt(Dictionary<ThTerminalToilate, List<Point3d>> toilatePtDict)
        {
            List<ThToilateGJson> virtualModelList = new List<ThToilateGJson>();

            foreach (var toilatePt in toilatePtDict)
            {
                var toilate = toilatePt.Key;

                for (int i = 0; i < toilatePt.Value.Count; i++)
                {
                    var pt = toilatePt.Value[i];
                    var id = i == 0 ? toilate.Uuid : toilate.Uuid + ThDrainageSDCommon.GJSecPtSuffix;

                    var virtualPt = new ThToilateGJson()
                    {
                        Id = id,
                        Direction = toilate.Dir,
                        Pt = pt,
                        AreaId = toilate.AreaId,
                        GroupId = toilate.GroupId,
                    };

                    virtualModelList.Add(virtualPt);
                }
            }

            return virtualModelList;
        }

        public static List<ThToilateGJson> toVirtualPt(Dictionary<ThTerminalToilate, Point3d> virtualPtDict)
        {
            List<ThToilateGJson> virtualModelList = new List<ThToilateGJson>();

            foreach (var virtualPt in virtualPtDict)
            {
                var toilate = virtualPt.Key;
                var pt = virtualPt.Value;
                var virtualModel = new ThToilateGJson()
                {
                    Id = toilate.Uuid,
                    Direction = toilate.Dir,
                    Pt = pt,
                    AreaId = toilate.AreaId,
                    GroupId = toilate.GroupId,
                };

                virtualModelList.Add(virtualModel);

            }


            return virtualModelList;
        }

    }
}
