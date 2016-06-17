namespace CAV.WinForms
{
    partial class InputBoxForm
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
            this.lbDescriptionText = new System.Windows.Forms.Label();
            this.tbInputText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lbDescriptionText
            // 
            this.lbDescriptionText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbDescriptionText.AutoSize = true;
            this.lbDescriptionText.Location = new System.Drawing.Point(12, 9);
            this.lbDescriptionText.MaximumSize = new System.Drawing.Size(200, 200);
            this.lbDescriptionText.Name = "lbDescriptionText";
            this.lbDescriptionText.Size = new System.Drawing.Size(88, 13);
            this.lbDescriptionText.TabIndex = 10000;
            this.lbDescriptionText.Text = "Текст описания";
            // 
            // tbInputText
            // 
            this.tbInputText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbInputText.Location = new System.Drawing.Point(15, 27);
            this.tbInputText.Name = "tbInputText";
            this.tbInputText.Size = new System.Drawing.Size(204, 20);
            this.tbInputText.TabIndex = 1;
            this.tbInputText.Text = "Текст по умолчанию";
            // 
            // InputBoxForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(234, 102);
            this.Controls.Add(this.tbInputText);
            this.Controls.Add(this.lbDescriptionText);
            this.MaximumSize = new System.Drawing.Size(400, 400);
            this.MinimumSize = new System.Drawing.Size(250, 140);
            this.Name = "InputBoxForm";
            this.Text = "Вввод значения";
            this.Controls.SetChildIndex(this.lbDescriptionText, 0);
            this.Controls.SetChildIndex(this.tbInputText, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Label lbDescriptionText;
        public System.Windows.Forms.TextBox tbInputText;

    }
}