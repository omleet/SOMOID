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
            this.door_open = new System.Windows.Forms.Button();
            this.door_close = new System.Windows.Forms.Button();
            this.notificationList = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // door_open
            // 
            this.door_open.Location = new System.Drawing.Point(24, 24);
            this.door_open.Name = "door_open";
            this.door_open.Size = new System.Drawing.Size(150, 62);
            this.door_open.TabIndex = 0;
            this.door_open.Text = "OPEN";
            this.door_open.UseVisualStyleBackColor = true;
            this.door_open.Click += new System.EventHandler(this.door_open_Click);
            // 
            // door_close
            // 
            this.door_close.Location = new System.Drawing.Point(24, 102);
            this.door_close.Name = "door_close";
            this.door_close.Size = new System.Drawing.Size(150, 62);
            this.door_close.TabIndex = 1;
            this.door_close.Text = "CLOSE";
            this.door_close.UseVisualStyleBackColor = true;
            this.door_close.Click += new System.EventHandler(this.door_close_Click);
            // 
            // notificationList
            // 
            this.notificationList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.notificationList.FormattingEnabled = true;
            this.notificationList.HorizontalScrollbar = true;
            this.notificationList.IntegralHeight = false;
            this.notificationList.Location = new System.Drawing.Point(200, 24);
            this.notificationList.Name = "notificationList";
            this.notificationList.Size = new System.Drawing.Size(300, 228);
            this.notificationList.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(534, 276);
            this.Controls.Add(this.notificationList);
            this.Controls.Add(this.door_close);
            this.Controls.Add(this.door_open);
            this.Name = "Form1";
            this.Text = "Application B";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button door_open;
        private System.Windows.Forms.Button door_close;
        private System.Windows.Forms.ListBox notificationList;
    }
}

