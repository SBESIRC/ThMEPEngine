using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Data.YJK;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Command
{
    public class ThYjkReinforceExtractCmd : ThMEPBaseCommand, IDisposable
    {
        #region ---------- Input -----------
        /// <summary>
        /// 标注文字图层
        /// </summary>
        public List<string> TextLayers { get; set; } = new List<string>();
        /// <summary>
        /// 墙图层
        /// </summary>
        public List<string> WallLayers { get; set; } = new List<string>();
        /// <summary>
        /// 墙柱图层
        /// </summary>
        public List<string> WallColumnLayers { get; set; } = new List<string>();
        #endregion
        #region ---------- Output ----------
        public List<EdgeComponentExtractInfo> ExtractInfos { get; private set; } =
            new List<EdgeComponentExtractInfo>();
        public bool IsSuccess { get; set; }
        #endregion
        public ThYjkReinforceExtractCmd()
        {
            ActionName = "提取信息";
            CommandName = "THQZPJ";
        }

        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
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
                if (pc.CollectedPoints == null || pc.CollectedPoints.Count == 0)
                {
                    return;
                }
                IsSuccess = true;
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var dataset = new ThYJKDataSetFactory()
                {
                    WallLayers = this.WallLayers,
                    TextLayers = this.TextLayers,
                    WallColumnLayers = WallColumnLayers,
                    AntiSeismicGrade = ThWallColumnReinforceConfig.Instance.AntiSeismicGrade,
                };
                dataset.Create(acadDb.Database, frame.Vertices());
                ExtractInfos = dataset.Results;
                frame.Dispose();
            }
        }
    }
}
