using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.CAD
{
    public class DBExtract : IDisposable
    {
        public List<Polyline> MainBeams
        {
            get;
            private set;
        } = new List<Polyline>();

        public List<Polyline> SecondBeams
        {
            get;
            private set;
        } = new List<Polyline>();

        public List<Polyline> Walls
        {
            get;
            private set;
        } = new List<Polyline>();

        public List<Polyline> Columns
        {
            get;
            private set;
        } = new List<Polyline>();

        public List<Polyline> SubtractCurves = new List<Polyline>();

        public void Dispose()
        {
        }

        public void GetCurves()
        {
            // 临时数据
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //ThBeamConnectRecogitionEngine.ExecuteRecognize(acadDatabase.Database,)
                foreach (var mainBeam in acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("Main_beam"); }).ToList())
                {
                    MainBeams.Add(mainBeam.Clone() as Polyline);
                }

                foreach (var secondBeam in acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("second_beam"); }).ToList())
                {
                    SecondBeams.Add(secondBeam.Clone() as Polyline);
                }

                foreach (var wall in acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("outerWall"); }).ToList())
                {
                    Walls.Add(wall.Clone() as Polyline);
                }

                foreach (var column in acadDatabase.ModelSpace.OfType<Polyline>().Where(o => { return o.Layer.Contains("innerColumn"); }).ToList())
                {
                    Columns.Add(column.Clone() as Polyline);
                }

                SubtractCurves.AddRange(MainBeams);
                SubtractCurves.AddRange(Columns);
            }
        }
    }
}
