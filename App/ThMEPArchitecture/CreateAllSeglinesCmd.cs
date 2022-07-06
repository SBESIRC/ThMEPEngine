using AcHelper;
using Linq2Acad;
using System;
using ThMEPArchitecture.ParkingStallArrangement.Extractor;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPEngineCore.Command;
using ThMEPArchitecture.ViewModel;
using System.IO;
using Serilog;
using Autodesk.AutoCAD.EditorInput;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using System.Linq;
using ThCADCore.NTS;
using ThParkingStall.Core.Tools;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;

namespace ThMEPArchitecture
{
    public class CreateAllSeglinesCmd : ThMEPBaseCommand, IDisposable
    {
        public static string LogFileName = Path.Combine(System.IO.Path.GetTempPath(), "SeglineLog.txt");

        public Serilog.Core.Logger Logger = new Serilog.LoggerConfiguration().WriteTo
            .File(LogFileName, flushToDiskInterval: new TimeSpan(0, 0, 5), rollingInterval: RollingInterval.Day, retainedFileCountLimit:10).CreateLogger();
        public static ParkingStallArrangementViewModel ParameterViewModel { get; set; }

        public CreateAllSeglinesCmd()
        {
            CommandName = "-THDXFGXSC";//天华地下分割线生成
            ActionName = "生成";
        }

        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            try
            {
                using (var docLock = Active.Document.LockDocument())
                using (AcadDatabase acadDatabase = AcadDatabase.Active())
                {
                    Run(acadDatabase);
                }
            }
            catch (Exception ex)
            {
                Active.Editor.WriteMessage(ex.Message);
                Logger?.Information(ex.Message);
                Logger?.Information("##################################");
                Logger?.Information(ex.StackTrace);
            }
        }

        public override void AfterExecute()
        {
            Active.Editor.WriteMessage($"seconds: {_stopwatch.Elapsed.TotalSeconds} \n");
            base.AfterExecute();
        }
        //public void Run(AcadDatabase acadDatabase, bool randomFlag = true)//false 自动生成全部分割线, ture 生成随机分割线
        //{
        //    var rstDataExtract = InputData.GetOuterBrder(acadDatabase, out OuterBrder outerBrder, Logger);
        //    if (!rstDataExtract)
        //    {
        //        return;
        //    }
        //    //TODO
        //    if(randomFlag)
        //    {
        //        var seglines = Dfs.GetRandomSeglines(outerBrder);//生成随机的分割线方案

        //    }
        //    else
        //    {
        //        var seglinesList = Dfs.GetDichotomySegline(outerBrder);//生成全部的分割线方案
        //        foreach(var seglines in seglinesList)
        //        {
                    
        //        }
        //    }
        //}

        public void Run(AcadDatabase acadDatabase) 
        {
            var entOpt = new PromptSelectionOptions { MessageForAdding = "\n选择线和多段线:" };
            var result = Active.Editor.GetSelection(entOpt);
            if (result.Status != PromptStatus.OK) return;
            var tol = Active.Editor.GetInteger("\n 请输入钝角阈值:");
            if (tol.Status != PromptStatus.OK) return;
            var options = new PromptKeywordOptions("\n是否选凹角：");
            options.Keywords.Add("是", "Y", "是(Y)");
            options.Keywords.Add("否", "N", "否(N)");

            options.Keywords.Default = "是";
            var Msg = Active.Editor.GetKeywords(options);
            if (Msg.Status != PromptStatus.OK) return;
            var inner = Msg.StringResult.Equals("是");

            var objs = new List<Line>();
            foreach (var id in result.Value.GetObjectIds())
            {

                var obj = acadDatabase.Element<Entity>(id);
                if (obj is Polyline pline)
                {
                    objs.AddRange(pline.ToLines());
                }
                else if (obj is Line line)
                {
                    objs.Add(line);
                }
            }
            //objs.ForEach(l => l.ToNTSLineSegment().Extend(100).ToDbLine().AddToCurrentSpace());

            var polygons = objs.Select(l => l.ToNTSLineSegment()).ToList().GetPolygons();
            ;
            foreach (var polygon in polygons)
            {
                var Obtusified = polygon.Obtusify(inner,tol.Value);
                if (Obtusified == null) continue;
                Obtusified.ToDbMPolygon().AddToCurrentSpace();
            }
        }
    }
}
