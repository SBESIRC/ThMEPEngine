using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThPlatform3D.StructPlane.Service
{
    internal class ThElevationTableBuilder
    {
        private string column1Title = "层号";
        private string column2Title = "标高BG(m)";
        private string column3Title = "层高(m)";
        private string column4aTitle = "墙、柱";
        private string column4bTitle = "砼等级";
        private string column5aTitle = "梁、板";
        private string column5bTitle = "砼等级";
        private int titleRowHeight = 950;
        private int contentRowHeight = 550;
        private int titleTextHeight = 300;
        private int contentTextHeight = 300;
        private double titleWidthFactor = 0.7;
        private double contentWidthFactor = 0.7;
        private List<int> RowHeights;
        private List<int> ColumnWidths;
        private ObjectId _elevationTableTextStyleId = ObjectId.Null;

        private List<ElevationInfo> Infos { get; set; }
        public ThElevationTableBuilder(List<ElevationInfo> infos)
        {
            Infos = infos;
            RowHeights = GetRowHeights();            
            ColumnWidths = new List<int> { 1000, 1500, 1500, 1000, 1000 };
            _elevationTableTextStyleId = DbHelper.GetTextStyleId(ThPrintStyleManager.ElevationTableTextStyleName);
        }
        public DBObjectCollection Build()
        {
            var results = new DBObjectCollection();
            var hLines = DrawHorLines();
            var vLines = DrawVerLines();
            var headTexts = FillHeaderTexts();
            var contentTexts = FillContentTexts();
            // 设置图层
            hLines.OfType<Line>().ForEach(line =>
            {
                line.Layer = ThPrintLayerManager.ElevationTableLineLayerName;
                line.ColorIndex = (int)ColorIndex.BYLAYER;
                line.LineWeight = LineWeight.ByLayer;
            });
            vLines.OfType<Line>().ForEach(line =>
            {
                line.Layer = ThPrintLayerManager.ElevationTableLineLayerName;
                line.ColorIndex = (int)ColorIndex.BYLAYER;
                line.LineWeight = LineWeight.ByLayer;
            });
            headTexts.OfType<DBText>().ForEach(text =>
            {
                text.Layer = ThPrintLayerManager.ElevationTableTextLayerName;
                text.ColorIndex = (int)ColorIndex.BYLAYER;
                text.LineWeight = LineWeight.ByLayer;
            });
            contentTexts.OfType<DBText>().ForEach(text =>
            {
                text.Layer = ThPrintLayerManager.ElevationTableTextLayerName;
                text.ColorIndex = (int)ColorIndex.BYLAYER;
                text.LineWeight = LineWeight.ByLayer;
            });

            // 收集结果
            results = results.Union(hLines);
            results = results.Union(vLines);
            results = results.Union(headTexts);
            results = results.Union(contentTexts);
            return results;
        }
        private DBObjectCollection FillContentTexts()
        {
            var results = new DBObjectCollection();
            var preHeights = RowHeights.Take(1).Sum();
            for(int i=1;i< RowHeights.Count;i++)
            {
                var width = 0.0;
                var info = Infos[i - 1];
                for (int j=0;j<ColumnWidths.Count;j++)
                {
                    var center = new Point3d(width + ColumnWidths[j] / 2.0, 
                        preHeights+ RowHeights[i] / 2.0, 0.0);
                    width += ColumnWidths[j];
                    switch (j)
                    {
                        case 0:
                            var text1 = CreateText(center, info.FloorNo, contentTextHeight,
                                contentWidthFactor, _elevationTableTextStyleId);
                            results.Add(text1);
                            break;
                        case 1:
                            var text2 = CreateText(center, info.BottomElevation, contentTextHeight,
                                contentWidthFactor, _elevationTableTextStyleId);
                            results.Add(text2);
                            break;
                        case 2:
                            var text3 = CreateText(center, info.FloorHeight, contentTextHeight,
                                contentWidthFactor, _elevationTableTextStyleId);
                            results.Add(text3);
                            break;
                        case 3:
                            var text4 = CreateText(center,info.WallColumnGrade, contentTextHeight, 
                                contentWidthFactor, _elevationTableTextStyleId);                            
                            results.Add(text4);
                            break;
                        case 4:
                            var text5 = CreateText(center, info.BeamBoardGrade, contentTextHeight,
                                contentWidthFactor, _elevationTableTextStyleId);
                            results.Add(text5);
                            break;
                        default:
                            break;
                    }
                }
                preHeights += RowHeights[i];
            }
            return results
                .OfType<DBText>()
                .ToCollection();
        }
        private DBObjectCollection FillHeaderTexts()
        {
            var results = new DBObjectCollection();
            var width = 0.0;
            for(int i=0;i<ColumnWidths.Count;i++)
            {
                var center = new Point3d(width+ ColumnWidths[i]/2.0,titleRowHeight/2.0,0.0);
                width += ColumnWidths[i];
                switch (i)
                {
                    case 0:
                        var text1 = CreateText(center, column1Title, titleTextHeight, 
                            titleWidthFactor, _elevationTableTextStyleId);
                        results.Add(text1);
                        break;
                    case 1:
                        var text2 = CreateText(center, column2Title, titleTextHeight, 
                            titleWidthFactor, _elevationTableTextStyleId);
                        results.Add(text2);
                        break;
                    case 2:
                        var text3 = CreateText(center, column3Title, titleTextHeight, 
                            titleWidthFactor, _elevationTableTextStyleId);
                        results.Add(text3);
                        break;
                    case 3:
                        var text4a = CreateText(center + new Vector3d(0, titleTextHeight*0.7, 0), 
                            column4aTitle, titleTextHeight, titleWidthFactor,
                            _elevationTableTextStyleId);
                        var text4b = CreateText(center - new Vector3d(0, titleTextHeight * 0.7, 0), 
                            column4bTitle, titleTextHeight, titleWidthFactor,
                            _elevationTableTextStyleId);
                        results.Add(text4a);
                        results.Add(text4b);
                        break;
                    case 4:
                        var text5a = CreateText(center + new Vector3d(0, titleTextHeight * 0.7, 0),
                            column5aTitle, titleTextHeight, titleWidthFactor,
                            _elevationTableTextStyleId);
                        var text5b = CreateText(center - new Vector3d(0, titleTextHeight * 0.7, 0), 
                            column5bTitle, titleTextHeight, titleWidthFactor,
                            _elevationTableTextStyleId);
                        results.Add(text5a);
                        results.Add(text5b);
                        break;
                    default:
                        break;
                }
            }
            return results.OfType<DBText>().ToCollection();
        }
        private DBText CreateText(
            Point3d position,
            string content,
            double height,
            double widthFactor,
            ObjectId textStyleId,
            TextHorizontalMode hm = TextHorizontalMode.TextCenter,
            TextVerticalMode vm = TextVerticalMode.TextVerticalMid)
        {
            //在原点创建文字
            if(string.IsNullOrEmpty(content) || height<=0.0 || 
                widthFactor<=0.0 || textStyleId == ObjectId.Null)
            {
                return null;
            }
            else
            {
                return new DBText()
                {
                    TextString = content,
                    Height = height,
                    Position = position,
                    WidthFactor = widthFactor,
                    TextStyleId = textStyleId,
                    VerticalMode = vm,
                    HorizontalMode = hm,
                    AlignmentPoint = position
                };
            }
        }
        private DBObjectCollection DrawHorLines()
        {
            var results = new DBObjectCollection();
            var totalWidth = TotalColumnWidth;
            int height = 0;
            for (int i = 0; i < RowHeights.Count; i++)
            {
                height += RowHeights[i];
                if (i == 0)
                {
                    results.Add(new Line(Point3d.Origin, new Point3d(totalWidth, 0, 0)));
                }
                results.Add(new Line(new Point3d(0, height, 0), new Point3d(totalWidth, height, 0)));
            }
            return results;
        }
        private DBObjectCollection DrawVerLines()
        {
            var results = new DBObjectCollection();
            var totalHeight = TotalRowHeight;
            int width = 0;
            for (int i = 0; i < ColumnWidths.Count; i++)
            {
                width += ColumnWidths[i];
                if (i==0)
                {
                    results.Add(new Line(Point3d.Origin, new Point3d(0, totalHeight, 0)));
                }
                results.Add(new Line(new Point3d(width, 0, 0), new Point3d(width, totalHeight, 0)));
            }
            return results;
        }
        private int TotalRowHeight
        {
            get
            {
                return RowHeights.Sum();
            }
        }
        private int TotalColumnWidth
        {
            get
            {
                return ColumnWidths.Sum();
            }
        }
        private List<int> GetRowHeights()
        {
            var rowHeights = new List<int>() { titleRowHeight };
            Infos.ForEach(info => rowHeights.Add(contentRowHeight));
            return rowHeights;
        }
    }
    internal class ElevationInfo
    {
        /// <summary>
        /// 楼层号
        /// </summary>
        public string FloorNo { get; set; } = "";
        /// <summary>
        /// 标高
        /// </summary>
        public string BottomElevation { get; set; } = "";
        /// <summary>
        /// 层高
        /// </summary>
        public string FloorHeight { get; set; } = "";
        /// <summary>
        /// 墙柱等级
        /// </summary>
        public string WallColumnGrade { get; set; } = "";
        /// <summary>
        /// 楼板、梁等级
        /// </summary>
        public string BeamBoardGrade { get; set; } = "";
    }
}
