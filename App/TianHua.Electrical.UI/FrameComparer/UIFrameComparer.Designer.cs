
namespace TianHua.Electrical.UI.FrameComparer
{
    partial class UIFrameComparer
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
            this.listViewComparerRes = new System.Windows.Forms.ListView();
            this.columnRegionRange = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnObjRange = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnReference = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.curGraph = new System.Windows.Forms.Label();
            this.GraphPath = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.frameUpdate = new System.Windows.Forms.Button();
            this.btnDoComparer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listViewComparerRes
            // 
            this.listViewComparerRes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnRegionRange,
            this.columnObjRange,
            this.columnReference});
            this.listViewComparerRes.FullRowSelect = true;
            this.listViewComparerRes.GridLines = true;
            this.listViewComparerRes.HideSelection = false;
            this.listViewComparerRes.LabelEdit = true;
            this.listViewComparerRes.Location = new System.Drawing.Point(12, 50);
            this.listViewComparerRes.Name = "listViewComparerRes";
            this.listViewComparerRes.Size = new System.Drawing.Size(249, 292);
            this.listViewComparerRes.TabIndex = 0;
            this.listViewComparerRes.UseCompatibleStateImageBehavior = false;
            this.listViewComparerRes.View = System.Windows.Forms.View.Details;
            this.listViewComparerRes.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewComparerRes_ItemSelectionChanged);
            // 
            // columnRegionRange
            // 
            this.columnRegionRange.Text = "问题分类";
            this.columnRegionRange.Width = 72;
            // 
            // columnObjRange
            // 
            this.columnObjRange.Text = "对象类型";
            this.columnObjRange.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnObjRange.Width = 80;
            // 
            // columnReference
            // 
            this.columnReference.Text = "问题坐标";
            this.columnReference.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnReference.Width = 80;
            // 
            // curGraph
            // 
            this.curGraph.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.curGraph.Location = new System.Drawing.Point(13, 13);
            this.curGraph.Name = "curGraph";
            this.curGraph.Size = new System.Drawing.Size(61, 23);
            this.curGraph.TabIndex = 1;
            this.curGraph.Text = "当前图纸:";
            // 
            // GraphPath
            // 
            this.GraphPath.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.GraphPath.FormattingEnabled = true;
            this.GraphPath.Location = new System.Drawing.Point(80, 10);
            this.GraphPath.Name = "GraphPath";
            this.GraphPath.Size = new System.Drawing.Size(181, 20);
            this.GraphPath.TabIndex = 2;
            this.GraphPath.SelectedIndexChanged += new System.EventHandler(this.GraphPath_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 352);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "位移小于";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(73, 348);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(44, 21);
            this.textBox1.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(123, 352);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 12);
            this.label2.TabIndex = 5;
            this.label2.Text = "mm时忽略变化";
            // 
            // frameUpdate
            // 
            this.frameUpdate.Enabled = false;
            this.frameUpdate.Location = new System.Drawing.Point(111, 373);
            this.frameUpdate.Name = "frameUpdate";
            this.frameUpdate.Size = new System.Drawing.Size(75, 23);
            this.frameUpdate.TabIndex = 6;
            this.frameUpdate.Text = "框线更新";
            this.frameUpdate.UseVisualStyleBackColor = true;
            this.frameUpdate.Click += new System.EventHandler(this.frameUpdate_Click);
            // 
            // btnDoComparer
            // 
            this.btnDoComparer.Location = new System.Drawing.Point(192, 373);
            this.btnDoComparer.Name = "btnDoComparer";
            this.btnDoComparer.Size = new System.Drawing.Size(75, 23);
            this.btnDoComparer.TabIndex = 7;
            this.btnDoComparer.Text = "框线对比";
            this.btnDoComparer.UseVisualStyleBackColor = true;
            this.btnDoComparer.Click += new System.EventHandler(this.btnDoComparer_Click);
            // 
            // UIFrameComparer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(273, 408);
            this.Controls.Add(this.btnDoComparer);
            this.Controls.Add(this.frameUpdate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.GraphPath);
            this.Controls.Add(this.curGraph);
            this.Controls.Add(this.listViewComparerRes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UIFrameComparer";
            this.Text = "房间框线对比";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listViewComparerRes;
        private System.Windows.Forms.ColumnHeader columnRegionRange;
        private System.Windows.Forms.ColumnHeader columnObjRange;
        private System.Windows.Forms.ColumnHeader columnReference;
        private System.Windows.Forms.Label curGraph;
        private System.Windows.Forms.ComboBox GraphPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button frameUpdate;
        private System.Windows.Forms.Button btnDoComparer;
    }
}