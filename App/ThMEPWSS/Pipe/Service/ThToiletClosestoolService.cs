using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Plumbing;

namespace ThMEPWSS.Pipe.Service
{
    public class ThToiletClosestoolService
    {        
        private List<ThIfcClosestool> Closestools { get; set; }
        private ThIfcSpace ToiletSpace { get; set; }
        private ThCADCoreNTSSpatialIndex ClosestoolSpatialIndex { get; set; }
        public bool IsFinded
        {
            get
            {
                return ClosestoolCollection.Count>0;
            }
        }
        /// <summary>
        /// 找到的坐便器
        /// 目前只支持查找一个
        /// </summary>
        public List<ThIfcClosestool> ClosestoolCollection 
        { 
            get;
            set; 
        } 
        private ThToiletClosestoolService(
            List<ThIfcClosestool> closestools, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex closestoolSpatialIndex)
        {            
            Closestools = closestools;
            ToiletSpace = toiletSpace;
            ClosestoolSpatialIndex = closestoolSpatialIndex;
            ClosestoolCollection = new List<ThIfcClosestool>();
            if (ClosestoolSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                Closestools.ForEach(o => dbObjs.Add(o.Outline));
                ClosestoolSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThToiletClosestoolService Find(
            List<ThIfcClosestool> closestools, 
            ThIfcSpace toiletSpace, 
            ThCADCoreNTSSpatialIndex closestoolSpatialIndex = null)
        {
            var instance = new ThToiletClosestoolService(closestools, toiletSpace, closestoolSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {
            var tolitBoundary = ToiletSpace.Boundary as Polyline;
            var crossObjs = ClosestoolSpatialIndex.SelectCrossingPolygon(tolitBoundary);            
            var crossClosestools = Closestools.Where(o => crossObjs.Contains(o.Outline));
            var includedClosestools = crossClosestools.Where(o => tolitBoundary.Contains(o.Outline as Curve));
            includedClosestools.ForEach(o => ClosestoolCollection.Add(o));
        }        
    }
}
