namespace TianHua.Plumbing.UI
{
    partial class fmFloorDrain
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
            this.BtnUse = new DevExpress.XtraEditors.SimpleButton();
            this.BtnLayoutRiser = new DevExpress.XtraEditors.SimpleButton();
            this.ListBox = new DevExpress.XtraEditors.ListBoxControl();
            this.BtnParam = new DevExpress.XtraEditors.SimpleButton();
            this.BtnGetFloor = new DevExpress.XtraEditors.SimpleButton();
            this.BtnFloorFocus = new DevExpress.XtraEditors.SimpleButton();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ListBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.BtnUse);
            this.layoutControl1.Controls.Add(this.BtnLayoutRiser);
            this.layoutControl1.Controls.Add(this.ListBox);
            this.layoutControl1.Controls.Add(this.BtnParam);
            this.layoutControl1.Controls.Add(this.BtnGetFloor);
            this.layoutControl1.Controls.Add(this.BtnFloorFocus);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(734, 124, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(208, 423);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // BtnUse
            // 
            this.BtnUse.AllowFocus = false;
            this.BtnUse.Location = new System.Drawing.Point(109, 388);
            this.BtnUse.Name = "BtnUse";
            this.BtnUse.Size = new System.Drawing.Size(86, 22);
            this.BtnUse.StyleController = this.layoutControl1;
            this.BtnUse.TabIndex = 9;
            this.BtnUse.Text = "应用";
            this.BtnUse.Click += new System.EventHandler(this.BtnUse_Click);
            // 
            // BtnLayoutRiser
            // 
            this.BtnLayoutRiser.AllowFocus = false;
            this.BtnLayoutRiser.Location = new System.Drawing.Point(13, 388);
            this.BtnLayoutRiser.Name = "BtnLayoutRiser";
            this.BtnLayoutRiser.Size = new System.Drawing.Size(86, 22);
            this.BtnLayoutRiser.StyleController = this.layoutControl1;
            this.BtnLayoutRiser.TabIndex = 8;
            this.BtnLayoutRiser.Text = "布置立管";
            this.BtnLayoutRiser.Click += new System.EventHandler(this.BtnLayoutRiser_Click);
            // 
            // ListBox
            // 
            this.ListBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.ListBox.Location = new System.Drawing.Point(13, 78);
            this.ListBox.Name = "ListBox";
            this.ListBox.Size = new System.Drawing.Size(182, 300);
            this.ListBox.StyleController = this.layoutControl1;
            this.ListBox.TabIndex = 7;
            this.ListBox.SelectedIndexChanged += new System.EventHandler(this.ListBox_SelectedIndexChanged);
            // 
            // BtnParam
            // 
            this.BtnParam.AllowFocus = false;
            this.BtnParam.Location = new System.Drawing.Point(109, 13);
            this.BtnParam.MinimumSize = new System.Drawing.Size(0, 55);
            this.BtnParam.Name = "BtnParam";
            this.BtnParam.Size = new System.Drawing.Size(86, 55);
            this.BtnParam.StyleController = this.layoutControl1;
            this.BtnParam.TabIndex = 6;
            this.BtnParam.Text = "参数设置";
            this.BtnParam.Click += new System.EventHandler(this.BtnParam_Click);
            // 
            // BtnGetFloor
            // 
            this.BtnGetFloor.AllowFocus = false;
            this.BtnGetFloor.Location = new System.Drawing.Point(13, 45);
            this.BtnGetFloor.Name = "BtnGetFloor";
            this.BtnGetFloor.Size = new System.Drawing.Size(86, 22);
            this.BtnGetFloor.StyleController = this.layoutControl1;
            this.BtnGetFloor.TabIndex = 5;
            this.BtnGetFloor.Text = "读取楼层";
            this.BtnGetFloor.Click += new System.EventHandler(this.BtnGetFloor_Click);
            // 
            // BtnFloorFocus
            // 
            this.BtnFloorFocus.AllowFocus = false;
            this.BtnFloorFocus.Location = new System.Drawing.Point(13, 13);
            this.BtnFloorFocus.Name = "BtnFloorFocus";
            this.BtnFloorFocus.Size = new System.Drawing.Size(86, 22);
            this.BtnFloorFocus.StyleController = this.layoutControl1;
            this.BtnFloorFocus.TabIndex = 4;
            this.BtnFloorFocus.Text = "楼层框定";
            this.BtnFloorFocus.Click += new System.EventHandler(this.BtnFloorFocus_Click);
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.layoutControlItem3,
            this.layoutControlItem2,
            this.layoutControlItem4,
            this.layoutControlItem5,
            this.layoutControlItem6});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Padding = new DevExpress.XtraLayout.Utils.Padding(8, 8, 8, 8);
            this.layoutControlGroup1.Size = new System.Drawing.Size(208, 423);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.BtnFloorFocus;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem1.Size = new System.Drawing.Size(96, 32);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.BtnParam;
            this.layoutControlItem3.Location = new System.Drawing.Point(96, 0);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem3.Size = new System.Drawing.Size(96, 65);
            this.layoutControlItem3.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem3.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem3.TextToControlDistance = 0;
            this.layoutControlItem3.TextVisible = false;
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.BtnGetFloor;
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 32);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem2.Size = new System.Drawing.Size(96, 33);
            this.layoutControlItem2.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem2.TextVisible = false;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.ListBox;
            this.layoutControlItem4.Location = new System.Drawing.Point(0, 65);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem4.Size = new System.Drawing.Size(192, 310);
            this.layoutControlItem4.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem4.TextVisible = false;
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.BtnLayoutRiser;
            this.layoutControlItem5.Location = new System.Drawing.Point(0, 375);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem5.Size = new System.Drawing.Size(96, 32);
            this.layoutControlItem5.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem5.TextVisible = false;
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.BtnUse;
            this.layoutControlItem6.Location = new System.Drawing.Point(96, 375);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem6.Size = new System.Drawing.Size(96, 32);
            this.layoutControlItem6.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem6.TextVisible = false;
            // 
            // fmFloorDrain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(208, 423);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.LookAndFeel.SkinName = "The Bezier";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmFloorDrain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "地上排水";
            this.Load += new System.EventHandler(this.fmFloorDrain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ListBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraEditors.SimpleButton BtnParam;
        private DevExpress.XtraEditors.SimpleButton BtnGetFloor;
        private DevExpress.XtraEditors.SimpleButton BtnFloorFocus;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraEditors.SimpleButton BtnUse;
        private DevExpress.XtraEditors.SimpleButton BtnLayoutRiser;
        private DevExpress.XtraEditors.ListBoxControl ListBox;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
    }
}