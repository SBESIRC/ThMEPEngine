using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThLaneLineRecognitionEngine : IDisposable
    {
        public List<Curve> LaneCurves;
        public ThLaneLineRecognitionEngine()
        {
            LaneCurves = new List<Curve>();
        }
        public void Dispose()
        {            
        }
        public void Recognize(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (var lanelineDbExtension = new ThLaneLineDbExtension(database))
            {
                lanelineDbExtension.BuildElementCurves();
                lanelineDbExtension.LaneCurves.ForEach(o =>
                {
                    if (o.GetLength() > 0.0)
                    {
                        LaneCurves.Add(o.Clone() as Curve);
                    }
                });
            }
        }
    }
}
