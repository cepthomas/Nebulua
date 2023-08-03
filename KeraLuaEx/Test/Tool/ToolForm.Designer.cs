﻿namespace KeraLuaEx.Tool
{
    partial class ToolForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            btnOpen = new System.Windows.Forms.ToolStripButton();
            btnGoMain = new System.Windows.Forms.ToolStripButton();
            rtbScript = new System.Windows.Forms.RichTextBox();
            rtbOutput = new System.Windows.Forms.RichTextBox();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            splitContainer3 = new System.Windows.Forms.SplitContainer();
            rtbStack = new System.Windows.Forms.RichTextBox();
            splitContainer2 = new System.Windows.Forms.SplitContainer();
            btnGoJson = new System.Windows.Forms.ToolStripButton();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { btnOpen, btnGoJson, btnGoMain });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1286, 27);
            toolStrip1.TabIndex = 0;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnOpen
            // 
            btnOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnOpen.Name = "btnOpen";
            btnOpen.Size = new System.Drawing.Size(49, 24);
            btnOpen.Text = "Open";
            btnOpen.Click += Open_Click;
            // 
            // btnGoMain
            // 
            btnGoMain.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnGoMain.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnGoMain.Name = "btnGoMain";
            btnGoMain.Size = new System.Drawing.Size(69, 24);
            btnGoMain.Text = "Go Main";
            btnGoMain.Click += GoMain_Click;
            // 
            // rtbScript
            // 
            rtbScript.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            rtbScript.Dock = System.Windows.Forms.DockStyle.Fill;
            rtbScript.Location = new System.Drawing.Point(0, 0);
            rtbScript.Name = "rtbScript";
            rtbScript.Size = new System.Drawing.Size(960, 385);
            rtbScript.TabIndex = 1;
            rtbScript.Text = "";
            // 
            // rtbOutput
            // 
            rtbOutput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            rtbOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            rtbOutput.Location = new System.Drawing.Point(0, 0);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new System.Drawing.Size(960, 422);
            rtbOutput.TabIndex = 2;
            rtbOutput.Text = "";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 27);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer3);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new System.Drawing.Size(1286, 811);
            splitContainer1.SplitterDistance = 322;
            splitContainer1.TabIndex = 3;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer3.Location = new System.Drawing.Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(rtbStack);
            splitContainer3.Size = new System.Drawing.Size(322, 811);
            splitContainer3.SplitterDistance = 346;
            splitContainer3.TabIndex = 0;
            // 
            // rtbStack
            // 
            rtbStack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            rtbStack.Dock = System.Windows.Forms.DockStyle.Fill;
            rtbStack.Location = new System.Drawing.Point(0, 0);
            rtbStack.Name = "rtbStack";
            rtbStack.ReadOnly = true;
            rtbStack.Size = new System.Drawing.Size(322, 461);
            rtbStack.TabIndex = 0;
            rtbStack.Text = "";
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer2.Location = new System.Drawing.Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(rtbScript);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(rtbOutput);
            splitContainer2.Size = new System.Drawing.Size(960, 811);
            splitContainer2.SplitterDistance = 385;
            splitContainer2.TabIndex = 0;
            // 
            // btnGoJson
            // 
            btnGoJson.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            btnGoJson.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnGoJson.Name = "btnGoJson";
            btnGoJson.Size = new System.Drawing.Size(63, 24);
            btnGoJson.Text = "Go json";
            btnGoJson.Click += GoJson_Click;
            // 
            // ToolForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            ClientSize = new System.Drawing.Size(1286, 838);
            Controls.Add(splitContainer1);
            Controls.Add(toolStrip1);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "ToolForm";
            Text = "KeraLuaEx Test Tool";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.RichTextBox rtbScript;
        private System.Windows.Forms.RichTextBox rtbOutput;
        private System.Windows.Forms.ToolStripButton btnOpen;
        private System.Windows.Forms.ToolStripButton btnGoMain;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.RichTextBox rtbStack;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ToolStripButton btnGoJson;
    }
}

