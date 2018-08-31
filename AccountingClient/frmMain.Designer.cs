namespace AccountingClient
{
    internal partial class frmMain
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

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.textBoxCommand = new System.Windows.Forms.TextBox();
            this.scintilla = new ScintillaNET.Scintilla();
            this.SuspendLayout();
            // 
            // textBoxCommand
            // 
            this.textBoxCommand.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxCommand.Font = new System.Drawing.Font("Microsoft YaHei Mono", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBoxCommand.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.textBoxCommand.Location = new System.Drawing.Point(0, 0);
            this.textBoxCommand.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBoxCommand.Name = "textBoxCommand";
            this.textBoxCommand.Size = new System.Drawing.Size(970, 23);
            this.textBoxCommand.TabIndex = 4;
            this.textBoxCommand.TabStop = false;
            // 
            // scintilla
            // 
            this.scintilla.CaretPeriod = 200;
            this.scintilla.EndAtLastLine = false;
            this.scintilla.Lexer = ScintillaNET.Lexer.Cpp;
            this.scintilla.Location = new System.Drawing.Point(55, 159);
            this.scintilla.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.scintilla.Name = "scintilla";
            this.scintilla.Size = new System.Drawing.Size(590, 341);
            this.scintilla.TabIndex = 0;
            this.scintilla.TabStop = false;
            this.scintilla.WrapMode = ScintillaNET.WrapMode.Char;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(970, 574);
            this.Controls.Add(this.scintilla);
            this.Controls.Add(this.textBoxCommand);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "frmMain";
            this.Text = "Accounting Server";
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxCommand;
        private ScintillaNET.Scintilla scintilla;
    }
}

