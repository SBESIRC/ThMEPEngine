using System;
using System.Windows;
using System.Windows.Controls;

namespace TianHua.Electrical.PDS.UI.Models
{
    public class ThPDSCircuitGraphLayoutEngine
    {
        int r, c;
        Grid grid;
        public Grid Grid => this.grid;
        public int CurrentRow
        {
            get
            {
                return r;
            }

            set
            {
                r = value;
            }
        }

        public int CurrentColumn
        {
            get
            {
                return c;
            }

            set
            {
                c = value;
            }
        }

        public ThPDSCircuitGraphLayoutEngine()
        {
            this.grid = new Grid();
        }

        public void AddRowDef()
        {
            var rowDef = new RowDefinition()
            { Height = new GridLength(0, GridUnitType.Auto) };
            this.grid.RowDefinitions.Add(rowDef);
        }

        public void AddRowDef_ByPixel(double pixels)
        {
            this.grid.RowDefinitions.Add(new RowDefinition()
            { Height = new GridLength(pixels) });
        }

        public void AddRowDef_ByRatio(double ratio = 1, string shareSizeGroup = null)
        {
            var rolDef = new RowDefinition()
            { Height = new GridLength(ratio, GridUnitType.Star) };
            addRowDef(rolDef, shareSizeGroup);
        }

        public void AddColDef(string shareSizeGroup = null)
        {
            var colDef = new ColumnDefinition()
            { Width = new GridLength(0, GridUnitType.Auto), };
            addColDef(colDef, shareSizeGroup);
        }

        public void AddColDef_ByRatio(double ratio = 1, string shareSizeGroup = null)
        {
            var colDef = new ColumnDefinition()
            { Width = new GridLength(ratio, GridUnitType.Star) };
            addColDef(colDef, shareSizeGroup);
        }

        public void AddColDef_ByPixel(double pixels, string shareSizeGroup = null)
        {
            var colDef = new ColumnDefinition()
            { Width = new GridLength(pixels) };
            addColDef(colDef, shareSizeGroup);
        }

        void addColDef(ColumnDefinition colDef, string shareSizeGroup)
        {
            if (!shareSizeGroup.IsNullOrEmpty())
            {
                colDef.SharedSizeGroup = shareSizeGroup;
            }

            this.grid.ColumnDefinitions.Add(colDef);
        }

        void addRowDef(RowDefinition rowDef, string shareSizeGroup)
        {
            if (!shareSizeGroup.IsNullOrEmpty())
            {
                rowDef.SharedSizeGroup = shareSizeGroup;
            }

            this.grid.RowDefinitions.Add(rowDef);
        }

        public void Add()
        {
            this.Add(null);
        }

        public void Put(UIElement ui, int row, int col, int rowSpan = 1, int colSpan = 1)
        {
            ui.SetValue(Grid.RowProperty, row);
            ui.SetValue(Grid.ColumnProperty, col);
            ui.SetValue(Grid.RowSpanProperty, rowSpan);
            ui.SetValue(Grid.ColumnSpanProperty, colSpan);
        }

        public void Clear()
        {
            this.grid.Children.Clear();
            this.grid.RowDefinitions.Clear();
            this.grid.ColumnDefinitions.Clear();
            this.r = this.c = 0;
        }

        public void Add(UIElement ui, int rowSpan = 1, int colSpan = 1)
        {
            if (ui != null)
            {
                this.grid.Children.Add(ui);
                this.Put(ui, this.r, this.c, rowSpan, colSpan);
            }

            this.c++;
        }

        public void MoveToNextRow()
        {
            this.r++;
            this.c = 0;
        }

        public GridSplitter AddVerticalSpliter(double width, int rowSpan = 1, int colSpan = 1)
        {
            var spliter = new GridSplitter()
            { Width = width, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, };
            this.Add(spliter, rowSpan, colSpan);
            return spliter;
        }

        public GridSplitter AddHorizontalSpliter(double height, int rowSpan = 1, int colSpan = 1)
        {
            var spliter = new GridSplitter()
            { Height = height, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, };
            this.Add(spliter, rowSpan, colSpan);
            return spliter;
        }
    }
}
