namespace TianHua.FanSelection.UI
{
    partial class fmAirVolumeCalc
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
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.Gdc = new DevExpress.XtraGrid.GridControl();
            this.Gdv = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridView();
            this.gridBand1 = new DevExpress.XtraGrid.Views.BandedGrid.GridBand();
            this.ColCalcValue = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn();
            this.TxtAirCalcValue = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.gridBand2 = new DevExpress.XtraGrid.Views.BandedGrid.GridBand();
            this.ColFactor = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn();
            this.TxtAirCalcFactor = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.gridBand3 = new DevExpress.XtraGrid.Views.BandedGrid.GridBand();
            this.ColAirVolume = new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn();
            this.TxtDuctLength = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.TxtFriction = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.TxtLocRes = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.TxtDamper = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.TxtDynPress = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Gdc)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Gdv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAirCalcValue)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAirCalcFactor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDuctLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFriction)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtLocRes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDamper)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDynPress)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.BtnOK);
            this.layoutControl1.Controls.Add(this.Gdc);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(388, 344, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(357, 142);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // BtnOK
            // 
            this.BtnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.BtnOK.Location = new System.Drawing.Point(230, 108);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(115, 22);
            this.BtnOK.StyleController = this.layoutControl1;
            this.BtnOK.TabIndex = 33;
            this.BtnOK.Text = "确定";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // Gdc
            // 
            this.Gdc.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Gdc.Location = new System.Drawing.Point(12, 12);
            this.Gdc.LookAndFeel.SkinName = "Office 2013";
            this.Gdc.MainView = this.Gdv;
            this.Gdc.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.Gdc.Name = "Gdc";
            this.Gdc.Padding = new System.Windows.Forms.Padding(8);
            this.Gdc.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.TxtDuctLength,
            this.TxtFriction,
            this.TxtLocRes,
            this.TxtDamper,
            this.TxtDynPress,
            this.TxtAirCalcValue,
            this.TxtAirCalcFactor});
            this.Gdc.Size = new System.Drawing.Size(333, 92);
            this.Gdc.TabIndex = 32;
            this.Gdc.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.Gdv});
            // 
            // Gdv
            // 
            this.Gdv.Appearance.FocusedCell.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(205)))), ((int)(((byte)(230)))), ((int)(((byte)(247)))));
            this.Gdv.Appearance.FocusedCell.Options.UseBackColor = true;
            this.Gdv.Bands.AddRange(new DevExpress.XtraGrid.Views.BandedGrid.GridBand[] {
            this.gridBand1,
            this.gridBand2,
            this.gridBand3});
            this.Gdv.Columns.AddRange(new DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn[] {
            this.ColCalcValue,
            this.ColFactor,
            this.ColAirVolume});
            this.Gdv.GridControl = this.Gdc;
            this.Gdv.Name = "Gdv";
            this.Gdv.OptionsCustomization.AllowBandMoving = false;
            this.Gdv.OptionsCustomization.AllowBandResizing = false;
            this.Gdv.OptionsCustomization.AllowFilter = false;
            this.Gdv.OptionsCustomization.AllowGroup = false;
            this.Gdv.OptionsCustomization.AllowQuickHideColumns = false;
            this.Gdv.OptionsCustomization.ShowBandsInCustomizationForm = false;
            this.Gdv.OptionsDetail.AllowZoomDetail = false;
            this.Gdv.OptionsDetail.ShowDetailTabs = false;
            this.Gdv.OptionsMenu.EnableColumnMenu = false;
            this.Gdv.OptionsMenu.EnableFooterMenu = false;
            this.Gdv.OptionsMenu.EnableGroupPanelMenu = false;
            this.Gdv.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.Gdv.OptionsSelection.MultiSelect = true;
            this.Gdv.OptionsView.BestFitUseErrorInfo = DevExpress.Utils.DefaultBoolean.True;
            this.Gdv.OptionsView.HeaderFilterButtonShowMode = DevExpress.XtraEditors.Controls.FilterButtonShowMode.Button;
            this.Gdv.OptionsView.ShowColumnHeaders = false;
            this.Gdv.OptionsView.ShowDetailButtons = false;
            this.Gdv.OptionsView.ShowFilterPanelMode = DevExpress.XtraGrid.Views.Base.ShowFilterPanelMode.Never;
            this.Gdv.OptionsView.ShowGroupExpandCollapseButtons = false;
            this.Gdv.OptionsView.ShowGroupPanel = false;
            this.Gdv.OptionsView.ShowIndicator = false;
            this.Gdv.RowHeight = 23;
            this.Gdv.CellValueChanged += new DevExpress.XtraGrid.Views.Base.CellValueChangedEventHandler(this.Gdv_CellValueChanged);
            // 
            // gridBand1
            // 
            this.gridBand1.AppearanceHeader.Options.UseTextOptions = true;
            this.gridBand1.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridBand1.Caption = "计算值\r\n（m³/h）";
            this.gridBand1.Columns.Add(this.ColCalcValue);
            this.gridBand1.Name = "gridBand1";
            this.gridBand1.VisibleIndex = 0;
            this.gridBand1.Width = 75;
            // 
            // ColCalcValue
            // 
            this.ColCalcValue.AppearanceCell.Options.UseTextOptions = true;
            this.ColCalcValue.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColCalcValue.Caption = "计算值";
            this.ColCalcValue.ColumnEdit = this.TxtAirCalcValue;
            this.ColCalcValue.FieldName = "AirCalcValue";
            this.ColCalcValue.Name = "ColCalcValue";
            this.ColCalcValue.Visible = true;
            // 
            // TxtAirCalcValue
            // 
            this.TxtAirCalcValue.AutoHeight = false;
            this.TxtAirCalcValue.Mask.EditMask = "([0-9]{1,})";
            this.TxtAirCalcValue.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtAirCalcValue.Name = "TxtAirCalcValue";
            this.TxtAirCalcValue.Click += new System.EventHandler(this.TxtAirCalcValue_Click);
            // 
            // gridBand2
            // 
            this.gridBand2.AppearanceHeader.Options.UseTextOptions = true;
            this.gridBand2.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridBand2.Caption = "选型系数";
            this.gridBand2.Columns.Add(this.ColFactor);
            this.gridBand2.Name = "gridBand2";
            this.gridBand2.VisibleIndex = 1;
            this.gridBand2.Width = 75;
            // 
            // ColFactor
            // 
            this.ColFactor.AppearanceCell.Options.UseTextOptions = true;
            this.ColFactor.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColFactor.Caption = "选型系数";
            this.ColFactor.ColumnEdit = this.TxtAirCalcFactor;
            this.ColFactor.FieldName = "AirCalcFactor";
            this.ColFactor.Name = "ColFactor";
            this.ColFactor.Visible = true;
            // 
            // TxtAirCalcFactor
            // 
            this.TxtAirCalcFactor.AutoHeight = false;
            this.TxtAirCalcFactor.Mask.EditMask = "\\d*\\.{0,1}\\d{0,2}";
            this.TxtAirCalcFactor.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtAirCalcFactor.Name = "TxtAirCalcFactor";
            // 
            // gridBand3
            // 
            this.gridBand3.AppearanceHeader.Options.UseTextOptions = true;
            this.gridBand3.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.gridBand3.Caption = "风量\r\n（m³/h）";
            this.gridBand3.Columns.Add(this.ColAirVolume);
            this.gridBand3.Name = "gridBand3";
            this.gridBand3.RowCount = 2;
            this.gridBand3.VisibleIndex = 2;
            this.gridBand3.Width = 75;
            // 
            // ColAirVolume
            // 
            this.ColAirVolume.AppearanceCell.Options.UseTextOptions = true;
            this.ColAirVolume.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ColAirVolume.Caption = "风量";
            this.ColAirVolume.FieldName = "AirVolume";
            this.ColAirVolume.Name = "ColAirVolume";
            this.ColAirVolume.OptionsColumn.AllowEdit = false;
            this.ColAirVolume.Visible = true;
            // 
            // TxtDuctLength
            // 
            this.TxtDuctLength.AutoHeight = false;
            this.TxtDuctLength.Mask.EditMask = "([0-9]{1,})";
            this.TxtDuctLength.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtDuctLength.Name = "TxtDuctLength";
            // 
            // TxtFriction
            // 
            this.TxtFriction.AutoHeight = false;
            this.TxtFriction.Mask.EditMask = "\\d*\\.{0,1}\\d{0,1}";
            this.TxtFriction.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtFriction.Name = "TxtFriction";
            // 
            // TxtLocRes
            // 
            this.TxtLocRes.AutoHeight = false;
            this.TxtLocRes.Mask.EditMask = "\\d*\\.{0,1}\\d{0,1}";
            this.TxtLocRes.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtLocRes.Name = "TxtLocRes";
            // 
            // TxtDamper
            // 
            this.TxtDamper.AutoHeight = false;
            this.TxtDamper.Mask.EditMask = "([0-9]{1,})";
            this.TxtDamper.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtDamper.Name = "TxtDamper";
            // 
            // TxtDynPress
            // 
            this.TxtDynPress.AutoHeight = false;
            this.TxtDynPress.Mask.EditMask = "([0-9]{1,})";
            this.TxtDynPress.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtDynPress.Name = "TxtDynPress";
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.layoutControlItem2,
            this.emptySpaceItem1});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Size = new System.Drawing.Size(357, 142);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.Gdc;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(337, 96);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.BtnOK;
            this.layoutControlItem2.Location = new System.Drawing.Point(218, 96);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Size = new System.Drawing.Size(119, 26);
            this.layoutControlItem2.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem2.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 96);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(218, 26);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // fmAirVolumeCalc
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(357, 142);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmAirVolumeCalc";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "风量计算器";
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Gdc)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Gdv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAirCalcValue)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAirCalcFactor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDuctLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFriction)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtLocRes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDamper)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtDynPress)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        public DevExpress.XtraGrid.GridControl Gdc;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridView Gdv;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtDuctLength;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtFriction;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtLocRes;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtDamper;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtDynPress;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private DevExpress.XtraGrid.Views.BandedGrid.GridBand gridBand1;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn ColCalcValue;
        private DevExpress.XtraGrid.Views.BandedGrid.GridBand gridBand2;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn ColFactor;
        private DevExpress.XtraGrid.Views.BandedGrid.GridBand gridBand3;
        private DevExpress.XtraGrid.Views.BandedGrid.BandedGridColumn ColAirVolume;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtAirCalcValue;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit TxtAirCalcFactor;
    }
}