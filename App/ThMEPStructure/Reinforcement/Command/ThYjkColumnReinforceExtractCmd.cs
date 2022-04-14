using System;
using System.Collections.Generic;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Data.YJK;

namespace ThMEPStructure.Reinforcement.Command
{
    public class ThYjkColumnReinforceExtractCmd : ThMEPBaseCommand, IDisposable
    {
        public List<ColumnExtractInfo> ExtractInfos { get; private set; }
        public bool IsSuccess { get; set; }
        public ThYjkColumnReinforceExtractCmd()
        {
            ActionName = "提取信息";
            CommandName = "THZHZ";
            ExtractInfos = new List<ColumnExtractInfo>();
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            using (var pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                if(pc.CollectedPoints ==null || pc.CollectedPoints.Count==0)
                {
                    return;
                }
                IsSuccess = true;
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var dataset = new ThYJKColumnDataSetFactory();
                dataset.Create(acadDb.Database, frame.Vertices());
                ExtractInfos = dataset.Results;
                frame.Dispose();
            }
        }
    }
}
