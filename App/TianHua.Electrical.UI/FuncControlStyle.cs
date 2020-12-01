using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianHua.Electrical.UI
{
    public static class FuncControlStyle
    {
        
        public static void SetGridEditStyle(GridLookUpEdit _THGdv, bool IsViewEidt = true)
        {
            GridView _Gdv = new GridView();

            GridColumn ColValueMember = new GridColumn();

            GridColumn ColDisplayMember = new GridColumn();

            _THGdv.Properties.NullText = "";

            _THGdv.Properties.PopupSizeable = false;

            _THGdv.Properties.PopupWidthMode = DevExpress.XtraEditors.PopupWidthMode.UseEditorWidth;

            _THGdv.Properties.AutoHeight = false;

            _THGdv.Properties.PopupFormSize = new System.Drawing.Size(80,100);

            //_THGdv.Properties.THScr = Enum筛选模式.ScrLike;

            _THGdv.Properties.NullText = string.Empty;

            _THGdv.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;

            _THGdv.Properties.PopupView = _Gdv;

            _Gdv.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;

            //_Gdv.Appearance.FocusedRow.BackColor = Color.FromArgb(223, 238, 252);

            _Gdv.Name = "Gdv";

            _Gdv.OptionsSelection.EnableAppearanceFocusedCell = false;

            _Gdv.OptionsView.ShowColumnHeaders = false;

            _Gdv.OptionsView.ShowDetailButtons = false;

            _Gdv.OptionsView.ShowGroupPanel = false;

            _Gdv.OptionsView.ShowIndicator = false;

     

            if (IsViewEidt)
            {
                _Gdv.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                 ColValueMember,
                 ColDisplayMember});
                //_THGdv.THFiltercolumn = "%DisplayMember%|%ValueMember%";
                ColValueMember.Caption = "ValueMember";
                ColValueMember.FieldName = "ValueMember";
                ColValueMember.Name = "ColValueMember";
                ColDisplayMember.Caption = "DisplayMember";
                ColDisplayMember.FieldName = "DisplayMember";
                ColDisplayMember.Name = "ColDisplayMember";
                ColDisplayMember.Visible = true;
                ColDisplayMember.VisibleIndex = 0;
            }


        }

        public static void SetGridEditStyle(LookUpEdit _THGdv, bool IsViewEidt = true)
        {
            GridView _Gdv = new GridView();

            GridColumn ColValueMember = new GridColumn();

            GridColumn ColDisplayMember = new GridColumn();

            _THGdv.Properties.NullText = "";

            _THGdv.Properties.PopupSizeable = false;

            _THGdv.Properties.PopupWidthMode = DevExpress.XtraEditors.PopupWidthMode.UseEditorWidth;

            //_THGdv.Properties.PopupFormSize = new System.Drawing.Size(159, 100);

            //_THGdv.Properties.THScr = Enum筛选模式.ScrLike;

            _THGdv.Properties.NullText = string.Empty;

            _THGdv.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.Standard;

            //_THGdv.Properties.PopupView = _Gdv;

            _Gdv.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;

            _Gdv.Appearance.FocusedRow.BackColor = Color.FromArgb(223, 238, 252);

            _Gdv.Name = "Gdv";

            _Gdv.OptionsSelection.EnableAppearanceFocusedCell = false;

            _Gdv.OptionsView.ShowColumnHeaders = false;

            _Gdv.OptionsView.ShowDetailButtons = false;

            _Gdv.OptionsView.ShowGroupPanel = false;

            _Gdv.OptionsView.ShowIndicator = false;



            if (IsViewEidt)
            {
                _Gdv.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
                 ColValueMember,
                 ColDisplayMember});
                //_THGdv.Properties.THFiltercolumn = "%DisplayMember%|%ValueMember%";
                ColValueMember.Caption = "ValueMember";
                ColValueMember.FieldName = "ValueMember";
                ColValueMember.Name = "ColValueMember";
                ColDisplayMember.Caption = "DisplayMember";
                ColDisplayMember.FieldName = "DisplayMember";
                ColDisplayMember.Name = "ColDisplayMember";
                ColDisplayMember.Visible = true;
                ColDisplayMember.VisibleIndex = 0;
            }


        }

    }
}
