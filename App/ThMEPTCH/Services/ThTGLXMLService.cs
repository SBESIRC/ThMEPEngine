using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;
using ThMEPTCH.Data.IO;
using ThMEPTCH.Model;
using ThMEPTCH.TCHXmlDataConvert;
using ThMEPTCH.TCHXmlModels;
using ThMEPTCH.TCHXmlModels.TCHEntityModels;

namespace ThMEPTCH.Services
{
    public class ThTGLXMLService
    {
        public ThTCHProject LoadXML(string xmlPath)
        {
            ///step1 解析xml
            var fact = new XmlSerializerFactory<Tangent_obj_data>();
            var tchXmlRootModel = fact.Load(xmlPath);
            if (null == tchXmlRootModel
                || tchXmlRootModel.Floors == null
                || tchXmlRootModel.Floors.Count < 1
                || tchXmlRootModel.FloorEntitys == null
                ||tchXmlRootModel.FloorEntitys.Count<1)
                return null;
            //step2 转换中间模型
            //暂时没有考虑多楼栋,后续字段数据慢慢补齐
            //暂时还没有考虑弧形和异形的问题
            var thPrj = new ThTCHProject();
            thPrj.ProjectName = tchXmlRootModel.project_Name.name;
            var thSite = new ThTCHSite();
            var thBuilding = new ThTCHBuilding();
            var dataConvertService = new TCHXmlDataConvertService();
            foreach (var floor in tchXmlRootModel.Floors) 
            {
                var buildingStorey = new ThTCHBuildingStorey();
                buildingStorey.Number = floor.Floor_Num.value;
                buildingStorey.Height = floor.Floor_Height.GetDoubleValue();
                buildingStorey.Elevation = floor.Floor_Elevation.GetDoubleValue();
                buildingStorey.Origin = new Point3d(0, 0, buildingStorey.Elevation);

                var floorEntitys = tchXmlRootModel.FloorEntitys.Where(c => c.Collection_Index.GetIntValue() == floor.Entities_Index.GetIntValue()).FirstOrDefault();
                if (null != floorEntitys && floorEntitys.contents != null)
                {
                    var allTCHXmlEntitys = new List<TCHXmlEntity>();
                    foreach (var item in floorEntitys.contents)
                    {
                        if (null != item.TCH_WALLs)
                        {
                            foreach (var entity in item.TCH_WALLs)
                            {
                                if (null == entity)
                                    continue;
                                allTCHXmlEntitys.Add(entity);
                            }
                        }
                        if (null != item.TCH_OPENINGs)
                        {
                            foreach (var entity in item.TCH_OPENINGs)
                            {
                                if (null == entity)
                                    continue;
                                allTCHXmlEntitys.Add(entity);
                            }
                        }
                        if (null != item.TCH_SLabs) 
                        {
                            foreach (var entity in item.TCH_SLabs)
                            {
                                if (null == entity)
                                    continue;
                                allTCHXmlEntitys.Add(entity);
                            }
                        }
                    }

                    var resData = dataConvertService.StartConvert(allTCHXmlEntitys);
                    if (resData != null && resData.Count > 0)
                    {
                        var thWalls = resData.OfType<ThTCHWall>().ToList();
                        buildingStorey.Walls.AddRange(thWalls);
                        var thDoors = resData.OfType<ThTCHDoor>().ToList();
                        buildingStorey.Doors.AddRange(thDoors);
                        var thWindows = resData.OfType<ThTCHWindow>().ToList();
                        buildingStorey.Windows.AddRange(thWindows);
                        //暂时不从TGL中拿楼板信息
                        //var slabs = resData.OfType<ThTCHSlab>().ToList();
                        //buildingStorey.Slabs.AddRange(slabs);
                    }
                }
                thBuilding.Storeys.Add(buildingStorey);
            }
            thSite.Building = thBuilding;
            thPrj.Site = thSite;
            return thPrj;
        }
    }
}
