using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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

        public static List<ThGeometry> buildCoolPtGeometry(List<ThTerminalToilet> toiletList)
        {
            List<ThGeometry> geom = new List<ThGeometry>();

            toiletList.ForEach(toilet =>
            {
                for (int i = 0; i < toilet.SupplyCoolOnWall.Count(); i++)
                {
                    var pt = toilet.SupplyCoolOnWall[i];

                    if (pt != Point3d.Origin)
                    {
                        var geometry = new ThGeometry();
                        var id = i == 0 ? toilet.Uuid : toilet.Uuid + ThDrainageSDCommon.GJSecPtSuffix;

                        geometry.Properties.Add(ThDrainageSDCommon.ProId, id);
                        geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.WaterSupplyPoint.ToString());
                        geometry.Properties.Add(ThDrainageSDCommon.ProAreaId, toilet.AreaId);
                        geometry.Properties.Add(ThDrainageSDCommon.ProGroupId, toilet.GroupId);
                        geometry.Properties.Add(ThDrainageSDCommon.ProDirection, new double[] { toilet.Dir.X, toilet.Dir.Y });

                        geometry.Boundary = new DBPoint(pt);

                        geom.Add(geometry);
                    }
                }
            }
            );

            return geom;
        }

        public static List<ThGeometry> buildVirtualCoolPtGeomary(List<ThToiletGJson> virtualPtList)
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

        public static List<Line> updateToiletModel(List<ThToiletGJson> tGJList, List<ThTerminalToilet> toiletList)
        {
            List<Line> subLink = new List<Line>();

            foreach (var gj in tGJList)
            {
                var toilet = toiletList.Where(toi => gj.Id.Contains(toi.Uuid)).FirstOrDefault();
                if (toilet != null)
                {
                    //轴测图判断最重点位所属厕所也要用到支线方向，所以不要轻易支线计算的方向 toilet.Dir
                    Point3d subLinkEndPt = gj.Pt + toilet.Dir * ThDrainageSDCommon.LengthSublink;

                    toilet.SupplyCoolOnBranch.Add(subLinkEndPt);
                    toilet.AreaId = gj.AreaId;
                    toilet.GroupId = gj.GroupId;

                    subLink.Add(new Line(gj.Pt, subLinkEndPt));
                }
            }

            return subLink;
        }

        public static List<ThToiletGJson> toVirtualPt(Dictionary<ThTerminalToilet, List<Point3d>> toiletPtDict)
        {
            List<ThToiletGJson> virtualModelList = new List<ThToiletGJson>();

            foreach (var toiletPt in toiletPtDict)
            {
                var toilet = toiletPt.Key;

                for (int i = 0; i < toiletPt.Value.Count; i++)
                {
                    var pt = toiletPt.Value[i];
                    var id = i == 0 ? toilet.Uuid : toilet.Uuid + ThDrainageSDCommon.GJSecPtSuffix;

                    var virtualPt = new ThToiletGJson()
                    {
                        Id = id,
                        Direction = toilet.Dir,
                        Pt = pt,
                        AreaId = toilet.AreaId,
                        GroupId = toilet.GroupId,
                    };

                    virtualModelList.Add(virtualPt);
                }
            }

            return virtualModelList;
        }

        public static List<ThToiletGJson> toVirtualPt(Dictionary<ThTerminalToilet, Point3d> virtualPtDict)
        {
            List<ThToiletGJson> virtualModelList = new List<ThToiletGJson>();

            foreach (var virtualPt in virtualPtDict)
            {
                var toilet = virtualPt.Key;
                var pt = virtualPt.Value;
                var virtualModel = new ThToiletGJson()
                {
                    Id = toilet.Uuid,
                    Direction = toilet.Dir,
                    Pt = pt,
                    AreaId = toilet.AreaId,
                    GroupId = toilet.GroupId,
                };

                virtualModelList.Add(virtualModel);
            }

            return virtualModelList;
        }

    }
}
