using Linq2Acad;
using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.SystemDiagram.Model
{
    /// <summary>
    /// 自动火灾报警系统Model
    /// </summary>
    public abstract class ThAutoFireAlarmSystemModel
    {
        public List<ThFloorModel> floors { get; set; }

        //设置全局数据
        public abstract void SetGlobalData(Database database, Dictionary<Entity, List<KeyValuePair<string, string>>> elements, List<Entity> Entitydata);
        //初始化楼层
        public abstract List<ThFloorModel> InitStoreys(AcadDatabase adb, List<ThIfcSpatialElement> storeys, List<ThFireCompartment> fireCompartments);
        //初始化虚拟楼层
        public abstract List<ThFloorModel> InitVirtualStoreys(Database db, Polyline storyBoundary, List<ThFireCompartment> fireCompartments);
        //画编号
        public abstract void DrawFloorsNum(Database db, List<ThFloorModel> addFloorss);
        //画系统图
        public abstract void DrawSystemDiagram(Vector3d Offset, Matrix3d ConversionMatrix);
    }
}
