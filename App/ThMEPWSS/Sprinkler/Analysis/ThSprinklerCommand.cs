

using AcHelper;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System.Collections.Generic;
using ThCADExtension;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;
using AcHelper.Commands;
using System;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var factory = new ThSprinklerDataSetFactory();
                var geometries = factory.Create(currentDb.Database, frame.Vertices()).Container;

                var beamChecker = new ThSprinklerBeamChecker();
                var objs = beamChecker.Check(geometries);
                beamChecker.Present(currentDb.Database, objs);


            }
        }
    }
}
