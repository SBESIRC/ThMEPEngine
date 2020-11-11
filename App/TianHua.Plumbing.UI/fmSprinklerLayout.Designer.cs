namespace TianHua.Plumbing.UI
{
    partial class fmSprinklerLayout
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
            this.RidSprinklerType = new DevExpress.XtraEditors.RadioGroup();
            this.RidSprinklerScope = new DevExpress.XtraEditors.RadioGroup();
            this.ComBoxHazardLevel = new DevExpress.XtraEditors.ComboBoxEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.PicWrite = new DevExpress.XtraEditors.PictureEdit();
            this.PicLayout = new DevExpress.XtraEditors.PictureEdit();
            this.PicCheck = new DevExpress.XtraEditors.PictureEdit();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            this.CheckGirder = new System.Windows.Forms.CheckBox();
            this.layoutControlItem7 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RidSprinklerType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RidSprinklerScope.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxHazardLevel.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicWrite.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicLayout.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicCheck.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.CheckGirder);
            this.layoutControl1.Controls.Add(this.PicWrite);
            this.layoutControl1.Controls.Add(this.PicLayout);
            this.layoutControl1.Controls.Add(this.PicCheck);
            this.layoutControl1.Controls.Add(this.RidSprinklerType);
            this.layoutControl1.Controls.Add(this.RidSprinklerScope);
            this.layoutControl1.Controls.Add(this.ComBoxHazardLevel);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(833, 209, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(298, 230);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // RidSprinklerType
            // 
            this.RidSprinklerType.EditValue = "上喷";
            this.RidSprinklerType.Location = new System.Drawing.Point(83, 100);
            this.RidSprinklerType.Name = "RidSprinklerType";
            this.RidSprinklerType.Properties.AllowFocused = false;
            this.RidSprinklerType.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.RidSprinklerType.Properties.Appearance.Options.UseBackColor = true;
            this.RidSprinklerType.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RidSprinklerType.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("上喷", "上喷"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("下喷", "下喷")});
            this.RidSprinklerType.Size = new System.Drawing.Size(195, 20);
            this.RidSprinklerType.StyleController = this.layoutControl1;
            this.RidSprinklerType.TabIndex = 6;
            // 
            // RidSprinklerScope
            // 
            this.RidSprinklerScope.EditValue = "标准覆盖";
            this.RidSprinklerScope.Location = new System.Drawing.Point(83, 60);
            this.RidSprinklerScope.Name = "RidSprinklerScope";
            this.RidSprinklerScope.Properties.AllowFocused = false;
            this.RidSprinklerScope.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.RidSprinklerScope.Properties.Appearance.Options.UseBackColor = true;
            this.RidSprinklerScope.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.RidSprinklerScope.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("标准覆盖", "标准覆盖"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("扩大覆盖", "扩大覆盖")});
            this.RidSprinklerScope.Size = new System.Drawing.Size(195, 20);
            this.RidSprinklerScope.StyleController = this.layoutControl1;
            this.RidSprinklerScope.TabIndex = 5;
            // 
            // ComBoxHazardLevel
            // 
            this.ComBoxHazardLevel.EditValue = "中危险等级II级";
            this.ComBoxHazardLevel.Location = new System.Drawing.Point(83, 20);
            this.ComBoxHazardLevel.Name = "ComBoxHazardLevel";
            this.ComBoxHazardLevel.Properties.AllowFocused = false;
            this.ComBoxHazardLevel.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.ComBoxHazardLevel.Properties.Items.AddRange(new object[] {
            "轻危险级",
            "中危险等级I级",
            "中危险等级II级",
            "严重危险级、仓库危险级"});
            this.ComBoxHazardLevel.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.ComBoxHazardLevel.Size = new System.Drawing.Size(195, 20);
            this.ComBoxHazardLevel.StyleController = this.layoutControl1;
            this.ComBoxHazardLevel.TabIndex = 4;
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.layoutControlItem2,
            this.layoutControlItem3,
            this.layoutControlItem5,
            this.layoutControlItem4,
            this.layoutControlItem6,
            this.layoutControlItem7,
            this.emptySpaceItem1});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Size = new System.Drawing.Size(298, 230);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.ComBoxHazardLevel;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.MaxSize = new System.Drawing.Size(278, 40);
            this.layoutControlItem1.MinSize = new System.Drawing.Size(278, 40);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 10);
            this.layoutControlItem1.Size = new System.Drawing.Size(278, 40);
            this.layoutControlItem1.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem1.Text = "危险等级：";
            this.layoutControlItem1.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.RidSprinklerScope;
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 40);
            this.layoutControlItem2.MaxSize = new System.Drawing.Size(278, 40);
            this.layoutControlItem2.MinSize = new System.Drawing.Size(278, 40);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 10);
            this.layoutControlItem2.Size = new System.Drawing.Size(278, 40);
            this.layoutControlItem2.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem2.Text = "喷头范围：";
            this.layoutControlItem2.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.RidSprinklerType;
            this.layoutControlItem3.Location = new System.Drawing.Point(0, 80);
            this.layoutControlItem3.MaxSize = new System.Drawing.Size(278, 40);
            this.layoutControlItem3.MinSize = new System.Drawing.Size(278, 40);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 10);
            this.layoutControlItem3.Size = new System.Drawing.Size(278, 40);
            this.layoutControlItem3.SizeConstraintsType = DevExpress.XtraLayout.SizeConstraintsType.Custom;
            this.layoutControlItem3.Text = "喷头类型：";
            this.layoutControlItem3.TextSize = new System.Drawing.Size(60, 14);
            // 
            // PicWrite
            // 
            this.PicWrite.Cursor = System.Windows.Forms.Cursors.Default;
            this.PicWrite.EditValue = global::TianHua.Plumbing.UI.Properties.Resources.可布区域;
            this.PicWrite.Location = new System.Drawing.Point(205, 170);
            this.PicWrite.Name = "PicWrite";
            this.PicWrite.Properties.AllowFocused = false;
            this.PicWrite.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.PicWrite.Properties.Appearance.Options.UseBackColor = true;
            this.PicWrite.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003;
            this.PicWrite.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.PicWrite.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.PicWrite.Size = new System.Drawing.Size(73, 40);
            this.PicWrite.StyleController = this.layoutControl1;
            this.PicWrite.TabIndex = 9;
            this.PicWrite.Click += new System.EventHandler(this.PicWrite_Click);
            // 
            // PicLayout
            // 
            this.PicLayout.Cursor = System.Windows.Forms.Cursors.Default;
            this.PicLayout.EditValue = global::TianHua.Plumbing.UI.Properties.Resources.布置喷头;
            this.PicLayout.Location = new System.Drawing.Point(20, 170);
            this.PicLayout.Name = "PicLayout";
            this.PicLayout.Properties.AllowFocused = false;
            this.PicLayout.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.PicLayout.Properties.Appearance.Options.UseBackColor = true;
            this.PicLayout.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003;
            this.PicLayout.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.PicLayout.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.PicLayout.Size = new System.Drawing.Size(73, 40);
            this.PicLayout.StyleController = this.layoutControl1;
            this.PicLayout.TabIndex = 8;
            this.PicLayout.Click += new System.EventHandler(this.PicLayout_Click);
            // 
            // PicCheck
            // 
            this.PicCheck.Cursor = System.Windows.Forms.Cursors.Default;
            this.PicCheck.EditValue = global::TianHua.Plumbing.UI.Properties.Resources.盲区检测;
            this.PicCheck.Location = new System.Drawing.Point(113, 170);
            this.PicCheck.Name = "PicCheck";
            this.PicCheck.Properties.AllowFocused = false;
            this.PicCheck.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.PicCheck.Properties.Appearance.Options.UseBackColor = true;
            this.PicCheck.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Office2003;
            this.PicCheck.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.PicCheck.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.PicCheck.Size = new System.Drawing.Size(72, 40);
            this.PicCheck.StyleController = this.layoutControl1;
            this.PicCheck.TabIndex = 7;
            this.PicCheck.Click += new System.EventHandler(this.PicCheck_Click);
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.PicLayout;
            this.layoutControlItem5.Location = new System.Drawing.Point(0, 150);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 10);
            this.layoutControlItem5.Size = new System.Drawing.Size(93, 60);
            this.layoutControlItem5.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem5.TextVisible = false;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.PicCheck;
            this.layoutControlItem4.Location = new System.Drawing.Point(93, 150);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 10);
            this.layoutControlItem4.Size = new System.Drawing.Size(92, 60);
            this.layoutControlItem4.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem4.TextVisible = false;
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.PicWrite;
            this.layoutControlItem6.Location = new System.Drawing.Point(185, 150);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Padding = new DevExpress.XtraLayout.Utils.Padding(10, 10, 10, 10);
            this.layoutControlItem6.Size = new System.Drawing.Size(93, 60);
            this.layoutControlItem6.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem6.TextVisible = false;
            // 
            // CheckGirder
            // 
            this.CheckGirder.AutoCheck = false;
            this.CheckGirder.Checked = true;
            this.CheckGirder.CheckState = System.Windows.Forms.CheckState.Checked;
            this.CheckGirder.Location = new System.Drawing.Point(88, 135);
            this.CheckGirder.Name = "CheckGirder";
            this.CheckGirder.Size = new System.Drawing.Size(195, 20);
            this.CheckGirder.TabIndex = 10;
            this.CheckGirder.Text = "考虑梁";
            this.CheckGirder.UseVisualStyleBackColor = true;
            // 
            // layoutControlItem7
            // 
            this.layoutControlItem7.Control = this.CheckGirder;
            this.layoutControlItem7.Location = new System.Drawing.Point(73, 120);
            this.layoutControlItem7.Name = "layoutControlItem7";
            this.layoutControlItem7.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem7.Size = new System.Drawing.Size(205, 30);
            this.layoutControlItem7.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem7.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 120);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Size = new System.Drawing.Size(73, 30);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // fmSprinklerLayout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 230);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmSprinklerLayout";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "喷头布置";
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RidSprinklerType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RidSprinklerScope.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxHazardLevel.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicWrite.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicLayout.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicCheck.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraEditors.PictureEdit PicWrite;
        private DevExpress.XtraEditors.PictureEdit PicLayout;
        private DevExpress.XtraEditors.PictureEdit PicCheck;
        private DevExpress.XtraEditors.RadioGroup RidSprinklerType;
        private DevExpress.XtraEditors.RadioGroup RidSprinklerScope;
        private DevExpress.XtraEditors.ComboBoxEdit ComBoxHazardLevel;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
        private System.Windows.Forms.CheckBox CheckGirder;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem7;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
    }
}