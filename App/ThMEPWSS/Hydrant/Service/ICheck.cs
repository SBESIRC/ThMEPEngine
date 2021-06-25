using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Hydrant.Service
{
    public interface ICheck
    {
        List<ThIfcRoom> Rooms { get; set; }
        /// <summary>
        /// 保护区域对应的消火栓/灭火器的坐标点
        /// </summary>
        List<Tuple<Entity,Point3d,List<Entity>>> Covers { get; set; }
        void Check(Database db,Point3dCollection pts);
        void Print(Database db);
    }
}
