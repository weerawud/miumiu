namespace Miumiu
{
    partial class FormMain
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
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainerData1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBoxTechCal = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.labelPeriod = new System.Windows.Forms.Label();
            this.comboBoxTimeframeTechCal = new System.Windows.Forms.ComboBox();
            this.buttonRefreshTechCal = new System.Windows.Forms.Button();
            this.dataGridViewTechnicalCal = new System.Windows.Forms.DataGridView();
            this.groupBoxControl = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelControl = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxTimeframeTimeframeCal = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonRun = new System.Windows.Forms.Button();
            this.buttonExport = new System.Windows.Forms.Button();
            this.comboBoxStartDate = new System.Windows.Forms.ComboBox();
            this.comboBoxEndDate = new System.Windows.Forms.ComboBox();
            this.comboBoxSymbols = new System.Windows.Forms.ComboBox();
            this.comboBoxVolFilter = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.splitContainerData2 = new System.Windows.Forms.SplitContainer();
            this.tabControlDataResult = new System.Windows.Forms.TabControl();
            this.tabPageResult1 = new System.Windows.Forms.TabPage();
            this.dataGridViewDataResult1 = new System.Windows.Forms.DataGridView();
            this.tabPageResult2 = new System.Windows.Forms.TabPage();
            this.dataGridViewDataResult2 = new System.Windows.Forms.DataGridView();
            this.tabPageResult3 = new System.Windows.Forms.TabPage();
            this.dataGridViewDataResult3 = new System.Windows.Forms.DataGridView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.dataGridViewDataResult4 = new System.Windows.Forms.DataGridView();
            this.textBoxDataLogs = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.progressBarBackTest = new System.Windows.Forms.ProgressBar();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.textBoxEventLog = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.saveFileDialogExport = new System.Windows.Forms.SaveFileDialog();
            this.tabControlMain.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerData1)).BeginInit();
            this.splitContainerData1.Panel1.SuspendLayout();
            this.splitContainerData1.Panel2.SuspendLayout();
            this.splitContainerData1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBoxTechCal.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTechnicalCal)).BeginInit();
            this.groupBoxControl.SuspendLayout();
            this.tableLayoutPanelControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerData2)).BeginInit();
            this.splitContainerData2.Panel1.SuspendLayout();
            this.splitContainerData2.Panel2.SuspendLayout();
            this.splitContainerData2.SuspendLayout();
            this.tabControlDataResult.SuspendLayout();
            this.tabPageResult1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult1)).BeginInit();
            this.tabPageResult2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult2)).BeginInit();
            this.tabPageResult3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult3)).BeginInit();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult4)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPage1);
            this.tabControlMain.Controls.Add(this.tabPage2);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1260, 600);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.splitContainerData1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1252, 574);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Data";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainerData1
            // 
            this.splitContainerData1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerData1.Location = new System.Drawing.Point(3, 3);
            this.splitContainerData1.Name = "splitContainerData1";
            // 
            // splitContainerData1.Panel1
            // 
            this.splitContainerData1.Panel1.Controls.Add(this.splitContainer1);
            this.splitContainerData1.Panel1.Controls.Add(this.groupBoxControl);
            // 
            // splitContainerData1.Panel2
            // 
            this.splitContainerData1.Panel2.Controls.Add(this.splitContainerData2);
            this.splitContainerData1.Size = new System.Drawing.Size(1246, 568);
            this.splitContainerData1.SplitterDistance = 282;
            this.splitContainerData1.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 172);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBoxTechCal);
            this.splitContainer1.Size = new System.Drawing.Size(282, 396);
            this.splitContainer1.SplitterDistance = 315;
            this.splitContainer1.TabIndex = 5;
            // 
            // groupBoxTechCal
            // 
            this.groupBoxTechCal.Controls.Add(this.tableLayoutPanel2);
            this.groupBoxTechCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxTechCal.Location = new System.Drawing.Point(0, 0);
            this.groupBoxTechCal.Name = "groupBoxTechCal";
            this.groupBoxTechCal.Size = new System.Drawing.Size(282, 315);
            this.groupBoxTechCal.TabIndex = 3;
            this.groupBoxTechCal.TabStop = false;
            this.groupBoxTechCal.Text = "Technical Calculate";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.Controls.Add(this.labelPeriod, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxTimeframeTechCal, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.buttonRefreshTechCal, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.dataGridViewTechnicalCal, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 4;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(276, 296);
            this.tableLayoutPanel2.TabIndex = 0;
            // 
            // labelPeriod
            // 
            this.labelPeriod.AutoSize = true;
            this.labelPeriod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelPeriod.Location = new System.Drawing.Point(3, 206);
            this.labelPeriod.Name = "labelPeriod";
            this.labelPeriod.Size = new System.Drawing.Size(159, 30);
            this.labelPeriod.TabIndex = 12;
            this.labelPeriod.Text = "Period";
            this.labelPeriod.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBoxTimeframeTechCal
            // 
            this.comboBoxTimeframeTechCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxTimeframeTechCal.FormattingEnabled = true;
            this.comboBoxTimeframeTechCal.Location = new System.Drawing.Point(168, 209);
            this.comboBoxTimeframeTechCal.Name = "comboBoxTimeframeTechCal";
            this.comboBoxTimeframeTechCal.Size = new System.Drawing.Size(105, 21);
            this.comboBoxTimeframeTechCal.TabIndex = 11;
            this.comboBoxTimeframeTechCal.SelectedIndexChanged += new System.EventHandler(this.comboBoxTimeframeTechCal_SelectedIndexChanged);
            // 
            // buttonRefreshTechCal
            // 
            this.buttonRefreshTechCal.Location = new System.Drawing.Point(168, 239);
            this.buttonRefreshTechCal.Name = "buttonRefreshTechCal";
            this.buttonRefreshTechCal.Size = new System.Drawing.Size(68, 23);
            this.buttonRefreshTechCal.TabIndex = 13;
            this.buttonRefreshTechCal.Text = "Refresh";
            this.buttonRefreshTechCal.UseVisualStyleBackColor = true;
            // 
            // dataGridViewTechnicalCal
            // 
            this.dataGridViewTechnicalCal.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.tableLayoutPanel2.SetColumnSpan(this.dataGridViewTechnicalCal, 2);
            this.dataGridViewTechnicalCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewTechnicalCal.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewTechnicalCal.Name = "dataGridViewTechnicalCal";
            this.dataGridViewTechnicalCal.Size = new System.Drawing.Size(270, 200);
            this.dataGridViewTechnicalCal.TabIndex = 14;
            // 
            // groupBoxControl
            // 
            this.groupBoxControl.Controls.Add(this.tableLayoutPanelControl);
            this.groupBoxControl.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxControl.Location = new System.Drawing.Point(0, 0);
            this.groupBoxControl.Name = "groupBoxControl";
            this.groupBoxControl.Size = new System.Drawing.Size(282, 172);
            this.groupBoxControl.TabIndex = 4;
            this.groupBoxControl.TabStop = false;
            this.groupBoxControl.Text = "Control";
            // 
            // tableLayoutPanelControl
            // 
            this.tableLayoutPanelControl.ColumnCount = 2;
            this.tableLayoutPanelControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanelControl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanelControl.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanelControl.Controls.Add(this.comboBoxTimeframeTimeframeCal, 1, 2);
            this.tableLayoutPanelControl.Controls.Add(this.label5, 0, 3);
            this.tableLayoutPanelControl.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanelControl.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanelControl.Controls.Add(this.buttonRun, 1, 4);
            this.tableLayoutPanelControl.Controls.Add(this.buttonExport, 1, 5);
            this.tableLayoutPanelControl.Controls.Add(this.comboBoxStartDate, 1, 0);
            this.tableLayoutPanelControl.Controls.Add(this.comboBoxEndDate, 1, 1);
            this.tableLayoutPanelControl.Controls.Add(this.comboBoxSymbols, 1, 3);
            this.tableLayoutPanelControl.Controls.Add(this.comboBoxVolFilter, 0, 5);
            this.tableLayoutPanelControl.Controls.Add(this.label1, 0, 4);
            this.tableLayoutPanelControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelControl.Location = new System.Drawing.Point(3, 16);
            this.tableLayoutPanelControl.Name = "tableLayoutPanelControl";
            this.tableLayoutPanelControl.RowCount = 6;
            this.tableLayoutPanelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanelControl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tableLayoutPanelControl.Size = new System.Drawing.Size(276, 153);
            this.tableLayoutPanelControl.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(3, 50);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(159, 25);
            this.label4.TabIndex = 12;
            this.label4.Text = "Timeframe (sec)";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBoxTimeframeTimeframeCal
            // 
            this.comboBoxTimeframeTimeframeCal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxTimeframeTimeframeCal.FormattingEnabled = true;
            this.comboBoxTimeframeTimeframeCal.Items.AddRange(new object[] {
            "1",
            "5",
            "10",
            "15",
            "30",
            "60",
            "300",
            "900",
            "1800",
            "3600"});
            this.comboBoxTimeframeTimeframeCal.Location = new System.Drawing.Point(168, 53);
            this.comboBoxTimeframeTimeframeCal.Name = "comboBoxTimeframeTimeframeCal";
            this.comboBoxTimeframeTimeframeCal.Size = new System.Drawing.Size(105, 21);
            this.comboBoxTimeframeTimeframeCal.TabIndex = 11;
            this.comboBoxTimeframeTimeframeCal.Tag = "";
            this.comboBoxTimeframeTimeframeCal.Text = "1";
            this.comboBoxTimeframeTimeframeCal.SelectedIndexChanged += new System.EventHandler(this.comboBoxTimeframeTimeframeCal_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.Location = new System.Drawing.Point(3, 75);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(159, 25);
            this.label5.TabIndex = 21;
            this.label5.Text = "Symbols";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(159, 25);
            this.label2.TabIndex = 13;
            this.label2.Text = "Start Date";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(159, 25);
            this.label3.TabIndex = 14;
            this.label3.Text = "End Date";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonRun
            // 
            this.buttonRun.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonRun.Location = new System.Drawing.Point(168, 103);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(105, 19);
            this.buttonRun.TabIndex = 0;
            this.buttonRun.Text = "Run All";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonExport
            // 
            this.buttonExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonExport.Location = new System.Drawing.Point(168, 128);
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.Size = new System.Drawing.Size(105, 22);
            this.buttonExport.TabIndex = 17;
            this.buttonExport.Text = "Export...";
            this.buttonExport.UseVisualStyleBackColor = true;
            this.buttonExport.Click += new System.EventHandler(this.buttonExportToExcel_Click);
            // 
            // comboBoxStartDate
            // 
            this.comboBoxStartDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxStartDate.FormattingEnabled = true;
            this.comboBoxStartDate.Location = new System.Drawing.Point(168, 3);
            this.comboBoxStartDate.Name = "comboBoxStartDate";
            this.comboBoxStartDate.Size = new System.Drawing.Size(105, 21);
            this.comboBoxStartDate.TabIndex = 18;
            this.comboBoxStartDate.SelectedIndexChanged += new System.EventHandler(this.comboBoxStartDate_SelectedIndexChanged);
            // 
            // comboBoxEndDate
            // 
            this.comboBoxEndDate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxEndDate.FormattingEnabled = true;
            this.comboBoxEndDate.Location = new System.Drawing.Point(168, 28);
            this.comboBoxEndDate.Name = "comboBoxEndDate";
            this.comboBoxEndDate.Size = new System.Drawing.Size(105, 21);
            this.comboBoxEndDate.TabIndex = 19;
            this.comboBoxEndDate.SelectedIndexChanged += new System.EventHandler(this.comboBoxEndDate_SelectedIndexChanged);
            // 
            // comboBoxSymbols
            // 
            this.comboBoxSymbols.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBoxSymbols.FormattingEnabled = true;
            this.comboBoxSymbols.Location = new System.Drawing.Point(168, 78);
            this.comboBoxSymbols.Name = "comboBoxSymbols";
            this.comboBoxSymbols.Size = new System.Drawing.Size(105, 21);
            this.comboBoxSymbols.TabIndex = 20;
            this.comboBoxSymbols.SelectedIndexChanged += new System.EventHandler(this.comboBoxSymbols_SelectedIndexChanged);
            // 
            // comboBoxVolFilter
            // 
            this.comboBoxVolFilter.FormattingEnabled = true;
            this.comboBoxVolFilter.Items.AddRange(new object[] {
            "0",
            "50",
            "75",
            "100",
            "125",
            "150",
            "200",
            "250",
            "300",
            "350",
            "400",
            "450",
            "500",
            "1000",
            "1200",
            "1500",
            "2000"});
            this.comboBoxVolFilter.Location = new System.Drawing.Point(3, 128);
            this.comboBoxVolFilter.Name = "comboBoxVolFilter";
            this.comboBoxVolFilter.Size = new System.Drawing.Size(104, 21);
            this.comboBoxVolFilter.TabIndex = 22;
            this.comboBoxVolFilter.Text = "0";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 100);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(159, 25);
            this.label1.TabIndex = 23;
            this.label1.Text = "Filter Vol";
            // 
            // splitContainerData2
            // 
            this.splitContainerData2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerData2.Location = new System.Drawing.Point(0, 0);
            this.splitContainerData2.Name = "splitContainerData2";
            this.splitContainerData2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerData2.Panel1
            // 
            this.splitContainerData2.Panel1.Controls.Add(this.tabControlDataResult);
            // 
            // splitContainerData2.Panel2
            // 
            this.splitContainerData2.Panel2.Controls.Add(this.textBoxDataLogs);
            this.splitContainerData2.Size = new System.Drawing.Size(960, 568);
            this.splitContainerData2.SplitterDistance = 324;
            this.splitContainerData2.TabIndex = 0;
            // 
            // tabControlDataResult
            // 
            this.tabControlDataResult.Controls.Add(this.tabPageResult1);
            this.tabControlDataResult.Controls.Add(this.tabPageResult2);
            this.tabControlDataResult.Controls.Add(this.tabPageResult3);
            this.tabControlDataResult.Controls.Add(this.tabPage3);
            this.tabControlDataResult.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlDataResult.Location = new System.Drawing.Point(0, 0);
            this.tabControlDataResult.Name = "tabControlDataResult";
            this.tabControlDataResult.SelectedIndex = 0;
            this.tabControlDataResult.Size = new System.Drawing.Size(960, 324);
            this.tabControlDataResult.TabIndex = 1;
            // 
            // tabPageResult1
            // 
            this.tabPageResult1.Controls.Add(this.dataGridViewDataResult1);
            this.tabPageResult1.Location = new System.Drawing.Point(4, 22);
            this.tabPageResult1.Name = "tabPageResult1";
            this.tabPageResult1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageResult1.Size = new System.Drawing.Size(952, 298);
            this.tabPageResult1.TabIndex = 0;
            this.tabPageResult1.Text = "Result1";
            this.tabPageResult1.UseVisualStyleBackColor = true;
            // 
            // dataGridViewDataResult1
            // 
            this.dataGridViewDataResult1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDataResult1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewDataResult1.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewDataResult1.Name = "dataGridViewDataResult1";
            this.dataGridViewDataResult1.Size = new System.Drawing.Size(946, 292);
            this.dataGridViewDataResult1.TabIndex = 0;
            // 
            // tabPageResult2
            // 
            this.tabPageResult2.Controls.Add(this.dataGridViewDataResult2);
            this.tabPageResult2.Location = new System.Drawing.Point(4, 22);
            this.tabPageResult2.Name = "tabPageResult2";
            this.tabPageResult2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageResult2.Size = new System.Drawing.Size(952, 298);
            this.tabPageResult2.TabIndex = 1;
            this.tabPageResult2.Text = "Result2";
            this.tabPageResult2.UseVisualStyleBackColor = true;
            // 
            // dataGridViewDataResult2
            // 
            this.dataGridViewDataResult2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDataResult2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewDataResult2.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewDataResult2.Name = "dataGridViewDataResult2";
            this.dataGridViewDataResult2.Size = new System.Drawing.Size(946, 292);
            this.dataGridViewDataResult2.TabIndex = 0;
            // 
            // tabPageResult3
            // 
            this.tabPageResult3.Controls.Add(this.dataGridViewDataResult3);
            this.tabPageResult3.Location = new System.Drawing.Point(4, 22);
            this.tabPageResult3.Name = "tabPageResult3";
            this.tabPageResult3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageResult3.Size = new System.Drawing.Size(952, 298);
            this.tabPageResult3.TabIndex = 2;
            this.tabPageResult3.Text = "Result3";
            this.tabPageResult3.UseVisualStyleBackColor = true;
            // 
            // dataGridViewDataResult3
            // 
            this.dataGridViewDataResult3.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDataResult3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewDataResult3.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewDataResult3.Name = "dataGridViewDataResult3";
            this.dataGridViewDataResult3.Size = new System.Drawing.Size(946, 292);
            this.dataGridViewDataResult3.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dataGridViewDataResult4);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(952, 298);
            this.tabPage3.TabIndex = 3;
            this.tabPage3.Text = "Result4";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // dataGridViewDataResult4
            // 
            this.dataGridViewDataResult4.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewDataResult4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewDataResult4.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewDataResult4.Name = "dataGridViewDataResult4";
            this.dataGridViewDataResult4.Size = new System.Drawing.Size(946, 292);
            this.dataGridViewDataResult4.TabIndex = 1;
            // 
            // textBoxDataLogs
            // 
            this.textBoxDataLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxDataLogs.Location = new System.Drawing.Point(0, 0);
            this.textBoxDataLogs.Multiline = true;
            this.textBoxDataLogs.Name = "textBoxDataLogs";
            this.textBoxDataLogs.ReadOnly = true;
            this.textBoxDataLogs.Size = new System.Drawing.Size(960, 240);
            this.textBoxDataLogs.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.progressBarBackTest);
            this.tabPage2.Controls.Add(this.button4);
            this.tabPage2.Controls.Add(this.button3);
            this.tabPage2.Controls.Add(this.textBoxEventLog);
            this.tabPage2.Controls.Add(this.button1);
            this.tabPage2.Controls.Add(this.button2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1252, 574);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // progressBarBackTest
            // 
            this.progressBarBackTest.Location = new System.Drawing.Point(202, 118);
            this.progressBarBackTest.Margin = new System.Windows.Forms.Padding(2);
            this.progressBarBackTest.Name = "progressBarBackTest";
            this.progressBarBackTest.Size = new System.Drawing.Size(682, 23);
            this.progressBarBackTest.TabIndex = 7;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(94, 440);
            this.button4.Margin = new System.Windows.Forms.Padding(2);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(89, 22);
            this.button4.TabIndex = 6;
            this.button4.Text = "Export All Tick";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(896, 71);
            this.button3.Margin = new System.Windows.Forms.Padding(2);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(56, 28);
            this.button3.TabIndex = 5;
            this.button3.Text = "Search";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // textBoxEventLog
            // 
            this.textBoxEventLog.Location = new System.Drawing.Point(202, 147);
            this.textBoxEventLog.Multiline = true;
            this.textBoxEventLog.Name = "textBoxEventLog";
            this.textBoxEventLog.ReadOnly = true;
            this.textBoxEventLog.Size = new System.Drawing.Size(764, 317);
            this.textBoxEventLog.TabIndex = 4;
            this.textBoxEventLog.TextChanged += new System.EventHandler(this.textBoxEventLog_TextChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(528, 60);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "MT";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(108, 118);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Backtest";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1260, 600);
            this.Controls.Add(this.tabControlMain);
            this.Name = "FormMain";
            this.Text = "Miumiu QuanTrade";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.tabControlMain.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainerData1.Panel1.ResumeLayout(false);
            this.splitContainerData1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerData1)).EndInit();
            this.splitContainerData1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBoxTechCal.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewTechnicalCal)).EndInit();
            this.groupBoxControl.ResumeLayout(false);
            this.tableLayoutPanelControl.ResumeLayout(false);
            this.tableLayoutPanelControl.PerformLayout();
            this.splitContainerData2.Panel1.ResumeLayout(false);
            this.splitContainerData2.Panel2.ResumeLayout(false);
            this.splitContainerData2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerData2)).EndInit();
            this.splitContainerData2.ResumeLayout(false);
            this.tabControlDataResult.ResumeLayout(false);
            this.tabPageResult1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult1)).EndInit();
            this.tabPageResult2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult2)).EndInit();
            this.tabPageResult3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult3)).EndInit();
            this.tabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewDataResult4)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.SplitContainer splitContainerData1;
        private System.Windows.Forms.SplitContainer splitContainerData2;
        private System.Windows.Forms.DataGridView dataGridViewDataResult1;
        private System.Windows.Forms.TextBox textBoxDataLogs;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.TabControl tabControlDataResult;
        private System.Windows.Forms.TabPage tabPageResult1;
        private System.Windows.Forms.TabPage tabPageResult2;
        private System.Windows.Forms.DataGridView dataGridViewDataResult2;
        private System.Windows.Forms.TabPage tabPageResult3;
        private System.Windows.Forms.DataGridView dataGridViewDataResult3;
        private System.Windows.Forms.SaveFileDialog saveFileDialogExport;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxTimeframeTimeframeCal;
        private System.Windows.Forms.GroupBox groupBoxTechCal;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label labelPeriod;
        private System.Windows.Forms.ComboBox comboBoxTimeframeTechCal;
        private System.Windows.Forms.Button buttonRefreshTechCal;
        private System.Windows.Forms.DataGridView dataGridViewTechnicalCal;
        private System.Windows.Forms.GroupBox groupBoxControl;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelControl;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.ComboBox comboBoxStartDate;
        private System.Windows.Forms.ComboBox comboBoxEndDate;
        private System.Windows.Forms.ComboBox comboBoxSymbols;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBoxEventLog;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.DataGridView dataGridViewDataResult4;
        private System.Windows.Forms.ComboBox comboBoxVolFilter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.ProgressBar progressBarBackTest;
    }
}

