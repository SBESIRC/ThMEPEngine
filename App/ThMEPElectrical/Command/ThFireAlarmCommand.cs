using System;
using AcHelper;
using System.IO;
using Linq2Acad;
using ThCADExtension;
using FireAlarm.Data;
using AcHelper.Commands;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;

namespace ThMEPElectrical.Command
{
    public class ThFireAlarmCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area < 1e-4)
                {
                    return;
                }
                var nFrame = ThMEPFrameService.Normalize(frame);
                var datasetFactory = new ThFireAlarmDataSetFactory();
                var dataset = datasetFactory.Create(acadDatabase.Database, nFrame.Vertices());
                //输出
                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(dataset.Container, path, fileInfo.Name+DateTime.Now.ToString("hh-mm-ss"));
            }
        }
    }
}
