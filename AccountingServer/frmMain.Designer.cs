using System.Security.AccessControl;

namespace AccountingServer
{
    partial class frmMain
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.textBoxCommand = new System.Windows.Forms.TextBox();
            this.scintilla = new ScintillaNET.Scintilla();
            this.acMenu = new AutocompleteMenuNS.AutocompleteMenu();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // textBoxCommand
            // 
            this.acMenu.SetAutocompleteMenu(this.textBoxCommand, null);
            this.textBoxCommand.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxCommand.Font = new System.Drawing.Font("Microsoft YaHei Mono", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.textBoxCommand.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.textBoxCommand.Location = new System.Drawing.Point(0, 0);
            this.textBoxCommand.Name = "textBoxCommand";
            this.textBoxCommand.Size = new System.Drawing.Size(1455, 31);
            this.textBoxCommand.TabIndex = 4;
            this.textBoxCommand.TabStop = false;
            // 
            // scintilla
            // 
            this.scintilla.CaretPeriod = 200;
            this.scintilla.EndAtLastLine = false;
            this.scintilla.Lexer = ScintillaNET.Lexer.Cpp;
            this.scintilla.Location = new System.Drawing.Point(83, 238);
            this.scintilla.Name = "scintilla";
            this.scintilla.Size = new System.Drawing.Size(883, 509);
            this.scintilla.TabIndex = 0;
            this.scintilla.TabStop = false;
            this.scintilla.WrapMode = ScintillaNET.WrapMode.Char;
            this.scintilla.KeyUp += new System.Windows.Forms.KeyEventHandler(this.scintilla_KeyUp);
            // 
            // acMenu
            // 
            this.acMenu.AllowsTabKey = true;
            this.acMenu.AppearInterval = 50;
            this.acMenu.Colors = ((AutocompleteMenuNS.Colors)(resources.GetObject("acMenu.Colors")));
            this.acMenu.Font = new System.Drawing.Font("Microsoft YaHei Mono", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.acMenu.ImageList = null;
            this.acMenu.Items = new string[0];
            this.acMenu.LeftPadding = 36;
            this.acMenu.TargetControlWrapper = null;
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(654, 104);
            this.chart1.Name = "chart1";
            this.chart1.Palette = System.Windows.Forms.DataVisualization.Charting.ChartColorPalette.None;
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(665, 354);
            this.chart1.TabIndex = 7;
            this.chart1.TabStop = false;
            this.chart1.Text = "chart1";
            this.chart1.Visible = false;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1455, 861);
            this.Controls.Add(this.scintilla);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.textBoxCommand);
            this.Name = "frmMain";
            this.Text = "Accounting Server";
            this.Shown += new System.EventHandler(this.frmMain_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxCommand;
        private ScintillaNET.Scintilla scintilla;
        private AutocompleteMenuNS.AutocompleteMenu acMenu;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
    }
}

