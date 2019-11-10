using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using System.Text;

namespace Seastar
{
    public class RoundButton : Button
    {
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            GraphicsPath grPath = new GraphicsPath();
            grPath.AddEllipse(0, 0, ClientSize.Width, ClientSize.Height);
            this.Region = new System.Drawing.Region(grPath);
            base.OnPaint(e);
        }
    }

    partial class SimForm
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
            
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.negX = new System.Windows.Forms.Button();
            this.roundButton1 = new Seastar.RoundButton();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(31, 36);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(271, 29);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "v";
            // 
            // negX
            // 
            this.negX.BackColor = System.Drawing.Color.CornflowerBlue;
            this.negX.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.negX.Cursor = System.Windows.Forms.Cursors.Hand;
            this.negX.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.negX.FlatAppearance.BorderSize = 3;
            this.negX.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ActiveCaption;
            this.negX.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Highlight;
            this.negX.Font = new System.Drawing.Font("Calibri Light", 8.142858F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.negX.Location = new System.Drawing.Point(152, 221);
            this.negX.Name = "negX";
            this.negX.Size = new System.Drawing.Size(80, 80);
            this.negX.TabIndex = 0;
            this.negX.Text = "-X";
            this.negX.UseVisualStyleBackColor = false;
            this.negX.Click += new System.EventHandler(this.Button1_Click);
            // 
            // roundButton1
            // 
            this.roundButton1.Location = new System.Drawing.Point(371, 166);
            this.roundButton1.Name = "roundButton1";
            this.roundButton1.Size = new System.Drawing.Size(119, 110);
            this.roundButton1.TabIndex = 2;
            this.roundButton1.Text = "v";
            this.roundButton1.UseVisualStyleBackColor = true;
            // 
            // SimForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(822, 457);
            this.Controls.Add(this.roundButton1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.negX);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SimForm";
            this.Text = "v";
            this.Load += new System.EventHandler(this.SimForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button negX;
        private RoundButton posX;
        private System.Windows.Forms.TextBox textBox1;
        private RoundButton roundButton1;
    }
}