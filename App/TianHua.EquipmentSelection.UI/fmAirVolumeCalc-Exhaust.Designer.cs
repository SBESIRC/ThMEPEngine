namespace TianHua.FanSelection.UI
{
    partial class fmAirVolumeCalc_Exhaust
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(fmAirVolumeCalc_Exhaust));
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.BtnOK = new DevExpress.XtraEditors.SimpleButton();
            this.TxtAirVolume = new DevExpress.XtraEditors.TextEdit();
            this.TxtFactor = new DevExpress.XtraEditors.TextEdit();
            this.TxtEstimatedValue = new DevExpress.XtraEditors.TextEdit();
            this.TxtCalcValue = new DevExpress.XtraEditors.TextEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.simpleSeparator1 = new DevExpress.XtraLayout.SimpleSeparator();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.emptySpaceItem1 = new DevExpress.XtraLayout.EmptySpaceItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem7 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem8 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtAirVolume.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFactor.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtEstimatedValue.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCalcValue.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleSeparator1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem8)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.label3);
            this.layoutControl1.Controls.Add(this.label2);
            this.layoutControl1.Controls.Add(this.label1);
            this.layoutControl1.Controls.Add(this.BtnOK);
            this.layoutControl1.Controls.Add(this.TxtAirVolume);
            this.layoutControl1.Controls.Add(this.TxtFactor);
            this.layoutControl1.Controls.Add(this.TxtEstimatedValue);
            this.layoutControl1.Controls.Add(this.TxtCalcValue);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(612, 222, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(241, 174);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(192, 104);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 26);
            this.label3.TabIndex = 11;
            this.label3.Text = "m³/h";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(192, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 26);
            this.label2.TabIndex = 10;
            this.label2.Text = "m³/h";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(192, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 26);
            this.label1.TabIndex = 9;
            this.label1.Text = "m³/h";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // BtnOK
            // 
            this.BtnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.BtnOK.Location = new System.Drawing.Point(148, 137);
            this.BtnOK.Name = "BtnOK";
            this.BtnOK.Size = new System.Drawing.Size(78, 22);
            this.BtnOK.StyleController = this.layoutControl1;
            this.BtnOK.TabIndex = 8;
            this.BtnOK.Text = "确认";
            this.BtnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // TxtAirVolume
            // 
            this.TxtAirVolume.Location = new System.Drawing.Point(78, 107);
            this.TxtAirVolume.Name = "TxtAirVolume";
            this.TxtAirVolume.Size = new System.Drawing.Size(107, 20);
            this.TxtAirVolume.StyleController = this.layoutControl1;
            this.TxtAirVolume.TabIndex = 7;
            this.TxtAirVolume.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtAirVolume_KeyPress);
            // 
            // TxtFactor
            // 
            this.TxtFactor.Location = new System.Drawing.Point(78, 75);
            this.TxtFactor.Name = "TxtFactor";
            this.TxtFactor.Properties.Mask.EditMask = "\\d*\\.{0,1}\\d{0,2}";
            this.TxtFactor.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtFactor.Properties.EditValueChanged += new System.EventHandler(this.SelectFactorChanged);
            this.TxtFactor.Properties.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FactorChangedCheck);
            this.TxtFactor.Properties.Leave += new System.EventHandler(this.FactorLeaveCheck);
            this.TxtFactor.Size = new System.Drawing.Size(148, 20);
            this.TxtFactor.StyleController = this.layoutControl1;
            this.TxtFactor.TabIndex = 6;
            // 
            // TxtEstimatedValue
            // 
            this.TxtEstimatedValue.Location = new System.Drawing.Point(78, 45);
            this.TxtEstimatedValue.Name = "TxtEstimatedValue";
            this.TxtEstimatedValue.Properties.Mask.EditMask = "([0-9]{1,})";
            this.TxtEstimatedValue.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtEstimatedValue.Properties.EditValueChanged += new System.EventHandler(this.EstimateValueChanged);
            this.TxtEstimatedValue.Size = new System.Drawing.Size(107, 20);
            this.TxtEstimatedValue.StyleController = this.layoutControl1;
            this.TxtEstimatedValue.TabIndex = 5;
            // 
            // TxtCalcValue
            // 
            this.TxtCalcValue.Location = new System.Drawing.Point(78, 15);
            this.TxtCalcValue.Name = "TxtCalcValue";
            this.TxtCalcValue.Properties.AllowFocused = false;
            this.TxtCalcValue.Properties.ContextImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("TxtCalcValue.Properties.ContextImageOptions.SvgImage")));
            this.TxtCalcValue.Properties.ReadOnly = true;
            this.TxtCalcValue.Properties.EditValueChanged += new System.EventHandler(this.CalculateValueChanged);
            this.TxtCalcValue.Size = new System.Drawing.Size(107, 20);
            this.TxtCalcValue.StyleController = this.layoutControl1;
            this.TxtCalcValue.TabIndex = 4;
            this.TxtCalcValue.Click += new System.EventHandler(this.TxtCalcValue_Click);
            this.TxtCalcValue.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TxtCalcValue_KeyPress);
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.layoutControlItem1,
            this.layoutControlItem2,
            this.layoutControlItem3,
            this.simpleSeparator1,
            this.layoutControlItem4,
            this.layoutControlItem5,
            this.emptySpaceItem1,
            this.layoutControlItem6,
            this.layoutControlItem7,
            this.layoutControlItem8});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Size = new System.Drawing.Size(241, 174);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.TxtCalcValue;
            this.layoutControlItem1.Location = new System.Drawing.Point(0, 0);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem1.Size = new System.Drawing.Size(180, 30);
            this.layoutControlItem1.Text = "计算值：";
            this.layoutControlItem1.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.TxtEstimatedValue;
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 30);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem2.Size = new System.Drawing.Size(180, 30);
            this.layoutControlItem2.Text = "估算值：";
            this.layoutControlItem2.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.TxtFactor;
            this.layoutControlItem3.Location = new System.Drawing.Point(0, 60);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem3.Size = new System.Drawing.Size(221, 30);
            this.layoutControlItem3.Text = "选型系数：";
            this.layoutControlItem3.TextSize = new System.Drawing.Size(60, 14);
            // 
            // simpleSeparator1
            // 
            this.simpleSeparator1.AllowHotTrack = false;
            this.simpleSeparator1.Location = new System.Drawing.Point(0, 90);
            this.simpleSeparator1.Name = "simpleSeparator1";
            this.simpleSeparator1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.simpleSeparator1.Size = new System.Drawing.Size(221, 2);
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.TxtAirVolume;
            this.layoutControlItem4.Location = new System.Drawing.Point(0, 92);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem4.Size = new System.Drawing.Size(180, 30);
            this.layoutControlItem4.Text = "选型风量：";
            this.layoutControlItem4.TextSize = new System.Drawing.Size(60, 14);
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.BtnOK;
            this.layoutControlItem5.Location = new System.Drawing.Point(133, 122);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem5.Size = new System.Drawing.Size(88, 32);
            this.layoutControlItem5.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem5.TextVisible = false;
            // 
            // emptySpaceItem1
            // 
            this.emptySpaceItem1.AllowHotTrack = false;
            this.emptySpaceItem1.Location = new System.Drawing.Point(0, 122);
            this.emptySpaceItem1.Name = "emptySpaceItem1";
            this.emptySpaceItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.emptySpaceItem1.Size = new System.Drawing.Size(133, 32);
            this.emptySpaceItem1.TextSize = new System.Drawing.Size(0, 0);
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.label1;
            this.layoutControlItem6.Location = new System.Drawing.Point(180, 0);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Size = new System.Drawing.Size(41, 30);
            this.layoutControlItem6.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem6.TextVisible = false;
            // 
            // layoutControlItem7
            // 
            this.layoutControlItem7.Control = this.label2;
            this.layoutControlItem7.Location = new System.Drawing.Point(180, 30);
            this.layoutControlItem7.Name = "layoutControlItem7";
            this.layoutControlItem7.Size = new System.Drawing.Size(41, 30);
            this.layoutControlItem7.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem7.TextVisible = false;
            // 
            // layoutControlItem8
            // 
            this.layoutControlItem8.Control = this.label3;
            this.layoutControlItem8.Location = new System.Drawing.Point(180, 92);
            this.layoutControlItem8.Name = "layoutControlItem8";
            this.layoutControlItem8.Size = new System.Drawing.Size(41, 30);
            this.layoutControlItem8.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem8.TextVisible = false;
            // 
            // fmAirVolumeCalc_Exhaust
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(241, 174);
            this.Controls.Add(this.layoutControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmAirVolumeCalc_Exhaust";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "风量计算器";
            this.Load += new System.EventHandler(this.fmAirVolumeCalc_Exhaust_Load);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TxtAirVolume.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFactor.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtEstimatedValue.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtCalcValue.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleSeparator1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.emptySpaceItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem8)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraEditors.SimpleButton BtnOK;
        private DevExpress.XtraEditors.TextEdit TxtAirVolume;
        private DevExpress.XtraEditors.TextEdit TxtFactor;
        private DevExpress.XtraEditors.TextEdit TxtEstimatedValue;
        private DevExpress.XtraEditors.TextEdit TxtCalcValue;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraLayout.SimpleSeparator simpleSeparator1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private DevExpress.XtraLayout.EmptySpaceItem emptySpaceItem1;
        private System.Windows.Forms.Label label1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem7;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem8;
    }
}