using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Model
{
    public class ThRegionBorder
    {
        public string Id { get; set; }
        /// <summary>
        /// 布灯的边界Polyline,MPolygon
        /// </summary>
        public Entity RegionBorder { get; set; }
        /// <summary>
        /// 布灯线槽中心线(车道线)
        /// </summary>
        public List<Line> DxCenterLines { get; set; }
        /// <summary>
        /// 非布灯线槽中心线
        /// </summary>
        public List<Line> FdxCenterLines { get; set; }
        /// <summary>
        /// 单排线槽先
        /// 双排布置，绘制的专门用于单槽布置的线
        /// </summary>
        public List<Line> SingleRowLines { get; set; }
        /// <summary>
        /// 梁
        /// </summary>
        public List<ThIfcBeam> Beams { get; set; }
        /// <summary>
        /// 柱
        /// </summary>
        public List<ThIfcColumn> Columns { get; set; }
        /// <summary>
        /// 一号线
        /// </summary>
        public List<Line> FirstLightingLines { get; set; }
        /// <summary>
        /// 二号线
        /// </summary>
        public List<Line> SecondLightingLines { get; set; }
        /// <summary>
        /// 1号线到2号线的延伸线
        /// </summary>

        public List<Line> ExtendLines { get; set; }

        #region ---------- 收集防火分区内的布灯实体-----------
        /// <summary>
        /// Dwg中区域中已布的灯->
        /// </summary>
        public List<BlockReference> Lights { get; set; }

        /// <summary>
        /// Dwg中区域中已生成的文字
        /// </summary>
        public List<DBText> Texts { get; set; }

        /// <summary>
        /// Dwg中区域中已布灯的线(CenterLine)
        /// </summary>
        public List<Line> CenterLines { get; set; }

        /// <summary>
        /// 线槽边线(包括端口线)
        /// </summary>
        public List<Line> SideLines { get; set; }

        /// <summary>
        /// 线槽边线(包括端口线)
        /// </summary>
        public List<Curve> JumpWires { get; set; }

        /// <summary>
        /// 天正桥架
        /// </summary>
        public List<Entity> TCHCableTrays { get; set; }
        #endregion

        /// <summary>
        /// 边界到原点的偏移
        /// </summary>
        public ThMEPOriginTransformer Transformer { get; set; } = new ThMEPOriginTransformer(Point3d.Origin);
        /// <summary>
        /// Ucs的矩阵
        /// </summary>
        public Matrix3d WcsToUcs { get; set; }
       
        public ThRegionBorder()
        {
            Texts = new List<DBText>();
            Beams = new List<ThIfcBeam>();
            SideLines = new List<Line>();
            JumpWires = new List<Curve>();
            CenterLines = new List<Line>();
            Columns = new List<ThIfcColumn>();
            DxCenterLines = new List<Line>();
            FdxCenterLines = new List<Line>();
            SingleRowLines = new List<Line>();
            Lights = new List<BlockReference>();
            WcsToUcs = Matrix3d.Identity;
            Id = Guid.NewGuid().ToString();
            ExtendLines = new List<Line>();
            FirstLightingLines = new List<Line>();
            SecondLightingLines = new List<Line>();
        }

        public void Transform()
        {
            // 移动到原点
            // 若图元离原点非常远（大于1E+10)，精度会受很大影响
            Transformer.Transform(DxCenterLines.ToCollection());
            Transformer.Transform(RegionBorder);
            Transformer.Transform(FdxCenterLines.ToCollection());
            Columns.ForEach(c => Transformer.Transform(c.Outline));
            Beams.ForEach(c => Transformer.Transform(c.Outline));

            // 移动框中已布置的元素
            UpgradeOpen();
            Transformer.Transform(Lights.ToCollection());
            Transformer.Transform(Texts.ToCollection());
            Transformer.Transform(TCHCableTrays.ToCollection());
            Transformer.Transform(CenterLines.ToCollection());
            Transformer.Transform(SideLines.ToCollection());
            Transformer.Transform(JumpWires.ToCollection());
        }

        public void Reset()
        {
            Transformer.Reset(DxCenterLines.ToCollection());
            Transformer.Reset(FdxCenterLines.ToCollection());
            Transformer.Reset(TCHCableTrays.ToCollection());
            Transformer.Reset(RegionBorder);
            Columns.ForEach(c => Transformer.Reset(c.Outline));
            Beams.ForEach(c => Transformer.Reset(c.Outline));

            // 还原框中已布置的元素
            Transformer.Reset(Lights.ToCollection());
            Transformer.Reset(Texts.ToCollection());
            Transformer.Reset(CenterLines.ToCollection());
            Transformer.Reset(SideLines.ToCollection());
            Transformer.Reset(JumpWires.ToCollection());
            DowngradeOpen();
        }
        public void Noding()
        {
            var cleaner = new ThLineNodingService(DxCenterLines, FdxCenterLines, SingleRowLines);
            cleaner.Noding();
            DxCenterLines = cleaner.DxLines;
            FdxCenterLines = cleaner.FdxLines;
            SingleRowLines = cleaner.SingleRowLines;
        }

        public void CleanNoding()
        {
            DxCenterLines = DxCenterLines.CleanNoding();
        }

        public void HandleSharpAngle()
        {
            if (DxCenterLines.Count == 0)
            {
                return;
            }
            var cleaner = new ThSharpAngleHandleService(DxCenterLines, FdxCenterLines, SingleRowLines, 2700);
            cleaner.Handle();
            DxCenterLines = cleaner.Dxs.OfType<Line>().ToList();
            FdxCenterLines = cleaner.Fdxs.OfType<Line>().ToList();
            SingleRowLines = cleaner.SingleRowLines.OfType<Line>().ToList();
        }

        public void Normalize()
        {
            // 统一方向
            DxCenterLines = DxCenterLines.Normalize();
        }
        public void Sort()
        {
            DxCenterLines = DxCenterLines.Sort(WcsToUcs);
        }
        public void Trim()
        {
            // 裁剪并获取框内的车道线
            DxCenterLines = DxCenterLines.Trim(RegionBorder);
            FdxCenterLines = FdxCenterLines.Trim(RegionBorder);
            SingleRowLines = SingleRowLines.Trim(RegionBorder);
        }
        public void Shorten(double regionBorderBufferDistance)
        {
            // 为了避免线槽和防火卷帘冲突
            // 缩短车道线，和框线保持500的间隙
            var newBorder = RegionBorder.BufferEx(-regionBorderBufferDistance);
            if(newBorder ==null)
            {
                return;
            }
            DxCenterLines = DxCenterLines.Trim(newBorder);
            FdxCenterLines = FdxCenterLines.Trim(newBorder);
            SingleRowLines = SingleRowLines.Trim(newBorder);
        }
        public void Merge()
        {
            // 借用车道线的处理方法
            DxCenterLines = ThLaneLineMergeExtension.Merge(DxCenterLines.ToCollection()).OfType<Line>().ToList();
        }
        public List<Curve> Merge(double mergeRange)
        {
            //从小汤车道线合并服务中获取合并的主道线，辅道线
            //张皓提出此逻辑不需要，合并完以后，设计师不好调(2021.20.26)
            //var frame = regionBorder.RegionBorder is MPolygon polygon ? polygon.Shell() : regionBorder.RegionBorder as Polyline;
            //var curves = ThMergeLightLineService.Merge(frame, dxNomalLines, mergeRange);
            //regionBorder.DxCenterLines = curves.Explode();

            //合并外角小于45度的连接线
            var lines = ThMergeLightLineService.Merge(DxCenterLines);
            return lines.Select(l => l.ToPolyline(ThGarageLightCommon.RepeatedPointDistance)).Cast<Curve>().ToList();
        }
        public void TrimOffsetLines(double offsetDis)
        {
            // 处理灯线Buffer后超出Border部分
            var handleService = new ThFilterLineBufferOverBorderService();
            var results = handleService.Filter(DxCenterLines, RegionBorder, offsetDis);
            var garbages = DxCenterLines.ToCollection().Difference(results.ToCollection());
            garbages.MDispose();
            DxCenterLines.Clear();
            DxCenterLines = results;
        }
        private void UpgradeOpen()
        {
            Lights.ForEach(o => o.UpgradeOpen());
            Texts.ForEach(o => o.UpgradeOpen());
            CenterLines.ForEach(o=> o.UpgradeOpen());
            SideLines.ForEach(o => o.UpgradeOpen());
            JumpWires.ForEach(o => o.UpgradeOpen());
        }
        private void DowngradeOpen()
        {
            Lights.ForEach(o => o.DowngradeOpen());
            Texts.ForEach(o => o.DowngradeOpen());
            CenterLines.ForEach(o => o.DowngradeOpen());
            SideLines.ForEach(o => o.DowngradeOpen());
            JumpWires.ForEach(o => o.DowngradeOpen());
        }
        #region ---------- Print ----------
        public void PrintDxLines()
        {
            Print(DxCenterLines);
        }
        public void PrintFdxLines()
        {
            Print(FdxCenterLines);
        }
        public void PrinSingleRowLines()
        {
            Print(SingleRowLines);
        }
        public void PrintFirstLines()
        {
            Print(FirstLightingLines);
        }
        public void PrintSecondLines()
        {
            Print(SecondLightingLines);
        }
        public void PrintExtendLines()
        {
            Print(ExtendLines);
        }
        private void Print(List<Line> lines)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                lines.ForEach(l =>
                {
                    var newLine = l.Clone() as Line;
                    acadDb.ModelSpace.Add(newLine);
                    newLine.SetDatabaseDefaults();
                });
            }
        }
        #endregion
    }
}
