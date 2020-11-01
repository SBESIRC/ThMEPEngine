namespace TianHua.FanSelection.UI
{
    partial class CtlSmokeExtraction
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
            this.Notetext = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ComBoxSpatialType = new DevExpress.XtraEditors.ComboBoxEdit();
            this.RadSpray = new DevExpress.XtraEditors.RadioGroup();
            this.TxtHeight = new DevExpress.XtraEditors.TextEdit();
            this.TxtMinUnitVolume = new DevExpress.XtraEditors.TextEdit();
            this.layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            this.simpleLabelItem1 = new DevExpress.XtraLayout.SimpleLabelItem();
            this.layoutControlItem2 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem3 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem4 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem7 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem5 = new DevExpress.XtraLayout.LayoutControlItem();
            this.layoutControlItem6 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).BeginInit();
            this.layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxSpatialType.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RadSpray.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtHeight.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtMinUnitVolume.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleLabelItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).BeginInit();
            this.SuspendLayout();
            // 
            // layoutControl1
            // 
            this.layoutControl1.Controls.Add(this.Notetext);
            this.layoutControl1.Controls.Add(this.label2);
            this.layoutControl1.Controls.Add(this.label1);
            this.layoutControl1.Controls.Add(this.ComBoxSpatialType);
            this.layoutControl1.Controls.Add(this.RadSpray);
            this.layoutControl1.Controls.Add(this.TxtHeight);
            this.layoutControl1.Controls.Add(this.TxtMinUnitVolume);
            this.layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.layoutControl1.Location = new System.Drawing.Point(0, 0);
            this.layoutControl1.Name = "layoutControl1";
            this.layoutControl1.Root = this.layoutControlGroup1;
            this.layoutControl1.Size = new System.Drawing.Size(255, 176);
            this.layoutControl1.TabIndex = 0;
            this.layoutControl1.Text = "layoutControl1";
            // 
            // Notetext
            // 
            this.Notetext.Location = new System.Drawing.Point(2, 151);
            this.Notetext.MaximumSize = new System.Drawing.Size(251, 60);
            this.Notetext.MinimumSize = new System.Drawing.Size(251, 60);
            this.Notetext.Name = "Notetext";
            this.Notetext.Size = new System.Drawing.Size(251, 60);
            this.Notetext.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(215, 121);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 26);
            this.label2.TabIndex = 10;
            this.label2.Text = "m³/h";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(215, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 26);
            this.label1.TabIndex = 9;
            this.label1.Text = "m";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ComBoxSpatialType
            // 
            this.ComBoxSpatialType.Location = new System.Drawing.Point(70, 29);
            this.ComBoxSpatialType.Name = "ComBoxSpatialType";
            this.ComBoxSpatialType.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.ComBoxSpatialType.Properties.Items.AddRange(new object[] {
            "办公室、学校、客厅、走道",
            "商店、展览厅",
            "厂房",
            "仓库",
            "汽车库",
            "其他公共场所"});
            this.ComBoxSpatialType.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.ComBoxSpatialType.Properties.SelectedIndexChanged += new System.EventHandler(this.SpatialTypeSelectedChanged);
            this.ComBoxSpatialType.Size = new System.Drawing.Size(180, 20);
            this.ComBoxSpatialType.StyleController = this.layoutControl1;
            this.ComBoxSpatialType.TabIndex = 4;
            // 
            // RadSpray
            // 
            this.RadSpray.Location = new System.Drawing.Point(70, 59);
            this.RadSpray.Name = "RadSpray";
            this.RadSpray.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.RadSpray.Properties.Appearance.Options.UseBackColor = true;
            this.RadSpray.Properties.Columns = 2;
            this.RadSpray.Properties.Items.AddRange(new DevExpress.XtraEditors.Controls.RadioGroupItem[] {
            new DevExpress.XtraEditors.Controls.RadioGroupItem("有喷淋", "有喷淋"),
            new DevExpress.XtraEditors.Controls.RadioGroupItem("无喷淋", "无喷淋")});
            this.RadSpray.Properties.SelectedIndexChanged += new System.EventHandler(this.RadSpraySelectedChanged);
            this.RadSpray.Size = new System.Drawing.Size(180, 25);
            this.RadSpray.StyleController = this.layoutControl1;
            this.RadSpray.TabIndex = 5;
            // 
            // TxtHeight
            // 
            this.TxtHeight.Location = new System.Drawing.Point(70, 94);
            this.TxtHeight.Name = "TxtHeight";
            this.TxtHeight.Properties.Mask.AutoComplete = DevExpress.XtraEditors.Mask.AutoCompleteType.None;
            this.TxtHeight.Properties.Mask.EditMask = "([0-8]([.][0-9]{1,2}){0,1})|[9]";
            this.TxtHeight.Properties.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.RegEx;
            this.TxtHeight.Properties.Mask.ShowPlaceHolders = false;
            this.TxtHeight.Properties.NullValuePrompt = "输入0~9之间的数值";
            this.TxtHeight.Properties.NullValuePromptShowForEmptyValue = true;
            this.TxtHeight.Properties.EditValueChanged += new System.EventHandler(this.TxtHeightChanged);
            this.TxtHeight.Size = new System.Drawing.Size(138, 20);
            this.TxtHeight.StyleController = this.layoutControl1;
            this.TxtHeight.TabIndex = 5;
            // 
            // TxtMinUnitVolume
            // 
            this.TxtMinUnitVolume.EditValue = "107000";
            this.TxtMinUnitVolume.Location = new System.Drawing.Point(70, 124);
            this.TxtMinUnitVolume.Name = "TxtMinUnitVolume";
            this.TxtMinUnitVolume.Properties.ReadOnly = true;
            this.TxtMinUnitVolume.Size = new System.Drawing.Size(138, 20);
            this.TxtMinUnitVolume.StyleController = this.layoutControl1;
            this.TxtMinUnitVolume.TabIndex = 8;
            // 
            // layoutControlGroup1
            // 
            this.layoutControlGroup1.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            this.layoutControlGroup1.GroupBordersVisible = false;
            this.layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] {
            this.simpleLabelItem1,
            this.layoutControlItem2,
            this.layoutControlItem3,
            this.layoutControlItem4,
            this.layoutControlItem7,
            this.layoutControlItem1,
            this.layoutControlItem5,
            this.layoutControlItem6});
            this.layoutControlGroup1.Name = "layoutControlGroup1";
            this.layoutControlGroup1.Padding = new DevExpress.XtraLayout.Utils.Padding(0, 0, 0, 0);
            this.layoutControlGroup1.Size = new System.Drawing.Size(255, 213);
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
            this.simpleLabelItem1.TextSize = new System.Drawing.Size(122, 14);
            // 
            // layoutControlItem2
            // 
            this.layoutControlItem2.Control = this.ComBoxSpatialType;
            this.layoutControlItem2.CustomizationFormText = "空间类型：";
            this.layoutControlItem2.Location = new System.Drawing.Point(0, 24);
            this.layoutControlItem2.Name = "layoutControlItem2";
            this.layoutControlItem2.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem2.Size = new System.Drawing.Size(255, 30);
            this.layoutControlItem2.Text = "空间类型：";
            this.layoutControlItem2.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem2.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem2.TextToControlDistance = 5;
            // 
            // layoutControlItem3
            // 
            this.layoutControlItem3.Control = this.RadSpray;
            this.layoutControlItem3.CustomizationFormText = "喷      淋：";
            this.layoutControlItem3.Location = new System.Drawing.Point(0, 54);
            this.layoutControlItem3.Name = "layoutControlItem3";
            this.layoutControlItem3.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem3.Size = new System.Drawing.Size(255, 35);
            this.layoutControlItem3.Text = "喷      淋：";
            this.layoutControlItem3.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem3.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem3.TextToControlDistance = 5;
            // 
            // layoutControlItem4
            // 
            this.layoutControlItem4.Control = this.TxtHeight;
            this.layoutControlItem4.CustomizationFormText = "空间净高：";
            this.layoutControlItem4.Location = new System.Drawing.Point(0, 89);
            this.layoutControlItem4.Name = "layoutControlItem4";
            this.layoutControlItem4.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem4.Size = new System.Drawing.Size(213, 30);
            this.layoutControlItem4.Text = "空间净高：";
            this.layoutControlItem4.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem4.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem4.TextToControlDistance = 5;
            // 
            // layoutControlItem7
            // 
            this.layoutControlItem7.Control = this.TxtMinUnitVolume;
            this.layoutControlItem7.CustomizationFormText = "最小风量：";
            this.layoutControlItem7.Location = new System.Drawing.Point(0, 119);
            this.layoutControlItem7.Name = "layoutControlItem7";
            this.layoutControlItem7.Padding = new DevExpress.XtraLayout.Utils.Padding(5, 5, 5, 5);
            this.layoutControlItem7.Size = new System.Drawing.Size(213, 30);
            this.layoutControlItem7.Text = "最小风量：";
            this.layoutControlItem7.TextAlignMode = DevExpress.XtraLayout.TextAlignModeItem.AutoSize;
            this.layoutControlItem7.TextSize = new System.Drawing.Size(60, 14);
            this.layoutControlItem7.TextToControlDistance = 5;
            // 
            // layoutControlItem1
            // 
            this.layoutControlItem1.Control = this.label1;
            this.layoutControlItem1.Location = new System.Drawing.Point(213, 89);
            this.layoutControlItem1.Name = "layoutControlItem1";
            this.layoutControlItem1.Size = new System.Drawing.Size(42, 30);
            this.layoutControlItem1.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem1.TextVisible = false;
            // 
            // layoutControlItem5
            // 
            this.layoutControlItem5.Control = this.label2;
            this.layoutControlItem5.Location = new System.Drawing.Point(213, 119);
            this.layoutControlItem5.Name = "layoutControlItem5";
            this.layoutControlItem5.Size = new System.Drawing.Size(42, 30);
            this.layoutControlItem5.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem5.TextVisible = false;
            // 
            // layoutControlItem6
            // 
            this.layoutControlItem6.Control = this.Notetext;
            this.layoutControlItem6.Location = new System.Drawing.Point(0, 149);
            this.layoutControlItem6.Name = "layoutControlItem6";
            this.layoutControlItem6.Size = new System.Drawing.Size(255, 64);
            this.layoutControlItem6.TextSize = new System.Drawing.Size(0, 0);
            this.layoutControlItem6.TextVisible = false;
            // 
            // CtlSmokeExtraction
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.layoutControl1);
            this.Name = "CtlSmokeExtraction";
            this.Size = new System.Drawing.Size(255, 176);
            ((System.ComponentModel.ISupportInitialize)(this.layoutControl1)).EndInit();
            this.layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ComBoxSpatialType.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RadSpray.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtHeight.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtMinUnitVolume.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlGroup1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.simpleLabelItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem7)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem5)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.layoutControlItem6)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;
        private DevExpress.XtraLayout.SimpleLabelItem simpleLabelItem1;
        private DevExpress.XtraEditors.ComboBoxEdit ComBoxSpatialType;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem2;
        private DevExpress.XtraEditors.RadioGroup RadSpray;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem3;
        private DevExpress.XtraEditors.TextEdit TxtHeight;
        private DevExpress.XtraEditors.TextEdit TxtMinUnitVolume;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem4;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem7;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem5;
        private System.Windows.Forms.Label Notetext;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem6;
    }
}
