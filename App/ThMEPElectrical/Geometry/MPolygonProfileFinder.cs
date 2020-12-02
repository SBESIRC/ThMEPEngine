using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Geometry
{
    public class MPolygonProfileFinder
    {
        private List<Polyline> m_firstPolylines;
        private List<Polyline> m_secPolylines;

        public Polyline Shell
        {
            get;
            set;
        }

        public List<Polyline> Holes
        {
            get;
            set;
        } = new List<Polyline>();

        public PolygonInfo PolygonRes
        {
            get;
            set;
        }

        public MPolygonProfileFinder(List<Polyline> firstPolylines, List<Polyline> secPolylines)
        {
            m_firstPolylines = firstPolylines;
            m_secPolylines = secPolylines;
        }

        public static PolygonInfo MakePolygonInfo(List<Polyline> firstPolylines, List<Polyline> secPolylines)
        {
            var polygonFinder = new MPolygonProfileFinder(firstPolylines, secPolylines);
            polygonFinder.Do();
            return polygonFinder.PolygonRes;
        }

        public void Do()
        {

        }
        
    }
}
