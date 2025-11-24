namespace Summarizer
{
    partial class Summarizer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonToConvert = new Button();
            textBoxInput = new TextBox();
            textBoxOutput = new TextBox();
            labelInput = new Label();
            labelOutput = new Label();
            buttonClear = new Button();
            buttonCopyOutput = new Button();
            checkBoxAlwaysTop = new CheckBox();
            SuspendLayout();
            // 
            // buttonToConvert
            // 
            buttonToConvert.Location = new Point(343, 121);
            buttonToConvert.Name = "buttonToConvert";
            buttonToConvert.Size = new Size(125, 33);
            buttonToConvert.TabIndex = 0;
            buttonToConvert.Text = "변환하기";
            buttonToConvert.UseVisualStyleBackColor = true;
            buttonToConvert.Click += ButtonConvert_Click;
            // 
            // textBoxInput
            // 
            textBoxInput.Location = new Point(33, 34);
            textBoxInput.MaxLength = 65535;
            textBoxInput.Multiline = true;
            textBoxInput.Name = "textBoxInput";
            textBoxInput.ScrollBars = ScrollBars.Vertical;
            textBoxInput.Size = new Size(279, 359);
            textBoxInput.TabIndex = 1;
            // 
            // textBoxOutput
            // 
            textBoxOutput.Location = new Point(498, 34);
            textBoxOutput.MaxLength = 65535;
            textBoxOutput.Multiline = true;
            textBoxOutput.Name = "textBoxOutput";
            textBoxOutput.ScrollBars = ScrollBars.Vertical;
            textBoxOutput.Size = new Size(281, 359);
            textBoxOutput.TabIndex = 2;
            // 
            // labelInput
            // 
            labelInput.AutoSize = true;
            labelInput.Location = new Point(33, 16);
            labelInput.Name = "labelInput";
            labelInput.Size = new Size(111, 15);
            labelInput.TabIndex = 3;
            labelInput.Text = "변환할 텍스트 입력";
            // 
            // labelOutput
            // 
            labelOutput.AutoSize = true;
            labelOutput.Location = new Point(498, 16);
            labelOutput.Name = "labelOutput";
            labelOutput.Size = new Size(59, 15);
            labelOutput.TabIndex = 4;
            labelOutput.Text = "변환 결과";
            // 
            // buttonClear
            // 
            buttonClear.Location = new Point(343, 270);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(125, 33);
            buttonClear.TabIndex = 5;
            buttonClear.Text = "비우기";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // buttonCopyOutput
            // 
            buttonCopyOutput.Location = new Point(343, 172);
            buttonCopyOutput.Name = "buttonCopyOutput";
            buttonCopyOutput.Size = new Size(125, 33);
            buttonCopyOutput.TabIndex = 6;
            buttonCopyOutput.Text = "복사하기";
            buttonCopyOutput.UseVisualStyleBackColor = true;
            buttonCopyOutput.Click += buttonCopyOutput_Click;
            // 
            // checkBoxAlwaysTop
            // 
            checkBoxAlwaysTop.AutoSize = true;
            checkBoxAlwaysTop.Location = new Point(345, 43);
            checkBoxAlwaysTop.Name = "checkBoxAlwaysTop";
            checkBoxAlwaysTop.Size = new Size(122, 19);
            checkBoxAlwaysTop.TabIndex = 7;
            checkBoxAlwaysTop.Text = "항상 맨 위로 고정";
            checkBoxAlwaysTop.UseVisualStyleBackColor = true;
            checkBoxAlwaysTop.CheckedChanged += checkBoxAlwaysTop_CheckedChanged;
            // 
            // Summarizer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(810, 419);
            Controls.Add(checkBoxAlwaysTop);
            Controls.Add(buttonCopyOutput);
            Controls.Add(buttonClear);
            Controls.Add(labelOutput);
            Controls.Add(labelInput);
            Controls.Add(textBoxOutput);
            Controls.Add(textBoxInput);
            Controls.Add(buttonToConvert);
            Margin = new Padding(2);
            Name = "Summarizer";
            Text = "Summarizer";
            Load += Summarizer_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonToConvert;
        private TextBox textBoxInput;
        private TextBox textBoxOutput;
        private Label labelInput;
        private Label labelOutput;
        private Button buttonClear;
        private Button buttonCopyOutput;
        private CheckBox checkBoxAlwaysTop;
    }
}
