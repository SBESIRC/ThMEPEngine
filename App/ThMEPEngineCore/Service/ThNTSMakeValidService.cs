using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Interface;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThNTSMakeValidService : IMakeValid
    {
        public Polyline MakeValid(Polyline polyline)
        {
            if(polyline!=null && polyline.Length>0.0)
            {
               var objs = polyline.MakeValid();
               if(objs.Count==0)
                {
                    return polyline.Clone() as Polyline;
                }
               else if(objs.Count == 1)
                {
                    return objs[0] as Polyline;
                }
               else
                {
                    return objs.Cast<Polyline>().OrderByDescending(o => o.Length).First();
                }
            }
            else
            {
                return new Polyline();
            }
        }

        public DBObjectCollection MakeValid(DBObjectCollection polylines)
        {
            return polylines
                .Cast<Polyline>()
                .Select(o => MakeValid(o))
                .Where(o=>o.Area>0.0)
                .ToCollection();
        }
    }
}
