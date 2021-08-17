using System;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Command
{
    public class ThRoomServiceCommand : IAcadCommand, IDisposable
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
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
                var builder = new ThRoomBuilderEngine();
                var rooms = builder.BuildFromMS(acadDatabase.Database, frame.Vertices());
                var service = new ThMEPEngineCoreRoomService();
                service.Initialize();
                rooms.ForEach(r =>
                {
                    if (r.Tags.Count > 0) 
                    {
                        var labels = service.GetLabels(r);
                        if (labels.Count > 0) 
                        {
                            service.IsPublic(labels);
                        }
                        
                    }
                });
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}