using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace MIPS_Simulator1
{
    public partial class SimulatorForm : Form
    {
        private MIPS mips;
        private int currentRowIndex = -1;
        private int currentLineIndex = 0;
        private string[] oldRegisterValues;

        public SimulatorForm()
        {
            InitializeComponent();
            mips = new MIPS();
            InitializeDMTable();
            InitializeIMTable();
            InitializeRegisterTable();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] assemblyCodeArray = richTextBox1.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> assemblyCode = assemblyCodeArray.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            List<string> hexMachineCode = Compiler.CompileToHex(assemblyCode);
            List<string> binMachineCode = Compiler.CompileToBin(assemblyCode);
            int[] binMachineCodeInts = binMachineCode.Select(bin => unchecked((int)(uint)Convert.ToUInt32(bin, 2))).ToArray();
            mips.SetIM(assemblyCode.ToArray(), binMachineCodeInts);
            UpdateInstructionMemory(hexMachineCode, assemblyCode);
            UpdateDataMemoryTable(mips.DMToHex());
            UpdateRegisterTable();
            richTextBox2.Text = Compiler.machineCodeText;
            currentLineIndex1 = -1;
            currentLineIndex2 = -1;
            lastLine1 = false;
            lastLine2 = false;
            }
        private void button2_Click(object sender, EventArgs e)
        {
            oldRegisterValues = mips.RegToHex().ToArray();
            ClearRowHighlight(currentLineIndex);
            mips.Step();
            UpdateRegisterTable();
            UpdateDataMemoryTable(mips.DMToHex());
            HighlightCurrentLine();
            HighlightChangedCellsInColumn3();
            executionTraceBox.Text = mips.GetExecutionTrace();
            executionTraceBox.SelectionStart = executionTraceBox.Text.Length;
            executionTraceBox.ScrollToCaret();
        }
        private void HighlightChangedCellsInColumn3()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string cellValue = row.Cells[2].Value?.ToString();
                string previousValue = row.Cells[2].Tag?.ToString();

                if (cellValue != previousValue)
                {
                    row.Cells[2].Style.BackColor = Color.LightPink;
                    row.Cells[2].Tag = cellValue;
                }
                else
                {
                    row.Cells[2].Style.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                }
            }
        }
        private int currentLineIndex1 = -1;
        private int currentLineIndex2 = -1;
        private bool lastLine1 = false;
        private bool lastLine2 = false;
        private void HighlightCurrentLine()
        {
            int lineIndex1 = (mips.pc / 4)-1;
            if (lineIndex1 < richTextBox1.Lines.Length && !lastLine1)
            {
                ClearRowHighlight(currentLineIndex1);
                int start = richTextBox1.GetFirstCharIndexFromLine(lineIndex1);
                int length = richTextBox1.Lines[lineIndex1].Length;
                richTextBox1.Select(start, length);
                richTextBox1.SelectionBackColor = Color.Aqua;
                currentLineIndex1 = lineIndex1;
                if (lineIndex1 == richTextBox1.Lines.Length-1)
                {
                    lastLine1 = true;
                }

            }
            else if (lineIndex1 < richTextBox1.Lines.Length && lastLine1)
            {
                richTextBox1.SelectAll();
                richTextBox1.SelectionBackColor = richTextBox1.BackColor;
            }
            else
            {
                richTextBox1.SelectAll();
                richTextBox1.SelectionBackColor = richTextBox1.BackColor;
            }


            int lineIndex2 = (mips.pc / 4) - 1;
            if (lineIndex2 < richTextBox2.Lines.Length && !lastLine2)
            {
                ClearRowHighlight2(currentLineIndex2);
                int start = richTextBox2.GetFirstCharIndexFromLine(lineIndex2);
                int length = richTextBox2.Lines[lineIndex2].Length;
                richTextBox2.Select(start, length);
                richTextBox2.SelectionBackColor = Color.Aqua;
                currentLineIndex2 = lineIndex2;
                if (lineIndex2 == richTextBox2.Lines.Length - 1)
                {
                    lastLine2 = true;
                }
            }
            else if (lineIndex2 < richTextBox2.Lines.Length && lastLine2)
            {
                richTextBox2.SelectAll();
                richTextBox2.SelectionBackColor = richTextBox2.BackColor;
            }
            else
            {
                richTextBox2.SelectAll();
                richTextBox2.SelectionBackColor = richTextBox2.BackColor;
            }
        }

        private void ClearRowHighlight(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < richTextBox1.Lines.Length)
            {
                richTextBox1.Select(richTextBox1.GetFirstCharIndexFromLine(rowIndex), richTextBox1.Lines[rowIndex].Length);
                richTextBox1.SelectionBackColor = richTextBox1.BackColor;
            }
        }

        private void ClearRowHighlight2(int rowIndex)
        {
            if (rowIndex >= 0 && rowIndex < richTextBox2.Lines.Length)
            {
                richTextBox2.Select(richTextBox2.GetFirstCharIndexFromLine(rowIndex), richTextBox2.Lines[rowIndex].Length);
                richTextBox2.SelectionBackColor = richTextBox2.BackColor;
            }
        }

        private void UpdateInstructionMemory(List<string> hexMachineCode, List<string> assemblyCode)
        {
            dataGridView2.Rows.Clear();

            for (int i = 0; i < hexMachineCode.Count; i++)
            {
                string address = "0x" + (i * 4).ToString("X8");
                string machineCode = hexMachineCode[i];
                string source = assemblyCode[i];

                dataGridView2.Rows.Add(address, "0x" + machineCode, source);
            }
        }
        private void InitializeIMTable()
        {
            dataGridView2.Rows.Clear();

            for (int i = 0; i < 1024; i++)
            {
                dataGridView2.Rows.Add(new DataGridViewRow());
                int address = i * 4;
                dataGridView2.Rows[i].Cells[0].Value = "0x" + address.ToString("X8");
                dataGridView2.Rows[i].Cells[1].Value = "0x00000000";
                dataGridView2.Rows[i].Cells[2].Value = "";
            }
        }
        private void InitializeDMTable()
        {
            dataGridView3.Rows.Clear();

            for (int i = 0; i < 1024; i++)
            {
                DataGridViewRow row = new DataGridViewRow();
                dataGridView3.Rows.Add(row);

                int address = i * 4;
                dataGridView3.Rows[i].Cells[0].Value = "0x" + address.ToString("X8");
                dataGridView3.Rows[i].Cells[1].Value = "0x00000000";
            }
        }

        private void UpdateDataMemoryTable(string[] dataMemory)
        {
            for (int i = 0; i < dataMemory.Length; i++)
            {
                string value = dataMemory[i];
                dataGridView3.Rows[i].Cells[1].Value = value;
            }
        }

        private void InitializeRegisterTable()
        {
            dataGridView1.Rows.Clear();

            List<(string Name, string Number)> registerInfo = new List<(string, string)>
            {
                ("$zero", "00000"), ("$at", "00001"), ("$v0", "00010"), ("$v1", "00011"),
             ("$a0", "00100"), ("$a1", "00101"), ("$a2", "00110"), ("$a3", "00111"),
             ("$t0", "01000"), ("$t1", "01001"), ("$t2", "01010"),
             ("$t3", "01011"), ("$t4", "01100"), ("$t5", "01101"), ("$t6", "01110"),
             ("$t7", "01111"), ("$s0", "10000"), ("$s1", "10001"), ("$s2", "10010"),
             ("$s3", "10011"), ("$s4", "10100"), ("$s5", "10101"), ("$s6", "10110"),
             ("$s7", "10111"), ("$t8", "11000"), ("$t9", "11001"), ("$k0", "11010"),
             ("$k1", "11011"), ("$gp", "11100"), ("$sp", "11101"), ("$fp", "11110"),
             ("$ra", "11111"), ("$pc", "pc"), ("$hi", "hi"), ("$lo", "lo")
            };

            foreach (var reg in registerInfo)
            {
                dataGridView1.Rows.Add(reg.Name, reg.Number, "0x00000000", 0);
            }
        }

        private void UpdateRegisterTable()
        {
            string[] registerValues = mips.RegToHex();
            string pcValue = mips.PCToHex();
            string hiValue = mips.HiToHex();
            string loValue = mips.LoToHex();
            registerValues = registerValues.Concat(new string[] { pcValue, hiValue, loValue }).ToArray();
            for (int i = 0; i < registerValues.Length; i++)
            {
                string value = registerValues[i];
                int decimalValue = Convert.ToInt32(value, 16);

                dataGridView1.Rows[i].Cells[2].Value = value;
                dataGridView1.Rows[i].Cells[3].Value = decimalValue;
            }
        }

        private void ClearChangedCellHighlights()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.Cells[2].Style.BackColor = dataGridView1.DefaultCellStyle.BackColor;
                row.Cells[2].Tag = row.Cells[2].Value;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
            dataGridView1.Rows.Clear();
            dataGridView2.Rows.Clear();
            dataGridView3.Rows.Clear();
            currentRowIndex = -1;
            currentLineIndex = 0;
            mips.Reset();
            mips.ClearTrace();
            executionTraceBox.Clear();
            InitializeRegisterTable();
            InitializeIMTable();
            InitializeDMTable();
            ClearChangedCellHighlights();
            richTextBox2.Text = "";
            currentLineIndex1 = -1;
            currentLineIndex2 = -1;
            lastLine1 = false;
            lastLine2 = false;
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e) { }

    }
}