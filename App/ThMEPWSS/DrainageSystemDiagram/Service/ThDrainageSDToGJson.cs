using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using Linq2Acad;
using Dreambuild.AutoCAD;

using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.GeojsonExtractor;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDExchangeThGeom
    {
        public static List<ThGeometry> buildGeometry(List<ThExtractorBase> archiExtractor, List<ThIfcSanitaryTerminalToilate> toilateList, bool onBranch)
        {
            //build THGeometry
            var areaGeom = new List<ThGeometry>();
            archiExtractor.ForEach(o => areaGeom.AddRange(o.BuildGeometries()));
            var areaId = getAreaId(archiExtractor);
            toilateList.ForEach(x => x.AreaId = areaId);

            areaGeom.AddRange(supplyPtToGeo(toilateList, onBranch));

            return areaGeom;
        }


        private static List<ThGeometry> supplyPtToGeo(List<ThIfcSanitaryTerminalToilate> toilateList, bool onBranch)
        {
            List<ThGeometry> geom = new List<ThGeometry>();

            toilateList.ForEach(toilate =>
            {
                if (toilate.SupplyCoolOnWall != Point3d.Origin)
                {
                    var geometry = new ThGeometry();
                    geometry.Properties.Add(DrainageSDCommon.ProId, toilate.Uuid);
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.WaterSupplyPoint.ToString());
                    geometry.Properties.Add(DrainageSDCommon.ProAreaId, toilate.AreaId);
                    geometry.Properties.Add(DrainageSDCommon.ProGroupId, toilate.GroupId);
                   
                    if (onBranch)
                    {
                        geometry.Properties.Add(DrainageSDCommon.ProDirection, new double[] { toilate.Dir.X, toilate.Dir.Y });
                        geometry.Boundary = new DBPoint(toilate.SupplyCoolOnBranch);
                    }
                    else
                    {
                        geometry.Boundary = new DBPoint(toilate.SupplyCoolOnWall);
                    }

                    geom.Add(geometry);
                }

                if (toilate.SupplyCoolSecOnWall != Point3d.Origin)
                {
                    var geometry = new ThGeometry();

                    geometry.Properties.Add(DrainageSDCommon.ProId, toilate.Uuid + DrainageSDCommon.GJSecPtSuffix);
                    geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.WaterSupplyPoint.ToString());
                    geometry.Properties.Add(DrainageSDCommon.ProAreaId, toilate.AreaId);
                    geometry.Properties.Add(DrainageSDCommon.ProGroupId, toilate.GroupId);
                   
                    if (onBranch)
                    {
                        geometry.Properties.Add(DrainageSDCommon.ProDirection, new double[] { toilate.Dir.X, toilate.Dir.Y });
                        geometry.Boundary = new DBPoint(toilate.SupplyCoolSecOnBranch);
                    }
                    else
                    {
                        geometry.Boundary = new DBPoint(toilate.SupplyCoolSecOnWall);
                    }

                    geom.Add(geometry);
                }
            }
            );

            return geom;
        }

        public static string getAreaId(List<ThExtractorBase> archiExtractor)
        {
            string areaId = "";

            foreach (var extractor in archiExtractor)
            {
                if (extractor is ThDrainageSDRegionExtractor region)
                {
                    var pl = region.Region[0].Geometry as Polyline;
                    var areaPolylineToIdDic = region.ToiletGroupId;
                    areaId = areaPolylineToIdDic[pl];
                }
            }

            return areaId;
        }

        public static List<Line> updateToilateModel(List<ThToilateGJson> tGJList, List<ThIfcSanitaryTerminalToilate> toilateList)
        {
            List<Line> subLink = new List<Line>();

            foreach (var gj in tGJList)
            {
                var toilate = toilateList.Where(toilate => gj.Id.Contains(toilate.Uuid)).FirstOrDefault();
                if (toilate != null)
                {
                    //Point3d subLinkEndPt = gj.Pt + gj.Direction * DrainageSDCommon.TolSupplyPtDisplacement;
                    Point3d subLinkEndPt = gj.Pt + toilate.Dir * DrainageSDCommon.TolSupplyPtDisplacement;

                    if (gj.Id.Contains(DrainageSDCommon.GJSecPtSuffix))
                    {
                        toilate.SupplyCoolSecOnBranch = subLinkEndPt;
                        subLink.Add(new Line(toilate.SupplyCoolSecOnWall, toilate.SupplyCoolSecOnBranch));
                    }
                    else
                    {
                        toilate.SupplyCoolOnBranch = subLinkEndPt;
                        subLink.Add(new Line(toilate.SupplyCoolOnWall, toilate.SupplyCoolOnBranch));
                        toilate.Dir = gj.Direction;
                        toilate.AreaId = gj.AreaId;
                        toilate.GroupId = gj.GroupId;
                    }
                }
            }

            return subLink;
        }
    }
}
