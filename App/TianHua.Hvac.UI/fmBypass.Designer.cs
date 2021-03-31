
namespace TianHua.Hvac.UI
{
    partial class fmBypass
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.RBType5 = new System.Windows.Forms.RadioButton();
            this.RBType1 = new System.Windows.Forms.RadioButton();
            this.RBType2 = new System.Windows.Forms.RadioButton();
            this.RBType4 = new System.Windows.Forms.RadioButton();
            this.RBType3 = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.RadType2 = new System.Windows.Forms.RadioButton();
            this.RadType1 = new System.Windows.Forms.RadioButton();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.label2);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2.Controls.Add(this.textBox3);
            this.splitContainer1.Panel2.Controls.Add(this.textBox2);
            this.splitContainer1.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer1.Panel2.Controls.Add(this.listBox1);
            this.splitContainer1.Panel2.Controls.Add(this.buttonOK);
            this.splitContainer1.Panel2.Controls.Add(this.label4);
            this.splitContainer1.Panel2.Controls.Add(this.textBox1);
            this.splitContainer1.Panel2.Controls.Add(this.label3);
            this.splitContainer1.Size = new System.Drawing.Size(249, 431);
            this.splitContainer1.SplitterDistance = 129;
            this.splitContainer1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.RBType5);
            this.groupBox1.Controls.Add(this.RBType1);
            this.groupBox1.Controls.Add(this.RBType2);
            this.groupBox1.Controls.Add(this.RBType4);
            this.groupBox1.Controls.Add(this.RBType3);
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(290, 135);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "形式：";
            // 
            // RBType5
            // 
            this.RBType5.AutoSize = true;
            this.RBType5.Location = new System.Drawing.Point(15, 101);
            this.RBType5.Name = "RBType5";
            this.RBType5.Size = new System.Drawing.Size(107, 16);
            this.RBType5.TabIndex = 5;
            this.RBType5.Text = "风机进出口(下)";
            this.RBType5.UseVisualStyleBackColor = true;
            this.RBType5.CheckedChanged += new System.EventHandler(this.RBType5_CheckedChanged);
            // 
            // RBType1
            // 
            this.RBType1.AutoSize = true;
            this.RBType1.Location = new System.Drawing.Point(15, 13);
            this.RBType1.Name = "RBType1";
            this.RBType1.Size = new System.Drawing.Size(95, 16);
            this.RBType1.TabIndex = 1;
            this.RBType1.Text = "旁通至进风井";
            this.RBType1.UseVisualStyleBackColor = true;
            // 
            // RBType2
            // 
            this.RBType2.AutoSize = true;
            this.RBType2.Checked = true;
            this.RBType2.Location = new System.Drawing.Point(15, 35);
            this.RBType2.Name = "RBType2";
            this.RBType2.Size = new System.Drawing.Size(83, 16);
            this.RBType2.TabIndex = 2;
            this.RBType2.TabStop = true;
            this.RBType2.Text = "旁通至室内";
            this.RBType2.UseVisualStyleBackColor = true;
            // 
            // RBType4
            // 
            this.RBType4.AutoSize = true;
            this.RBType4.Location = new System.Drawing.Point(15, 79);
            this.RBType4.Name = "RBType4";
            this.RBType4.Size = new System.Drawing.Size(107, 16);
            this.RBType4.TabIndex = 4;
            this.RBType4.Text = "风机进出口(上)";
            this.RBType4.UseVisualStyleBackColor = true;
            this.RBType4.CheckedChanged += new System.EventHandler(this.RBType4_CheckedChanged);
            // 
            // RBType3
            // 
            this.RBType3.AutoSize = true;
            this.RBType3.Location = new System.Drawing.Point(15, 57);
            this.RBType3.Name = "RBType3";
            this.RBType3.Size = new System.Drawing.Size(107, 16);
            this.RBType3.TabIndex = 3;
            this.RBType3.Text = "风机进出口(侧)";
            this.RBType3.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Location = new System.Drawing.Point(18, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(142, 30);
            this.label2.TabIndex = 16;
            this.label2.Text = "计算风速    12  m/s";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(69, 49);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(11, 12);
            this.label1.TabIndex = 15;
            this.label1.Text = "x";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(96, 44);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(45, 21);
            this.textBox3.TabIndex = 14;
            this.textBox3.Text = "0";
            this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            this.textBox3.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox3_KeyPress);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(12, 44);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(45, 21);
            this.textBox2.TabIndex = 13;
            this.textBox2.Text = "0";
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            this.textBox2.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox2_KeyPress);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.RadType2);
            this.groupBox2.Controls.Add(this.RadType1);
            this.groupBox2.Location = new System.Drawing.Point(12, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(135, 42);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "旁通风管规格";
            // 
            // RadType2
            // 
            this.RadType2.AutoSize = true;
            this.RadType2.Location = new System.Drawing.Point(59, 20);
            this.RadType2.Name = "RadType2";
            this.RadType2.Size = new System.Drawing.Size(59, 16);
            this.RadType2.TabIndex = 7;
            this.RadType2.Text = "自定义";
            this.RadType2.UseVisualStyleBackColor = true;
            this.RadType2.CheckedChanged += new System.EventHandler(this.RadType2_CheckedChanged);
            // 
            // RadType1
            // 
            this.RadType1.AutoSize = true;
            this.RadType1.Checked = true;
            this.RadType1.Location = new System.Drawing.Point(6, 20);
            this.RadType1.Name = "RadType1";
            this.RadType1.Size = new System.Drawing.Size(47, 16);
            this.RadType1.TabIndex = 6;
            this.RadType1.TabStop = true;
            this.RadType1.Text = "推荐";
            this.RadType1.UseVisualStyleBackColor = true;
            this.RadType1.CheckedChanged += new System.EventHandler(this.RadType1_CheckedChanged);
            // 
            // listBox1
            // 
            this.listBox1.Font = new System.Drawing.Font("宋体", 10F);
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Items.AddRange(new object[] {
            "1250x800",
            "1000x1000",
            "1000x630"});
            this.listBox1.Location = new System.Drawing.Point(15, 72);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(164, 56);
            this.listBox1.TabIndex = 12;
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(12, 145);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(148, 23);
            this.buttonOK.TabIndex = 11;
            this.buttonOK.Text = "确定";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(153, 49);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 17);
            this.label4.TabIndex = 10;
            this.label4.Text = "m/s";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(47, 45);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 21);
            this.textBox1.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(10, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 17);
            this.label3.TabIndex = 8;
            this.label3.Text = "风速：";
            // 
            // fmBypass
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(249, 431);
            this.Controls.Add(this.splitContainer1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "fmBypass";
            this.Text = "旁通设置";
            this.Load += new System.EventHandler(this.fmBypass_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RadioButton RBType5;
        private System.Windows.Forms.RadioButton RBType4;
        private System.Windows.Forms.RadioButton RBType1;
        private System.Windows.Forms.RadioButton RBType3;
        private System.Windows.Forms.RadioButton RBType2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RadioButton RadType2;
        private System.Windows.Forms.RadioButton RadType1;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox2;
    }
}