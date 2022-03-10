using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Hvac
{
    public class PortModifyParam
    {
        public Point3d pos;
        public Handle handle;
        public string portRange;
        public double portWidth;
        public double portHeight;
        public double portAirVolume;
        public double rotateAngle;
    }
    public class TextModifyParam
    {
        public Point3d pos;
        public Handle handle;
        public double height;
        public string textString;
        public double rotateAngle;
    }
    public class HoleModifyParam
    {
        public Handle handle;
        public string holeName;
        public string holeLayer;
        public Point3d insertP;
        public double rotateAngle;
        public double len;
        public double width;
    }
    public class MufflerModifyParam
    {
        public Handle handle;
        public string name;
        public string mufflerLayer;
        public Point3d insertP;
        public string mufflerVisibility;
        public double len;
        public double width;
        public double height;
        public double textHeight;
        public double rotateAngle;
    }
    public class ValveModifyParam
    {
        public Handle handle;
        public string valveName;
        public string valveLayer;
        public string valveVisibility;
        public Point3d insertP;
        public double rotateAngle;
        public double width;
        public double height;
        public double textAngle;
        public double textHeight;
    }
    public class EntityModifyParam
    {
        public string type;                           //写进去的XData
        public Handle handle;                         //读图时解析
        public Point3d centerP;                       //读图时解析
        // 端点到端口宽度的映射
        public Dictionary<Point3d, string> portWidths;//读图时解析
    }
    public class DuctModifyParam
    {
        public Point3d sp;          // 管段起点
        public Point3d ep;          // 管段终点
        public Handle handle;       // 管段组handle
        public string type;
        public string ductSize;
        public double airVolume;
        public double elevation;
        public DuctModifyParam() { }
        public DuctModifyParam(string ductSize,
                               double airVolume,
                               double elevation,
                               Point3d sp,
                               Point3d ep)
        {
            type = "Duct";
            this.sp = sp;
            this.ep = ep;
            this.elevation = elevation;
            this.ductSize = ductSize;
            this.airVolume = airVolume;
        }
    }
    public class VTElbowModifyParam
    {
        public Handle handle;
        public Point3d detectP;
    }
}