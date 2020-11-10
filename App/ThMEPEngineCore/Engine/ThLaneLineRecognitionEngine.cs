using Linq2Acad;
using System;
using System.Collections.Generic;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThLaneLineRecognitionEngine : IDisposable
    {
        public List<Curve> Lanes { get; private set; }
        public ThLaneLineRecognitionEngine()
        {
            Lanes = new List<Curve>();
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
                        Lanes.Add(o.Clone() as Curve);
                    }
                });
            }
        }
    }
}
