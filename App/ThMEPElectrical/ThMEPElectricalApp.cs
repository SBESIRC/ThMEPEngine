using AcHelper;
using Linq2Acad;
using ThCADExtension;
using GeometryExtensions;
using ThMEPElectrical.CAD;
using ThMEPElectrical.Core;
using ThMEPElectrical.Model;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical
{
    public class ThMEPElectricalApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
            ThMPolygonTool.Initialize();
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THMainBeamRegion", CommandFlags.Modal)]
        public void ThBeamRegion()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                var polys = packageManager.DoMainBeamProfiles();
                //DrawUtils.DrawProfile(polys.Polylines2Curves(), "MainBeamProfiles");
            }
        }

        private PlaceParameter Parameter
        {
            get
            {
                if (ThMEPElectricalService.Instance.Parameter == null)
                {
                    ThMEPElectricalService.Instance.Parameter = new PlaceParameter();
                }
                return ThMEPElectricalService.Instance.Parameter;
            }
        }

        [CommandMethod("TIANHUACAD", "THABBPlace", CommandFlags.Modal)]
        public void ThProfilesPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBMultiPlace", CommandFlags.Modal)]
        public void ThMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMultiWallMainBeamPlace();
            }
        }

        [CommandMethod("TIANHUACAD", "THOBBRect", CommandFlags.Modal)]
        public void ThProfilesRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainBeamRect();
            }
        }

        [CommandMethod("TIANHUACAD", "THABBRect", CommandFlags.Modal)]
        public void ThABBRect()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainBeamABBRect();
            }
        }

        // 轴网
        [CommandMethod("TIANHUACAD", "THZW", CommandFlags.Modal)]
        public void THDoGridTestProfiles()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoGridTestProfilesWithUcs();
            }
        }

        // 梁吊顶
        [CommandMethod("TIANHUACAD", "THFDCP", CommandFlags.Modal)]
        public void THNoBeamMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoGridBeamPlacePointsWithUcs();
            }
        }

        // 楼层
        [CommandMethod("TIANHUACAD", "THFDFS", CommandFlags.Modal)]
        public void THNoBeamStorey()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoNoBeamPlacePointsWithUcs();
            }
        }

        [CommandMethod("TIANHUACAD", "THFDLBack", CommandFlags.Modal)]
        public void THMSABBMultiPlace()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainSecondBeamPlacePoints();
            }
        }

        [CommandMethod("TIANHUACAD", "THFDL", CommandFlags.Modal)]
        public void THMSABBMultiPlaceUcs()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoMainSecondBeamPlacePointsWithUcs();
            }
        }

        [CommandMethod("TIANHUACAD", "THYGMQ", CommandFlags.Modal)]
        public void THYGMQ()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var packageManager = new PackageManager(Parameter);
                packageManager.DoBlindAreaReminder();
            }
        }

        [CommandMethod("TIANHUACAD", "THUCSCOMPASS", CommandFlags.Modal)]
        public void ThUcsCompass()
        {
            while (true)
            {
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    Active.Database.ImportCompassBlock(
                        ThMEPCommon.UCS_COMPASS_BLOCK_NAME,
                        ThMEPCommon.UCS_COMPASS_LAYER_NAME);
                    var objId = Active.Database.InsertCompassBlock(
                        ThMEPCommon.UCS_COMPASS_BLOCK_NAME,
                        ThMEPCommon.UCS_COMPASS_LAYER_NAME,
                        null);
                    var ucs2Wcs = Active.Editor.UCS2WCS();
                    var compass = acadDatabase.Element<BlockReference>(objId, true);
                    compass.TransformBy(ucs2Wcs);
                    var jig = new ThCompassDrawJig(Point3d.Origin.TransformBy(ucs2Wcs));
                    jig.AddEntity(compass);
                    PromptResult pr = Active.Editor.Drag(jig);
                    if (pr.Status != PromptStatus.OK)
                    {
                        compass.Erase();
                        break;
                    }
                    jig.TransformEntities();
                }
            }
        }
    }
}
