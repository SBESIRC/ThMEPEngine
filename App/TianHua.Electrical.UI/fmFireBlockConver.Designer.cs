namespace TianHua.Electrical.UI
{
    partial class fmFireBlockConver
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.Gdc = new DevExpress.XtraGrid.GridControl();
            this.Gdv = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.ColNo = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColIsSelect = new DevExpress.XtraGrid.Columns.GridColumn();
            this.CheckIsSelect = new DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit();
            this.ColUpstreamRealName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColUpstreamIcon = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PictureEdit = new DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit();
            this.ColVisibility = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDownstreamRealName = new DevExpress.XtraGrid.Columns.GridColumn();
            this.ColDownstreamIcon = new DevExpress.XtraGrid.Columns.GridColumn();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.ComBoxProportion = new DevExpress.XtraEditors.ComboBoxEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.simpleLabelItem1 = new DevExpress.XtraLayout.SimpleLabelItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Gdc)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Gdv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CheckIsSelect)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureEdit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxProportion.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleLabelItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.Gdc);
            this.layoutControl1.Controls.Add(this.BtnCancel);
            this.layoutControl1.Controls.Add(this.BtnOK);
            this.layoutControl1.Controls.Add(this.ComBoxProportion);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(695, 340, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(517, 501);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // Gdc
            // 
            this.Gdc.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Gdc.Location = new System.Drawing.Point(10, 40);
            this.Gdc.MainView = this.Gdv;
            this.Gdc.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Gdc.Name = "Gdc";
            this.Gdc.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.PictureEdit,
            this.CheckIsSelect});
            this.Gdc.Size = new System.Drawing.Size(497, 419);
            this.Gdc.TabIndex = 23;
            this.Gdc.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.Gdv});
            // 
            // Gdv
            // 
            this.Gdv.Appearance.FocusedRow.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(223)))), ((int)(((byte)(238)))), ((int)(((byte)(252)))));
            this.Gdv.Appearance.FocusedRow.Options.UseBackColor = true;
            this.Gdv.ColumnPanelRowHeight = 25;
            this.Gdv.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.ColNo,
            this.ColIsSelect,
            this.ColUpstreamRealName,
            this.ColUpstreamIcon,
            this.ColVisibility,
            this.ColDownstreamRealName,
            this.ColDownstreamIcon});
            this.Gdv.FocusRectStyle = DevExpress.XtraGrid.Views.Grid.DrawFocusRectStyle.RowFocus;
            this.Gdv.GridControl = this.Gdc;
            this.Gdv.Name = "Gdv";
            this.Gdv.OptionsCustomization.AllowColumnMoving = false;
            this.Gdv.OptionsCustomization.AllowFilter = false;
            this.Gdv.OptionsCustomization.AllowGroup = false;
            this.Gdv.OptionsCustomization.AllowQuickHideColumns = false;
            this.Gdv.OptionsCustomization.AllowSort = false;
            this.Gdv.OptionsDetail.AllowZoomDetail = false;
            this.Gdv.OptionsDetail.ShowDetailTabs = false;
            this.Gdv.OptionsMenu.EnableColumnMenu = false;
            this.Gdv.OptionsMenu.EnableFooterMenu = false;
            this.Gdv.OptionsMenu.EnableGroupPanelMenu = false;
            this.Gdv.OptionsNavigation.AutoFocusNewRow = true;
            this.Gdv.OptionsNavigation.EnterMoveNextColumn = true;
            this.Gdv.OptionsSelection.CheckBoxSelectorColumnWidth = 55;
            this.Gdv.OptionsSelection.EnableAppearanceFocusedCell = false;
            this.Gdv.OptionsSelection.MultiSelect = true;
            this.Gdv.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;
            this.Gdv.OptionsView.ShowDetailButtons = false;
            this.Gdv.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;
            this.Gdv.OptionsView.ShowGroupPanel = false;
            this.Gdv.OptionsView.ShowIndicator = false;
            this.Gdv.RowHeight = 45;
            //this.Gdv.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.Gdv_CellValueChanged);
            // 
            // ColNo
            // 
            this.ColNo.AppearanceCell.Options.UseTextOptions = true;
            this.ColNo.AppearanceHeader.Options.UseTextOptions = true;
            this.ColNo.Caption = "序号";
            this.ColNo.FieldName = "No";
            this.ColNo.Name = "ColNo";
            this.ColNo.OptionsColumn.AllowEdit = false;
            this.ColNo.OptionsColumn.AllowMove = false;
            this.ColNo.Width = 52;
            // 
            // ColIsSelect
            // 
            this.ColIsSelect.AppearanceCell.Options.UseTextOptions = true;
            this.ColIsSelect.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColIsSelect.AppearanceHeader.Options.UseTextOptions = true;
            this.ColIsSelect.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColIsSelect.Caption = "选择";
            this.ColIsSelect.ColumnEdit = this.CheckIsSelect;
            this.ColIsSelect.FieldName = "IsSelect";
            this.ColIsSelect.Name = "ColIsSelect";
            this.ColIsSelect.Width = 55;
            // 
            // CheckIsSelect
            // 
            this.CheckIsSelect.AutoHeight = false;
            this.CheckIsSelect.Name = "CheckIsSelect";
            // 
            // ColUpstreamRealName
            // 
            this.ColUpstreamRealName.AppearanceCell.Options.UseTextOptions = true;
            this.ColUpstreamRealName.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColUpstreamRealName.AppearanceHeader.Options.UseTextOptions = true;
            this.ColUpstreamRealName.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColUpstreamRealName.Caption = "源图块名";
            this.ColUpstreamRealName.FieldName = "UpstreamRealName";
            this.ColUpstreamRealName.Name = "ColUpstreamRealName";
            this.ColUpstreamRealName.OptionsColumn.AllowEdit = false;
            this.ColUpstreamRealName.Visible = true;
            this.ColUpstreamRealName.VisibleIndex = 1;
            this.ColUpstreamRealName.Width = 110;
            // 
            // ColUpstreamIcon
            // 
            this.ColUpstreamIcon.AppearanceCell.Options.UseTextOptions = true;
            this.ColUpstreamIcon.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColUpstreamIcon.AppearanceHeader.Options.UseTextOptions = true;
            this.ColUpstreamIcon.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColUpstreamIcon.Caption = "源图块示";
            this.ColUpstreamIcon.ColumnEdit = this.PictureEdit;
            this.ColUpstreamIcon.FieldName = "UpstreamIcon";
            this.ColUpstreamIcon.Name = "ColUpstreamIcon";
            this.ColUpstreamIcon.OptionsColumn.AllowEdit = false;
            this.ColUpstreamIcon.Visible = true;
            this.ColUpstreamIcon.VisibleIndex = 2;
            this.ColUpstreamIcon.Width = 73;
            // 
            // PictureEdit
            // 
            this.PictureEdit.Name = "PictureEdit";
            this.PictureEdit.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            // 
            // ColVisibility
            // 
            this.ColVisibility.AppearanceCell.Options.UseTextOptions = true;
            this.ColVisibility.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColVisibility.AppearanceHeader.Options.UseTextOptions = true;
            this.ColVisibility.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColVisibility.Caption = "可见性";
            this.ColVisibility.FieldName = "Visibility";
            this.ColVisibility.Name = "ColVisibility";
            this.ColVisibility.OptionsColumn.AllowEdit = false;
            this.ColVisibility.Visible = true;
            this.ColVisibility.VisibleIndex = 3;
            // 
            // ColDownstreamRealName
            // 
            this.ColDownstreamRealName.AppearanceCell.Options.UseTextOptions = true;
            this.ColDownstreamRealName.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColDownstreamRealName.AppearanceHeader.Options.UseTextOptions = true;
            this.ColDownstreamRealName.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColDownstreamRealName.Caption = "目标图块名";
            this.ColDownstreamRealName.FieldName = "DownstreamRealName";
            this.ColDownstreamRealName.Name = "ColDownstreamRealName";
            this.ColDownstreamRealName.OptionsColumn.AllowEdit = false;
            this.ColDownstreamRealName.Visible = true;
            this.ColDownstreamRealName.VisibleIndex = 4;
            this.ColDownstreamRealName.Width = 107;
            // 
            // ColDownstreamIcon
            // 
            this.ColDownstreamIcon.AppearanceCell.Options.UseTextOptions = true;
            this.ColDownstreamIcon.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColDownstreamIcon.AppearanceHeader.Options.UseTextOptions = true;
            this.ColDownstreamIcon.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColDownstreamIcon.Caption = "目标图块示";
            this.ColDownstreamIcon.ColumnEdit = this.PictureEdit;
            this.ColDownstreamIcon.FieldName = "DownstreamIcon";
            this.ColDownstreamIcon.Name = "ColDownstreamIcon";
            this.ColDownstreamIcon.OptionsColumn.AllowEdit = false;
            this.ColDownstreamIcon.Visible = true;
            this.ColDownstreamIcon.VisibleIndex = 5;
            this.ColDownstreamIcon.Width = 74;
            // 
            // BtnCancel
            // 
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(408, 469);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(99, 22);
            this.BtnCancel.StyleController = this.layoutControl1;
            this.BtnCancel.TabIndex = 22;
            this.BtnCancel.Text = "取消";
            // 
            // BtnOK
            // 
            this.BtnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.BtnOK.Location = new System.Drawing.Point(295, 469);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(103, 22);
            this.BtnOK.StyleController = this.layoutControl1;
            this.BtnOK.TabIndex = 21;
            this.BtnOK.Text = "确定";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // ComBoxProportion
            // 
            this.ComBoxProportion.EditValue = "1:100";
            this.ComBoxProportion.Location = new System.Drawing.Point(433, 10);
            this.ComBoxProportion.Name = "ComBoxProportion";
            this.ComBoxProportion.Properties.AllowFocused = false;
            this.ComBoxProportion.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.ComBoxProportion.Properties.Items.AddRange(new object[] {
            "1:25",
            "1:50",
            "1:100",
            "1:150",
            "1:200"});
            this.ComBoxProportion.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.ComBoxProportion.Size = new System.Drawing.Size(74, 20);
            this.ComBoxProportion.StyleController = this.layoutControl1;
            this.ComBoxProportion.TabIndex = 4;
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.simpleLabelItem1,
            this.layoutControlItem2,
            this.layoutControlItem4,
            this.emptySpaceItem1,
            this.layoutControlItem1,
            this.layoutControlItem3});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlGroup1.Size = new System.Drawing.Size(517, 501);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // simpleLabelItem1
            // 
            this.simpleLabelItem1.AllowHotTrack = false;
            this.simpleLabelItem1.Location = new System.Drawing.Point(0, 0);
            this.simpleLabelItem1.Name = "simpleLabelItem1";
            this.simpleLabelItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.simpleLabelItem1.Size = new System.Drawing.Size(360, 30);
            this.simpleLabelItem1.Text = "转换规则：";
            this.simpleLabelItem1.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.BtnOK;
            this.layoutControlItem2.Location = new System.Drawing.Point(285, 459);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem2.Size = new System.Drawing.Size(113, 32);
            this.layoutControlItem2.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem2.TextVisible = false;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.BtnCancel;
            this.layoutControlItem4.Location = new System.Drawing.Point(398, 459);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem4.Size = new System.Drawing.Size(109, 32);
            this.layoutControlItem4.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem4.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 526);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.emptySpaceItem1.Size = new System.Drawing.Size(285, 32);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.ComBoxProportion;
            this.layoutControlItem1.Location = new System.Drawing.Point(360, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem1.Size = new System.Drawing.Size(147, 30);
            this.layoutControlItem1.Text = "出图比例：";
            this.layoutControlItem1.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.Gdc;
            this.layoutControlItem3.Location = new System.Drawing.Point(0, 30);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem3.Size = new System.Drawing.Size(507, 429);
            this.layoutControlItem3.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem3.TextVisible = false;
            // 
            // fmFireBlockConver
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(517, 501);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.LookAndFeel.SkinName = "The Bezier";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmFireBlockConver";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "消防联动平面提资转换";
            this.Load += new System.EventHandler(this.fmFireBlockConver_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Gdc)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Gdv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CheckIsSelect)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureEdit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxProportion.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleLabelItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraEditors.ComboBoxEdit ComBoxProportion;
        private DevExpress.XtraLayout.SimpleLabelItem simpleLabelItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraGrid.GridControl Gdc;
        public DevExpress.XtraGrid.Views.Grid.GridView Gdv;
        private DevExpress.XtraGrid.Columns.GridColumn ColNo;
        private DevExpress.XtraGrid.Columns.GridColumn ColIsSelect;
        private DevExpress.XtraGrid.Columns.GridColumn ColUpstreamRealName;
        private DevExpress.XtraGrid.Columns.GridColumn ColUpstreamIcon;
        private DevExpress.XtraEditors.Repository.RepositoryItemPictureEdit PictureEdit;
        private DevExpress.XtraGrid.Columns.GridColumn ColVisibility;
        private DevExpress.XtraGrid.Columns.GridColumn ColDownstreamRealName;
        private DevExpress.XtraGrid.Columns.GridColumn ColDownstreamIcon;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraEditors.Repository.RepositoryItemCheckEdit CheckIsSelect;
    }
}