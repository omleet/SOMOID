namespace Application_B
{
    partial class Form1
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
            this.light_on = new System.Windows.Forms.Button();
            this.light_off = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // light_on
            // 
            this.light_on.Location = new System.Drawing.Point(74, 77);
            this.light_on.Name = "light_on";
            this.light_on.Size = new System.Drawing.Size(133, 62);
            this.light_on.TabIndex = 0;
            this.light_on.Text = "Light ON";
            this.light_on.UseVisualStyleBackColor = true;
            this.light_on.Click += new System.EventHandler(this.light_on_Click);
            // 
            // light_off
            // 
            this.light_off.Location = new System.Drawing.Point(74, 145);
            this.light_off.Name = "light_off";
            this.light_off.Size = new System.Drawing.Size(133, 62);
            this.light_off.TabIndex = 1;
            this.light_off.Text = "Light OFF";
            this.light_off.UseVisualStyleBackColor = true;
            this.light_off.Click += new System.EventHandler(this.light_off_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(276, 279);
            this.Controls.Add(this.light_off);
            this.Controls.Add(this.light_on);
            this.Name = "Form1";
            this.Text = "Application B";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button light_on;
        private System.Windows.Forms.Button light_off;
    }
}

