using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.UndergroundWaterSystem.Model
{
    public abstract class ThBaseModel
    {
        public Point3d Position { set; get; }//位置
        public abstract void Initialization(Entity entity);//初始化数据，获取所需要的信息
        
    }
}
