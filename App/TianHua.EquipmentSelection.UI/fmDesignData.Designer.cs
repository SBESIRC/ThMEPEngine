namespace TianHua.FanSelection.UI
{
    partial class fmDesignData
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
            DevExpress.XtraEditors.Controls.EditorButtonImageOptions editorButtonImageOptions1 = new DevExpress.XtraEditors.Controls.EditorButtonImageOptions();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject1 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject2 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject3 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject4 = new DevExpress.Utils.SerializableAppearanceObject();
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.BtnCancel = new DevExpress.XtraEditors.SimpleButton();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.Gdc = new DevExpress.XtraGrid.GridControl();
            this.Gdv = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridView();
            this.gridBand1 = new DevExpress.XtraGrid.Views.BandedGrid.GridBand();
            this.ColName = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn();
            this.ComBoxName = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.gridBand2 = new DevExpress.XtraGrid.Views.BandedGrid.GridBand();
            this.ColLastOperationDate = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn();
            this.ColLastOperationName = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn();
            this.TxtName = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.PicSearch = new DevExpress.XtraEditors.PictureEdit();
            this.TxtSearch = new DevExpress.XtraEditors.TextEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem7 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem2 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem8 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem9 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Gdc)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Gdv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem8)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem9)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.BtnCancel);
            this.layoutControl1.Controls.Add(this.BtnOK);
            this.layoutControl1.Controls.Add(this.Gdc);
            this.layoutControl1.Controls.Add(this.PicSearch);
            this.layoutControl1.Controls.Add(this.TxtSearch);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(783, 59, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(411, 297);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // BtnCancel
            // 
            this.BtnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.BtnCancel.Location = new System.Drawing.Point(284, 265);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(112, 22);
            this.BtnCancel.StyleController = this.layoutControl1;
            this.BtnCancel.TabIndex = 22;
            this.BtnCancel.Text = "取消";
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // BtnOK
            // 
            this.BtnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.BtnOK.Location = new System.Drawing.Point(165, 265);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(109, 22);
            this.BtnOK.StyleController = this.layoutControl1;
            this.BtnOK.TabIndex = 21;
            this.BtnOK.Text = "确定";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // Gdc
            // 
            this.Gdc.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Gdc.Location = new System.Drawing.Point(12, 36);
            this.Gdc.MainView = this.Gdv;
            this.Gdc.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Gdc.Name = "Gdc";
            this.Gdc.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.TxtName,
            this.ComBoxName});
            this.Gdc.Size = new System.Drawing.Size(387, 222);
            this.Gdc.TabIndex = 20;
            this.Gdc.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.Gdv});
            // 
            // Gdv
            // 
            this.Gdv.Appearance.FocusedCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(230)))), ((int)(((byte)(247)))));
            this.Gdv.Appearance.FocusedCell.Options.UseBackColor = true;
            this.Gdv.Bands.AddRange(new DevExpress.XtraGrid.Views.BandedGrid.GridBand[] {
            this.gridBand1,
            this.gridBand2});
            this.Gdv.Columns.AddRange(new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn[] {
            this.ColName,
            this.ColLastOperationDate,
            this.ColLastOperationName});
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
            this.Gdv.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.Gdv.OptionsView.ShowColumnHeaders = false;
            this.Gdv.OptionsView.ShowDetailButtons = false;
            this.Gdv.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;
            this.Gdv.OptionsView.ShowGroupPanel = false;
            this.Gdv.OptionsView.ShowIndicator = false;
            this.Gdv.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
            this.Gdv.RowHeight = 23;
            this.Gdv.SortInfo.AddRange(new DevExpress.XtraGrid.Columns.GridColumnSortInfo[] {
            new DevExpress.XtraGrid.Columns.GridColumnSortInfo(this.ColLastOperationDate, DevExpress.Data.ColumnSortOrder.Descending)});
            this.Gdv.ShowingEditor += new System.ComponentModel.CancelEventHandler(this.Gdv_ShowingEditor);
            this.Gdv.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.Gdv_CellValueChanged);
            this.Gdv.ValidatingEditor += new DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventHandler(this.Gdv_ValidatingEditor);
            // 
            // gridBand1
            // 
            this.gridBand1.AppearanceHeader.Options.UseTextOptions = true;
            this.gridBand1.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridBand1.Caption = "名称";
            this.gridBand1.Columns.Add(this.ColName);
            this.gridBand1.Name = "gridBand1";
            this.gridBand1.VisibleIndex = 0;
            this.gridBand1.Width = 107;
            // 
            // ColName
            // 
            this.ColName.AppearanceHeader.Options.UseTextOptions = true;
            this.ColName.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColName.Caption = "名称";
            this.ColName.ColumnEdit = this.ComBoxName;
            this.ColName.FieldName = "Name";
            this.ColName.Name = "ColName";
            this.ColName.Visible = true;
            this.ColName.Width = 107;
            // 
            // ComBoxName
            // 
            this.ComBoxName.AutoHeight = false;
            editorButtonImageOptions1.Image = global::TianHua.FanSelection.UI.Properties.Resources.删除;
            editorButtonImageOptions1.Location = DevExpress.XtraEditors.ImageLocation.Default;
            this.ComBoxName.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph, "", -1, true, true, true, editorButtonImageOptions1, new DevExpress.Utils.KeyShortcut(System.Windows.Forms.Keys.None), serializableAppearanceObject1, serializableAppearanceObject2, serializableAppearanceObject3, serializableAppearanceObject4, "", "Delete", null, DevExpress.Utils.ToolTipAnchor.Default)});
            this.ComBoxName.Name = "ComBoxName";
            this.ComBoxName.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.ComBoxName_ButtonClick);
            // 
            // gridBand2
            // 
            this.gridBand2.AppearanceHeader.Options.UseTextOptions = true;
            this.gridBand2.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridBand2.Caption = "时间";
            this.gridBand2.Columns.Add(this.ColLastOperationDate);
            this.gridBand2.Name = "gridBand2";
            this.gridBand2.VisibleIndex = 1;
            this.gridBand2.Width = 107;
            // 
            // ColLastOperationDate
            // 
            this.ColLastOperationDate.AppearanceCell.Options.UseTextOptions = true;
            this.ColLastOperationDate.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColLastOperationDate.AppearanceHeader.Options.UseTextOptions = true;
            this.ColLastOperationDate.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColLastOperationDate.Caption = "时间";
            this.ColLastOperationDate.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm:ss";
            this.ColLastOperationDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.ColLastOperationDate.FieldName = "LastOperationDate";
            this.ColLastOperationDate.Name = "ColLastOperationDate";
            this.ColLastOperationDate.OptionsColumn.AllowEdit = false;
            this.ColLastOperationDate.SortMode = DevExpress.XtraGrid.ColumnSortMode.Value;
            this.ColLastOperationDate.Visible = true;
            this.ColLastOperationDate.Width = 107;
            // 
            // ColLastOperationName
            // 
            this.ColLastOperationName.AppearanceCell.Options.UseTextOptions = true;
            this.ColLastOperationName.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColLastOperationName.AppearanceHeader.Options.UseTextOptions = true;
            this.ColLastOperationName.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColLastOperationName.Caption = "作者";
            this.ColLastOperationName.FieldName = "LastOperationName";
            this.ColLastOperationName.Name = "ColLastOperationName";
            this.ColLastOperationName.OptionsColumn.AllowEdit = false;
            this.ColLastOperationName.OptionsFilter.AllowAutoFilter = false;
            this.ColLastOperationName.Width = 109;
            // 
            // TxtName
            // 
            this.TxtName.AutoHeight = false;
            this.TxtName.Name = "TxtName";
            // 
            // PicSearch
            // 
            this.PicSearch.Cursor = System.Windows.Forms.Cursors.Default;
            this.PicSearch.EditValue = global::TianHua.FanSelection.UI.Properties.Resources.搜索;
            this.PicSearch.Location = new System.Drawing.Point(180, 12);
            this.PicSearch.Name = "PicSearch";
            this.PicSearch.Properties.AllowFocused = false;
            this.PicSearch.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.PicSearch.Properties.Appearance.Options.UseBackColor = true;
            this.PicSearch.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.PicSearch.Properties.NullText = " ";
            this.PicSearch.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.PicSearch.Size = new System.Drawing.Size(20, 20);
            this.PicSearch.StyleController = this.layoutControl1;
            this.PicSearch.TabIndex = 6;
            // 
            // TxtSearch
            // 
            this.TxtSearch.Location = new System.Drawing.Point(39, 12);
            this.TxtSearch.Name = "TxtSearch";
            this.TxtSearch.Properties.AllowFocused = false;
            this.TxtSearch.Size = new System.Drawing.Size(141, 20);
            this.TxtSearch.StyleController = this.layoutControl1;
            this.TxtSearch.TabIndex = 5;
            this.TxtSearch.EditValueChanging += new DevExpress.XtraEditors.Controls.ChangingEventHandler(this.TxtSearch_EditValueChanging);
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem2,
            this.layoutControlItem3,
            this.emptySpaceItem1,
            this.layoutControlItem7,
            this.emptySpaceItem2,
            this.layoutControlItem8,
            this.layoutControlItem9});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 5);
            this.layoutControlGroup1.Size = new System.Drawing.Size(411, 297);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.TxtSearch;
            this.layoutControlItem2.ControlAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.layoutControlItem2.CustomizationFormText = "layoutControlItem2";
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(2, 0, 2, 2);
            this.layoutControlItem2.Size = new System.Drawing.Size(170, 24);
            this.layoutControlItem2.Text = "搜索";
            this.layoutControlItem2.TextSize = new System.Drawing.Size(24, 14);
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.PicSearch;
            this.layoutControlItem3.Location = new System.Drawing.Point(170, 0);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Padding = new DevExpress.XtraLayout.Utils.Padding(0, 2, 2, 2);
            this.layoutControlItem3.Size = new System.Drawing.Size(22, 24);
            this.layoutControlItem3.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem3.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(192, 0);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(199, 24);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItem7
            // 
            this.layoutControlItem7.Control = this.Gdc;
            this.layoutControlItem7.Location = new System.Drawing.Point(0, 24);
            this.layoutControlItem7.Name = "layoutControlItem7";
            this.layoutControlItem7.Size = new System.Drawing.Size(391, 226);
            this.layoutControlItem7.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem7.TextVisible = false;
            // 
            // emptySpaceItem2
            // 
            this.emptySpaceItem2.AllowHotTrack = false;
            this.emptySpaceItem2.Location = new System.Drawing.Point(0, 250);
            this.emptySpaceItem2.Name = "emptySpaceItem2";
            this.emptySpaceItem2.Size = new System.Drawing.Size(150, 32);
            this.emptySpaceItem2.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItem8
            // 
            this.layoutControlItem8.Control = this.BtnOK;
            this.layoutControlItem8.Location = new System.Drawing.Point(150, 250);
            this.layoutControlItem8.Name = "layoutControlItem8";
            this.layoutControlItem8.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem8.Size = new System.Drawing.Size(119, 32);
            this.layoutControlItem8.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem8.TextVisible = false;
            // 
            // layoutControlItem9
            // 
            this.layoutControlItem9.Control = this.BtnCancel;
            this.layoutControlItem9.Location = new System.Drawing.Point(269, 250);
            this.layoutControlItem9.Name = "layoutControlItem9";
            this.layoutControlItem9.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem9.Size = new System.Drawing.Size(122, 32);
            this.layoutControlItem9.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem9.TextVisible = false;
            // 
            // fmDesignData
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(411, 297);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmDesignData";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "设计数据";
            this.Load += new System.EventHandler(this.fmDesignData_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Gdc)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Gdv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtName)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtSearch.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem8)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem9)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraEditors.PictureEdit PicSearch;
        private DevExpress.XtraEditors.TextEdit TxtSearch;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraGrid.GridControl Gdc;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem7;
        private DevExpress.XtraEditors.SimpleButton BtnCancel;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem8;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem9;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem2;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridView Gdv;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn ColName;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn ColLastOperationDate;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn ColLastOperationName;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox ComBoxName;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtName;
        private DevExpress.XtraGrid.Views.BandedGrid.GridBand gridBand1;
        private DevExpress.XtraGrid.Views.BandedGrid.GridBand gridBand2;
    }
}