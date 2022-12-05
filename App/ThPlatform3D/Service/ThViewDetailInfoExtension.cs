using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThPlatform3D.Model;
using ThPlatform3D.Model.MysqlModel;

namespace ThPlatform3D.Service
{
    public static class ThViewDetailInfoExtension
    {
        public static ThProjectFile ToThProjectFile(this ProjectFile projectFile)
        {
            return new ThProjectFile()
            {
                ProjectFileId = projectFile.ProjectFileId,
                PrjId = projectFile.PrjId,
                SubPrjId = projectFile.SubPrjId,
                FileName = projectFile.FileName,
                MajorName = projectFile.MajorName,
                IsDel = projectFile.IsDel.HasValue ? projectFile.IsDel.Value ==1 : false,
            };
        }
        public static PlaneViewInfo ToPlaneViewInfoInMySql(this ThViewDetailInfo planeViewInfo)
        {
            return new PlaneViewInfo()
            {
                Id = planeViewInfo.Id,
                FileName = planeViewInfo.FileName,
                BuildingNo = planeViewInfo.BuildingNo,
                ViewName=planeViewInfo.ViewName,
                ViewType=planeViewInfo.ViewType,
                ViewScale=planeViewInfo.ViewScale,
                StructureMajorData = BoolToByte(planeViewInfo.StructureMajorData),
                PCMajorData = BoolToByte(planeViewInfo.PCMajorData),
                ViewTemplate=planeViewInfo.ViewTemplate,
                Floor = planeViewInfo.Floor,
                SectionElevation=planeViewInfo.SectionElevation,
                BottomElevation=planeViewInfo.BottomElevation,
                TopElevation=planeViewInfo.TopElevation,
                ProjectFileId=planeViewInfo.ProjectFileId,
                ViewState=planeViewInfo.ViewState,
                Direction=planeViewInfo.Direction,
            };
        }
        public static ElevationViewInfo ToElevationViewInfoInMySql(this ThViewDetailInfo elevationViewInfo)
        {
            return new ElevationViewInfo()
            { 
                Id = elevationViewInfo.Id,
                FileName = elevationViewInfo.FileName,
                BuildingNo = elevationViewInfo.BuildingNo,
                ViewName=elevationViewInfo.ViewName,
                ViewType=elevationViewInfo.ViewType,
                ViewScale=elevationViewInfo.ViewScale,
                StructureMajorData= BoolToByte(elevationViewInfo.StructureMajorData),
                PCMajorData = BoolToByte(elevationViewInfo.PCMajorData),
                ViewTemplate  = elevationViewInfo.ViewTemplate,
                OutDoorFloorElevation = elevationViewInfo.OutDoorFloorElevation,
                ProjectFileId=elevationViewInfo.ProjectFileId,
                ViewState=elevationViewInfo.ViewState,
                Direction=elevationViewInfo.Direction,
            };
        }       
        public static SectionViewInfo ToSectionViewInfoInMySql(this ThViewDetailInfo sectionViewInfo)
        {
            return new SectionViewInfo()
            {
                Id = sectionViewInfo.Id,
                FileName = sectionViewInfo.FileName,
                BuildingNo = sectionViewInfo.BuildingNo,
                ViewName = sectionViewInfo.ViewName,
                ViewType = sectionViewInfo.ViewType,
                ViewScale = sectionViewInfo.ViewScale,
                StructureMajorData = BoolToByte(sectionViewInfo.StructureMajorData),
                PCMajorData = BoolToByte(sectionViewInfo.PCMajorData),
                ViewTemplate = sectionViewInfo.ViewTemplate,
                OutDoorFloorElevation = sectionViewInfo.OutDoorFloorElevation,
                SectionFrame = String.Join(";", sectionViewInfo.SectionFrames.ToArray()),
                UseFloorSection = BoolToByte(sectionViewInfo.UseFloorSection),
                SectionDistance = sectionViewInfo.SectionDistance,
                ProjectFileId = sectionViewInfo.ProjectFileId,
                ViewState= sectionViewInfo.ViewState,
                Direction= sectionViewInfo.Direction,
            };
        }
        public static ThViewDetailInfo ToViewDetailInfo(this PlaneViewInfo planeViewInfo)
        {
            return new ThViewDetailInfo()
            {
                Id = planeViewInfo.Id,
                FileName = planeViewInfo.FileName,
                BuildingNo = planeViewInfo.BuildingNo,
                ViewName = planeViewInfo.ViewName,
                ViewType = planeViewInfo.ViewType,
                ViewScale = planeViewInfo.ViewScale,
                StructureMajorData = ByteToBool(planeViewInfo.StructureMajorData),
                PCMajorData = ByteToBool(planeViewInfo.PCMajorData),
                ViewTemplate = planeViewInfo.ViewTemplate,
                Floor = planeViewInfo.Floor,
                SectionElevation = ToDouble(planeViewInfo.SectionElevation),
                BottomElevation = ToDouble(planeViewInfo.BottomElevation),
                TopElevation = ToDouble(planeViewInfo.TopElevation),
                ProjectFileId = planeViewInfo.ProjectFileId,
                ViewState = planeViewInfo.ViewState,
                Direction = planeViewInfo.Direction,
            };
        }
        public static ThViewDetailInfo ToViewDetailInfo(this ElevationViewInfo elevationViewInfo)
        {
            return new ThViewDetailInfo()
            {
                Id = elevationViewInfo.Id,
                FileName = elevationViewInfo.FileName,
                BuildingNo = elevationViewInfo.BuildingNo,
                ViewName = elevationViewInfo.ViewName,
                ViewType = elevationViewInfo.ViewType,
                ViewScale = elevationViewInfo.ViewScale,
                StructureMajorData = ByteToBool(elevationViewInfo.StructureMajorData),
                PCMajorData = ByteToBool(elevationViewInfo.PCMajorData),
                ViewTemplate = elevationViewInfo.ViewTemplate,
                OutDoorFloorElevation = ToDouble(elevationViewInfo.OutDoorFloorElevation),
                ProjectFileId = elevationViewInfo.ProjectFileId,
                ViewState = elevationViewInfo.ViewState,
                Direction = elevationViewInfo.Direction,
            };
        }
        public static ThViewDetailInfo ToViewDetailInfo(this SectionViewInfo sectionViewInfo)
        {
            var result = new ThViewDetailInfo()
            {
                Id = sectionViewInfo.Id,
                FileName = sectionViewInfo.FileName,
                BuildingNo = sectionViewInfo.BuildingNo,
                ViewName = sectionViewInfo.ViewName,
                ViewType = sectionViewInfo.ViewType,
                ViewScale = sectionViewInfo.ViewScale,
                StructureMajorData = ByteToBool(sectionViewInfo.StructureMajorData),
                PCMajorData = ByteToBool(sectionViewInfo.PCMajorData),
                ViewTemplate = sectionViewInfo.ViewTemplate,
                OutDoorFloorElevation = ToDouble(sectionViewInfo.OutDoorFloorElevation),                
                UseFloorSection = ByteToBool(sectionViewInfo.UseFloorSection),
                SectionDistance = ToDouble(sectionViewInfo.SectionDistance),
                ProjectFileId = sectionViewInfo.ProjectFileId,
                ViewState = sectionViewInfo.ViewState,
                Direction = sectionViewInfo.Direction,
            };
            foreach(string item in sectionViewInfo.SectionFrame.Split(';'))
            {
                result.SectionFrames.Add(item);
            }
            return result;
        }

        private static double ToDouble(double? value)
        {
            return value.HasValue ? value.Value : 0.0;
        }

        private static byte? BoolToByte(bool boolean)
        {
            return BitConverter.GetBytes(boolean)[0];
        }

        private static bool ByteToBool(byte? value)
        {
            return value != null ? BitConverter.ToBoolean(new byte[] { (byte)value }, 0) : false;
        }
    }
}
