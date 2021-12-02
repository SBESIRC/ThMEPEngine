using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.AFAS.Service
{
    public class ThHandleNonClosedPolylineService
    {
        private const double GapLength = 1000.0;
        //public static Polyline Handle(Polyline origin)
        //{
        //    //不支持自交，如果自交的话，返回的是空线段
        //    //不直线两个点的线段
        //    //......
        //    var clone = origin.Clone() as Polyline;
        //    if(IsValid(clone))
        //    {
        //        clone.Closed = true;
        //        return clone;
        //    }
        //    else
        //    {
        //        return origin;
        //    }
        //}

        public static Curve Handle(Curve origin)
        {
            //不支持自交，如果自交的话，返回的是空线段
            //不直线两个点的线段
            //......
       
            Curve objReturn = null;
            if (origin is Polyline pl)
            {
                var clone = pl.Clone() as Polyline;
                if (IsValid(clone))
                {
                    clone.Closed = true;
                    objReturn= clone;
                }
                else
                {
                    objReturn= origin;
                }
            }
            else
            {
                objReturn = origin;
            }
            return objReturn;
        }

        private static bool IsValid(Polyline polyline)
        {
            if(polyline.NumberOfVertices==2)
            {
                return false;
            }
            if(polyline.StartPoint.DistanceTo(polyline.EndPoint) <= GapLength)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
