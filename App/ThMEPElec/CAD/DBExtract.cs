using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.CAD
{
    public class DBExtract : IDisposable
    {
        public List<Polyline> MainBeams;
        public List<Polyline> SecondBeams;
        public List<Polyline> Walls;
        public List<Polyline> Columns;

        public List<Polyline> SubtractCurves = new List<Polyline>();

        public void Dispose()
        {
        }

        public void GetCurves()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                MainBeams = acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("Main_beam"); }).ToList();
                SecondBeams = acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("second_beam"); }).ToList();
                Walls = acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("outerWall"); }).ToList();
                Columns = acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("innerColumn"); }).ToList();

                SubtractCurves.AddRange(MainBeams);
                SubtractCurves.AddRange(Columns);
            }
        }
    }
}
