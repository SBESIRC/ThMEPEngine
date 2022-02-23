using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThRoomOutlineSimplifier:ThPolygonalElementSimplifier
    {
        public ThRoomOutlineSimplifier()
        {
            OFFSETDISTANCE = 20.0;
            DISTANCETOLERANCE = 1.0;
            TESSELLATEARCLENGTH = 100.0;
            ClOSED_DISTANC_TOLERANCE = 1000.0; // 待定
            AREATOLERANCE = 100.0; //过滤房间面积
        }
    }
}
