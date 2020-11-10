namespace TianHua.FanSelection.UI
{
    partial class CtlGarage
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            this.RadSpray = new DevExpress.XtraEditors.RadioGroup();
            this.TxtHeight = new DevExpress.XtraEditors.TextEdit();
            this.TxtMinUnitVolume = new DevExpress.XtraEditors.TextEdit();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.simpleLabelItem1 = new DevExpress.XtraLayout.SimpleLabelItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem7 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RadSpray.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtHeight.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtMinUnitVolume.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleLabelItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.RadSpray);
            this.layoutControl1.Controls.Add(this.TxtHeight);
            this.layoutControl1.Controls.Add(this.TxtMinUnitVolume);
            this.layoutControl1.Controls.Add(this.label2);
            this.layoutControl1.Controls.Add(this.label3);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(639, 193, 650, 400);
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(255, 120);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // RadSpray
            // 
            this.RadSpray.Location = new System.Drawing.Point(70, 29);
            this.RadSpray.Name = "RadSpray";
            this.RadSpray.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.RadSpray.Properties.Appearance.Options.UseBackColor = true;
            this.RadSpray.Properties.Columns = 2;
            this.RadSpray.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("有喷淋", "有喷淋"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("无喷淋", "无喷淋")});
            this.RadSpray.Properties.SelectedIndexChanged += new System.EventHandler(this.RadSpraySelectedChanged);
            this.RadSpray.Size = new System.Drawing.Size(180, 26);
            this.RadSpray.StyleController = this.layoutControl1;
            this.RadSpray.TabIndex = 5;
            // 
            // TxtHeight
            // 
            this.TxtHeight.Location = new System.Drawing.Point(70, 65);
            this.TxtHeight.Name = "TxtHeight";
            this.TxtHeight.Properties.Mask.AutoComplete = DevExpress.XtraEditors.Mask.AutoCompleteType.None;
            this.TxtHeight.Properties.Mask.EditMask = "([0-8]([.][0-9]{1,2}){0,1})|[9]";
            this.TxtHeight.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtHeight.Properties.Mask.ShowPlaceHolders = false;
            this.TxtHeight.Properties.EditValueChanged += new System.EventHandler(this.TxtHeightChanged);
            this.TxtHeight.Size = new System.Drawing.Size(137, 20);
            this.TxtHeight.StyleController = this.layoutControl1;
            this.TxtHeight.TabIndex = 5;
            // 
            // TxtMinUnitVolume
            // 
            this.TxtMinUnitVolume.Location = new System.Drawing.Point(70, 95);
            this.TxtMinUnitVolume.Name = "TxtMinUnitVolume";
            this.TxtMinUnitVolume.Properties.ReadOnly = true;
            this.TxtMinUnitVolume.Size = new System.Drawing.Size(137, 20);
            this.TxtMinUnitVolume.StyleController = this.layoutControl1;
            this.TxtMinUnitVolume.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(217, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 20);
            this.label2.TabIndex = 9;
            this.label2.Text = "m";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(217, 95);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 20);
            this.label3.TabIndex = 10;
            this.label3.Text = "m³/h";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.simpleLabelItem1,
            this.layoutControlItem2,
            this.layoutControlItem4,
            this.layoutControlItem5,
            this.layoutControlItem6,
            this.layoutControlItem7});
            this.layoutControlGroup1.Name = "Root";
            this.layoutControlGroup1.Padding = new DevExpress.XtraLayout.Utils.Padding(0, 0, 0, 0);
            this.layoutControlGroup1.Size = new System.Drawing.Size(255, 120);
            this.layoutControlGroup1.TextVisible = false;
            // 
            // simpleLabelItem1
            // 
            this.simpleLabelItem1.AllowHotTrack = false;
            this.simpleLabelItem1.CustomizationFormText = "非中庭，建筑空间净高小于等于6m的场所。";
            this.simpleLabelItem1.Location = new System.Drawing.Point(0, 0);
            this.simpleLabelItem1.Name = "simpleLabelItem1";
            this.simpleLabelItem1.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.simpleLabelItem1.Size = new System.Drawing.Size(255, 24);
            this.simpleLabelItem1.Text = "汽车库";
            this.simpleLabelItem1.TextSize = new System.Drawing.Size(36, 14);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.RadSpray;
            this.layoutControlItem2.CustomizationFormText = "喷      淋：";
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 24);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem2.Size = new System.Drawing.Size(255, 36);
            this.layoutControlItem2.Text = "喷      淋：";
            this.layoutControlItem2.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem2.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem2.TextToControlDistance = 5;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.TxtHeight;
            this.layoutControlItem4.CustomizationFormText = "空间净高：";
            this.layoutControlItem4.Location = new System.Drawing.Point(0, 60);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem4.Size = new System.Drawing.Size(212, 30);
            this.layoutControlItem4.Text = "空间净高：";
            this.layoutControlItem4.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem4.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem4.TextToControlDistance = 5;
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.TxtMinUnitVolume;
            this.layoutControlItem5.CustomizationFormText = "最小风量：";
            this.layoutControlItem5.Location = new System.Drawing.Point(0, 90);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem5.Size = new System.Drawing.Size(212, 30);
            this.layoutControlItem5.Text = "最小风量：";
            this.layoutControlItem5.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem5.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem5.TextToControlDistance = 5;
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.label2;
            this.layoutControlItem6.CustomizationFormText = "layoutControlItem6";
            this.layoutControlItem6.Location = new System.Drawing.Point(212, 60);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem6.Size = new System.Drawing.Size(43, 30);
            this.layoutControlItem6.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem6.TextVisible = false;
            // 
            // layoutControlItem7
            // 
            this.layoutControlItem7.Control = this.label3;
            this.layoutControlItem7.CustomizationFormText = "layoutControlItem7";
            this.layoutControlItem7.Location = new System.Drawing.Point(212, 90);
            this.layoutControlItem7.Name = "layoutControlItem7";
            this.layoutControlItem7.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem7.Size = new System.Drawing.Size(43, 30);
            this.layoutControlItem7.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem7.TextVisible = false;
            // 
            // CtlGarage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutControl1);
            this.Name = "CtlGarage";
            this.Size = new System.Drawing.Size(255, 120);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RadSpray.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtHeight.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtMinUnitVolume.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleLabelItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraLayout.SimpleLabelItem simpleLabelItem1;
        private DevExpress.XtraEditors.RadioGroup RadSpray;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraEditors.TextEdit TxtHeight;
        private DevExpress.XtraEditors.TextEdit TxtMinUnitVolume;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private System.Windows.Forms.Label label2;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
        private System.Windows.Forms.Label label3;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem7;
    }
}
