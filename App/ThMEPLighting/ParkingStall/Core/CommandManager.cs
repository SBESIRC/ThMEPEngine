using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPLighting.ParkingStall.Model;
using ThMEPLighting.ParkingStall.Business.UserInteraction;
using ThCADExtension;
using ThMEPLighting.ParkingStall.CAD;
using ThMEPLighting.ParkingStall.Assistant;
using ThMEPLighting.ParkingStall.Worker.ParkingGroup;
using ThMEPLighting.ParkingStall.Worker.PlaceLight;
using ThMEPLighting.ParkingStall.Business.Block;

namespace ThMEPLighting.ParkingStall.Core
{
    public class CommandManager
    {
        public void ExtractParkStallProfiles()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);
                DrawUtils.DrawProfileDebug(selectRelatedParkProfiles.Polylines2Curves(), "parkingStall");
            }
        }

        public void GenerateParkGroup()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);

                // 去除内部的车位信息
                var validParkProfiles = GenerateValidParkPolys.MakeValidParkPolylines(selectRelatedParkProfiles, polygonInfo.InnerProfiles);
                // 分组车位信息处理
                var parkingRelatedGroups = ParkingGroupGenerator.MakeParkingGroupGenerator(validParkProfiles);
                DrawUtils.DrawGroup(parkingRelatedGroups);
            }
        }

        public void GenerateGroupLight()
        {
            var wallPolylines = EntityPicker.MakeUserPickPolys();
            if (wallPolylines.Count == 0)
                return;

            var wallPolygonInfos = WallPolygonInfoCalculator.DoWallPolygonInfoCalculator(wallPolylines);
            foreach (var polygonInfo in wallPolygonInfos)
            {
                var wallPtCollection = polygonInfo.ExternalProfile.Vertices();
                var selectRelatedParkProfiles = InfoReader.MakeParkingStallPolys(wallPtCollection);

                // 去除内部的车位信息
                var validParkProfiles = GenerateValidParkPolys.MakeValidParkPolylines(selectRelatedParkProfiles, polygonInfo.InnerProfiles);
                // 分组车位信息处理
                var parkingRelatedGroups = ParkingGroupGenerator.MakeParkingGroupGenerator(validParkProfiles);

                // 车位分组布置灯信息
                var groupLights = ParkingGroupPlaceLightGenerator.MakeParkingPlaceLightGenerator(parkingRelatedGroups);

                ParkLightAngleCalculator.MakeParkLightAngleCalculator(groupLights, Light_Place_Type.LONG_EDGE);
                BlockInsertor.MakeBlockInsert(groupLights);
                //GroupLightViewer.MakeGroupLightViewer(groupLights);
            }
        }
    }
}
