using System;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using System.Collections.Generic;
using ThMEPEngineCore.IO;
using System.IO;
using AcHelper;

using ThMEPEngineCore.Command;
using ThMEPElectrical.FireAlarmFixLayout.Data;

namespace ThMEPElectrical.Command
{
    public class ThFireAlarmExtractData : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }
        public ThFireAlarmExtractData()
        {
            CommandName = "ThFireAlarmExtractData";
            ActionName = "测试数据";
        }

        public override void SubExecute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var frame = ThMEPEngineCore.CAD.ThWindowInteraction.GetPolyline(
                    PointCollector.Shape.Window, new List<string> { "请框选一个范围" });
                if (frame.Area < 1e-4)
                {
                    return;
                }
                var pts = frame.Vertices();
                var datasetFactory = new ThFaFixLayoutDataSetFactory();
                var dataset = datasetFactory.Create(acadDatabase.Database, pts);
                var fileInfo = new FileInfo(Active.Document.Name);
                var path = fileInfo.Directory.FullName;
                ThGeoOutput.Output(dataset.Container, path, fileInfo.Name + DateTime.Now.ToShortTimeString().Replace(':', '-'));
            }
        }
    }
}
