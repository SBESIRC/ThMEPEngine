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
    }
}
