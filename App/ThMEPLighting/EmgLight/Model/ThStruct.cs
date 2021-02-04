using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.EmgLight.Service;


namespace ThMEPLighting.EmgLight.Model
{
    public class ThStruct
    {
        //private EmgLightCommon.ThStructType m_thStructType;

        //  public ThStruct(Polyline structure, Polyline oriStruct, EmgLightCommon.ThStructType type)
        public ThStruct(Polyline structure, Polyline oriStruct)
        {
            geom = structure;
            oriStructGeo = oriStruct;
            centerPt = GeomUtils.GetStructCenter(geom);
            dir = (geom.EndPoint - geom.StartPoint).GetNormal();
            //  m_thStructType = type;

        }

        //public EmgLightCommon.ThStructType thStructType { 
        //    get 
        //    { 
        //        return m_thStructType; 
        //    } 
        //}
        public Vector3d dir { get; }

        public Polyline geom { get; }

        public Polyline oriStructGeo { get; }

        public Point3d centerPt { get; }

    }
}
