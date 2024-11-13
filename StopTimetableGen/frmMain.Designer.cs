namespace StopTimetableGen
{
    partial class frmMain
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
            this.cbRoute = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.dtStartDate = new System.Windows.Forms.DateTimePicker();
            this.dtEndDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.btnGo = new System.Windows.Forms.Button();
            this.pbStatus = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.chAllDays = new System.Windows.Forms.CheckBox();
            this.chWorkdays = new System.Windows.Forms.CheckBox();
            this.chWeekends = new System.Windows.Forms.CheckBox();
            this.chSaturdays = new System.Windows.Forms.CheckBox();
            this.chSundays = new System.Windows.Forms.CheckBox();
            this.cbTemplate = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cbIgnoredStop = new System.Windows.Forms.ComboBox();
            this.btnAddIgnoredStop = new System.Windows.Forms.Button();
            this.lbIgnoredStops = new System.Windows.Forms.ListBox();
            this.txtOutputFolder = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label6 = new System.Windows.Forms.Label();
            this.btnOutputFolderSelect = new System.Windows.Forms.Button();
            this.chBenevolentDays = new System.Windows.Forms.CheckBox();
            this.chcIgnoreLowFloor = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // cbRoute
            // 
            this.cbRoute.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbRoute.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbRoute.FormattingEnabled = true;
            this.cbRoute.Location = new System.Drawing.Point(102, 12);
            this.cbRoute.Name = "cbRoute";
            this.cbRoute.Size = new System.Drawing.Size(382, 21);
            this.cbRoute.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Linka";
            // 
            // dtStartDate
            // 
            this.dtStartDate.Location = new System.Drawing.Point(102, 40);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(200, 20);
            this.dtStartDate.TabIndex = 2;
            this.dtStartDate.Value = new System.DateTime(2024, 12, 15, 0, 0, 0, 0);
            // 
            // dtEndDate
            // 
            this.dtEndDate.Location = new System.Drawing.Point(102, 67);
            this.dtEndDate.Name = "dtEndDate";
            this.dtEndDate.Size = new System.Drawing.Size(200, 20);
            this.dtEndDate.TabIndex = 3;
            this.dtEndDate.Value = new System.DateTime(2025, 12, 12, 0, 0, 0, 0);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Datum od";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(53, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Datum do";
            // 
            // btnGo
            // 
            this.btnGo.Location = new System.Drawing.Point(12, 361);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(472, 27);
            this.btnGo.TabIndex = 6;
            this.btnGo.Text = "Generovat";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // pbStatus
            // 
            this.pbStatus.Location = new System.Drawing.Point(12, 419);
            this.pbStatus.MarqueeAnimationSpeed = 20;
            this.pbStatus.Name = "pbStatus";
            this.pbStatus.Size = new System.Drawing.Size(472, 24);
            this.pbStatus.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.pbStatus.TabIndex = 7;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(9, 403);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(68, 13);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "Načítání dat";
            // 
            // chAllDays
            // 
            this.chAllDays.AutoSize = true;
            this.chAllDays.Location = new System.Drawing.Point(12, 159);
            this.chAllDays.Name = "chAllDays";
            this.chAllDays.Size = new System.Drawing.Size(83, 17);
            this.chAllDays.TabIndex = 11;
            this.chAllDays.Text = "Celotýdenní";
            this.chAllDays.UseVisualStyleBackColor = true;
            // 
            // chWorkdays
            // 
            this.chWorkdays.AutoSize = true;
            this.chWorkdays.Location = new System.Drawing.Point(12, 183);
            this.chWorkdays.Name = "chWorkdays";
            this.chWorkdays.Size = new System.Drawing.Size(90, 17);
            this.chWorkdays.TabIndex = 12;
            this.chWorkdays.Text = "Pracovní dny";
            this.chWorkdays.UseVisualStyleBackColor = true;
            // 
            // chWeekends
            // 
            this.chWeekends.AutoSize = true;
            this.chWeekends.Location = new System.Drawing.Point(175, 159);
            this.chWeekends.Name = "chWeekends";
            this.chWeekends.Size = new System.Drawing.Size(66, 17);
            this.chWeekends.TabIndex = 13;
            this.chWeekends.Text = "Víkendy";
            this.chWeekends.UseVisualStyleBackColor = true;
            // 
            // chSaturdays
            // 
            this.chSaturdays.AutoSize = true;
            this.chSaturdays.Location = new System.Drawing.Point(175, 183);
            this.chSaturdays.Name = "chSaturdays";
            this.chSaturdays.Size = new System.Drawing.Size(59, 17);
            this.chSaturdays.TabIndex = 14;
            this.chSaturdays.Text = "Soboty";
            this.chSaturdays.UseVisualStyleBackColor = true;
            // 
            // chSundays
            // 
            this.chSundays.AutoSize = true;
            this.chSundays.Location = new System.Drawing.Point(324, 159);
            this.chSundays.Name = "chSundays";
            this.chSundays.Size = new System.Drawing.Size(60, 17);
            this.chSundays.TabIndex = 15;
            this.chSundays.Text = "Neděle";
            this.chSundays.UseVisualStyleBackColor = true;
            // 
            // cbTemplate
            // 
            this.cbTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTemplate.FormattingEnabled = true;
            this.cbTemplate.Location = new System.Drawing.Point(102, 93);
            this.cbTemplate.Name = "cbTemplate";
            this.cbTemplate.Size = new System.Drawing.Size(289, 21);
            this.cbTemplate.TabIndex = 16;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 96);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(46, 13);
            this.label4.TabIndex = 17;
            this.label4.Text = "Šablona";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 220);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 19;
            this.label5.Text = "Ignorovat zastávku";
            // 
            // cbIgnoredStop
            // 
            this.cbIgnoredStop.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbIgnoredStop.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbIgnoredStop.FormattingEnabled = true;
            this.cbIgnoredStop.Location = new System.Drawing.Point(116, 217);
            this.cbIgnoredStop.Name = "cbIgnoredStop";
            this.cbIgnoredStop.Size = new System.Drawing.Size(275, 21);
            this.cbIgnoredStop.TabIndex = 20;
            // 
            // btnAddIgnoredStop
            // 
            this.btnAddIgnoredStop.Location = new System.Drawing.Point(397, 215);
            this.btnAddIgnoredStop.Name = "btnAddIgnoredStop";
            this.btnAddIgnoredStop.Size = new System.Drawing.Size(87, 23);
            this.btnAddIgnoredStop.TabIndex = 21;
            this.btnAddIgnoredStop.Text = "Přidat";
            this.btnAddIgnoredStop.UseVisualStyleBackColor = true;
            this.btnAddIgnoredStop.Click += new System.EventHandler(this.btnAddIgnoredStop_Click);
            // 
            // lbIgnoredStops
            // 
            this.lbIgnoredStops.FormattingEnabled = true;
            this.lbIgnoredStops.Location = new System.Drawing.Point(12, 244);
            this.lbIgnoredStops.Name = "lbIgnoredStops";
            this.lbIgnoredStops.Size = new System.Drawing.Size(472, 82);
            this.lbIgnoredStops.TabIndex = 22;
            this.lbIgnoredStops.DoubleClick += new System.EventHandler(this.lbIgnoredStops_DoubleClick);
            // 
            // txtOutputFolder
            // 
            this.txtOutputFolder.Location = new System.Drawing.Point(102, 120);
            this.txtOutputFolder.Name = "txtOutputFolder";
            this.txtOutputFolder.ReadOnly = true;
            this.txtOutputFolder.Size = new System.Drawing.Size(350, 20);
            this.txtOutputFolder.TabIndex = 23;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 123);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(82, 13);
            this.label6.TabIndex = 24;
            this.label6.Text = "Výstupní složka";
            // 
            // btnOutputFolderSelect
            // 
            this.btnOutputFolderSelect.Location = new System.Drawing.Point(458, 118);
            this.btnOutputFolderSelect.Name = "btnOutputFolderSelect";
            this.btnOutputFolderSelect.Size = new System.Drawing.Size(26, 23);
            this.btnOutputFolderSelect.TabIndex = 25;
            this.btnOutputFolderSelect.Text = "...";
            this.btnOutputFolderSelect.UseVisualStyleBackColor = true;
            this.btnOutputFolderSelect.Click += new System.EventHandler(this.btnOutputFolderSelect_Click);
            // 
            // chBenevolentDays
            // 
            this.chBenevolentDays.AutoSize = true;
            this.chBenevolentDays.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.chBenevolentDays.Location = new System.Drawing.Point(303, 182);
            this.chBenevolentDays.Name = "chBenevolentDays";
            this.chBenevolentDays.Size = new System.Drawing.Size(181, 17);
            this.chBenevolentDays.TabIndex = 26;
            this.chBenevolentDays.Text = "Aplikovat benevolentní kalendář";
            this.chBenevolentDays.UseVisualStyleBackColor = true;
            // 
            // chcIgnoreLowFloor
            // 
            this.chcIgnoreLowFloor.AutoSize = true;
            this.chcIgnoreLowFloor.Location = new System.Drawing.Point(12, 332);
            this.chcIgnoreLowFloor.Name = "chcIgnoreLowFloor";
            this.chcIgnoreLowFloor.Size = new System.Drawing.Size(152, 17);
            this.chcIgnoreLowFloor.TabIndex = 27;
            this.chcIgnoreLowFloor.Text = "Ignorovat nízkopodlažnost";
            this.chcIgnoreLowFloor.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            this.AcceptButton = this.btnGo;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(496, 455);
            this.Controls.Add(this.chcIgnoreLowFloor);
            this.Controls.Add(this.chBenevolentDays);
            this.Controls.Add(this.btnOutputFolderSelect);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtOutputFolder);
            this.Controls.Add(this.lbIgnoredStops);
            this.Controls.Add(this.btnAddIgnoredStop);
            this.Controls.Add(this.cbIgnoredStop);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbTemplate);
            this.Controls.Add(this.chSundays);
            this.Controls.Add(this.chSaturdays);
            this.Controls.Add(this.chWeekends);
            this.Controls.Add(this.chWorkdays);
            this.Controls.Add(this.chAllDays);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pbStatus);
            this.Controls.Add(this.btnGo);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.dtEndDate);
            this.Controls.Add(this.dtStartDate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbRoute);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.Text = "Generování ZJŘ";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbRoute;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtStartDate;
        private System.Windows.Forms.DateTimePicker dtEndDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnGo;
        private System.Windows.Forms.ProgressBar pbStatus;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.CheckBox chAllDays;
        private System.Windows.Forms.CheckBox chWorkdays;
        private System.Windows.Forms.CheckBox chWeekends;
        private System.Windows.Forms.CheckBox chSaturdays;
        private System.Windows.Forms.CheckBox chSundays;
        private System.Windows.Forms.ComboBox cbTemplate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cbIgnoredStop;
        private System.Windows.Forms.Button btnAddIgnoredStop;
        private System.Windows.Forms.ListBox lbIgnoredStops;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnOutputFolderSelect;
        private System.Windows.Forms.CheckBox chBenevolentDays;
        private System.Windows.Forms.CheckBox chcIgnoreLowFloor;
    }
}

