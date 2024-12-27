using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MIPS_Simulator1
{
    public class MIPS
    {
        private const int HexWidth32 = 8;
        private const int HexWidth16 = 4;
        private int[] reg;
        private int[] IM;
        private string[] IM_asm;
        private int IM_len;
        private int[] DM;
        public int pc;
        private int hi;
        private int lo;
        private string instr;
        private string instr_asm;
        private string opcode;
        private int rs;
        private int rt;
        private int rd;
        private int shamt;
        private string funct;
        private int imm;
        private int target;
        private int stepCount;
        private StringBuilder executionTrace;

        public MIPS()
        {
            reg = new int[32];
            Array.Fill(reg, 0);
            IM = new int[1024];
            IM_asm = new string[1024];
            IM_len = 0;
            DM = new int[1024];
            Array.Fill(DM, 0);
            pc = 0;
            hi = 0;
            lo = 0;
            instr = string.Empty;
            instr_asm = string.Empty;
            opcode = string.Empty;
            rs = 0;
            rt = 0;
            rd = 0;
            shamt = 0;
            funct = string.Empty;
            imm = 0;
            target = 0;
            stepCount = 0;
            executionTrace = new StringBuilder();
        }

        public void SetIM(string[] assemblyCode, int[] binMachineCode)
        {
            IM_len = binMachineCode.Length;
            Array.Copy(assemblyCode, IM_asm, assemblyCode.Length);
            Array.Copy(binMachineCode, IM, binMachineCode.Length);
        }

        public void Fetch()
        {
            if (pc / 4 >= IM.Length)
            {
                return;
            }
            instr = Convert.ToString(IM[pc / 4], 2).PadLeft(32, '0');
            instr_asm = IM_asm[pc / 4];
        }

        public void Step()
        {
            // MessageBox.Show($"target {target}");
            Fetch();
            if (string.IsNullOrEmpty(instr) || string.IsNullOrEmpty(instr_asm))
            {
                return;
            }
            var parts = InstructionParser.ParseInstruction(instr_asm);
            if (parts != null)
            {
                if (parts.Category == "Label" && parts.Opcode == "exit")
                {
                    shouldContinue = false;
                    return;
                }
                ParseMachineCode(parts);
                Execute();
                if (!(parts.Category == "Jump" || parts.Category == "Branch" || parts.Category == "RJump"))
                {
                    pc += 4;
                }
            }
            else
            {
                pc += 4;
            }
            // MessageBox.Show($"target {target}");
        }

        private bool shouldContinue = true;

        public void RunUntilEnd()
        {
            while (pc < IM_len && shouldContinue)
            {
                Step();
            }
        }


        public void ParseMachineCode(Instruction parts)
        {
            opcode = instr.Substring(0, 6);  // 6-bit opcode (32-bit komut)
            rs = Convert.ToInt32(instr.Substring(6, 5), 2);  // 5-bit rs
            rt = Convert.ToInt32(instr.Substring(11, 5), 2);  // 5-bit rt
            rd = Convert.ToInt32(instr.Substring(16, 5), 2);  // 5-bit rd
            shamt = Convert.ToInt32(instr.Substring(21, 5), 2);  // 5-bit shift amount (shamt for shift operations)
            funct = instr.Substring(26, 6);  // 6-bit function field for R-type

            if (parts.Category == "Immediate" || parts.Category == "LoadStore" || parts.Category == "Branch")
            {
                if (parts.Immediate != null)
                {
                    imm = signedInt(Convert.ToInt32(parts.Immediate));
                }
            }

            // PC'nin üst 4 biti:
            int pcUpper = (int)(pc & 0xF0000000); // PC'nin üst 4 bitini tutar

            // 26 bitlik target alanı:
            int targetField = Convert.ToInt32(instr.Substring(6, 26), 2);

            // 2 bit sola kaydır:
            int shiftedTarget = targetField << 2;

            // Hedef adresi hesapla:
            target = pcUpper | shiftedTarget;

            //MessageBox.Show($"Target Değer: {target}");
        }




        public void Reset()
        {
            Array.Fill(reg, 0);
            Array.Fill(IM, 0);
            Array.Fill(IM_asm, "");
            Array.Fill(DM, 0);
            pc = 0;
            hi = 0;
            lo = 0;
            instr = string.Empty;
            instr_asm = string.Empty;
            opcode = string.Empty;
            rs = 0;
            rt = 0;
            rd = 0;
            shamt = 0;
            funct = string.Empty;
            imm = 0;
            target = 0;
            stepCount = 0;
        }

        public void Execute()
        {
            string operation = "";
            switch (opcode)
            {
                case "000000":
                    switch (funct)
                    {
                        case "100000": // Add
                            operation = "add";
                            Add();
                            break;
                        case "100010": // Sub
                            operation = "sub";
                            Sub();
                            break;
                        case "100100": // And
                            operation = "and";
                            And();
                            break;
                        case "100101": // Or
                            operation = "or";
                            Or();
                            break;
                        case "100110": // Xor
                            operation = "xor";
                            Xor();
                            break;
                        case "101010": // Slt
                            operation = "slt";
                            Slt();
                            break;
                        case "001000": // Jr
                            operation = "jr";
                            Jr();
                            break;
                        case "000000": // Sll
                            operation = "sll";
                            Sll();
                            break;
                        case "000010": // Srl
                            operation = "srl";
                            Srl();
                            break;
                        case "000011": // Sra
                            operation = "sra";
                            Sra();
                            break;
                        case "010000": // Mfhi
                            operation = "mfhi";
                            Mfhi();
                            break;
                        case "010010": // Mflo
                            operation = "mflo";
                            Mflo();
                            break;
                        case "011000": // Mult
                            operation = "mult";
                            Mult();
                            break;
                        case "011010": // Div
                            operation = "div";
                            Div();
                            break;
                        default:
                            throw new Exception($"Unsupported function code: {funct}");
                    }
                    break;

                case "000010": // J instruction
                    operation = "j";
                    J();
                    break;

                case "000011": // Jal instruction
                    operation = "jal";
                    Jal();
                    break;

                case "000100": // Beq instruction
                    operation = "beq";
                    Beq();
                    break;

                case "000101": // Bne instruction
                    operation = "bne";
                    Bne();
                    break;

                case "001000": // Addi instruction
                    operation = "addi";
                    Addi();
                    break;

                case "001010": // Slti instruction
                    operation = "slti";
                    Slti();
                    break;

                case "001100": // Andi instruction
                    operation = "andi";
                    Andi();
                    break;

                case "001101": // Ori instruction
                    operation = "ori";
                    Ori();
                    break;

                case "100011": // Lw instruction
                    operation = "lw";
                    Lw();
                    break;

                case "101011": // Sw instruction
                    operation = "sw";
                    Sw();
                    break;

                case "011100": // Muli instruction
                    operation = "muli";
                    Muli();
                    break;

                default:
                    throw new Exception($"Unsupported opcode: {opcode}");
            }
            LogExecution(operation);
        }

        public void Add()
        {
            reg[rd] = (int)((uint)reg[rs] + (uint)reg[rt]);
        }

        public void Sub()
        {
            reg[rd] = (int)((uint)reg[rs] - (uint)reg[rt]);
        }

        public void And()
        {
            reg[rd] = reg[rs] & reg[rt];
        }

        public void Or()
        {
            reg[rd] = reg[rs] | reg[rt];
        }

        public void Xor()
        {
            reg[rd] = reg[rs] ^ reg[rt];
        }

        public void Slt()
        {
            reg[rd] = reg[rs] < reg[rt] ? 1 : 0;
            Console.WriteLine($"SLT: reg[{rd}] = {reg[rd]} (reg[{rs}] = {reg[rs]} < reg[{rt}] = {reg[rt]})");
        }

        public void Jr()
        {
            pc = reg[rs];
        }

        public void Sll()
        {
            int shiftAmount = reg[rt] & 0x1F;
            reg[rd] = reg[rs] << shiftAmount;
        }

        public void Srl()
        {

            int shiftAmount = reg[rt] & 0x1F;
            reg[rd] = (int)((uint)reg[rs] >> shiftAmount);
        }

        public void Sra()
        {
            int shiftAmount = reg[rt] & 0x1F;
            reg[rd] = reg[rs] >> shiftAmount;
        }

        public void Mfhi()
        { //MessageBox.Show($"rd: {rd},hi {hi}");
            rd = 16;
            reg[rd] = hi;
            //MessageBox.Show($"rd: {rd}");
        }
        public void Mflo()
        { //MessageBox.Show($"rd: {rd},lo {lo}");
            rd = pc + 4;
            reg[rd] = lo; //MessageBox.Show($"rd: {rd}");
        }

        public void Mult()
        {
            long result = (long)reg[rs] * reg[rt];
            lo = (int)(result & 0xFFFFFFFF);
            hi = (int)(result >> 32);
        }

        public void Div()
        {
            if (reg[rt] != 0)
            {
                lo = reg[rs] / reg[rt];
                hi = reg[rs] % reg[rt];
            }
            else
            {
                throw new DivideByZeroException("Division by zero.");
            }
        }

        public void Beq()
        {
            //MessageBox.Show($"rs: {reg[rs]}, rt: {reg[rt]}, imm: {imm}, pc: {pc}");

            if (reg[rs] == reg[rt])
            {
                pc = (imm << 2);
                //MessageBox.Show($"Branch taken: new pc: {pc}");
            }
            else
            {
                pc = pc + 4;
            }
        }

        public void Bne()
        {
            if (reg[rs] != reg[rt])
            {
                pc = (imm << 2);
                //MessageBox.Show($"Branch taken: new pc: {pc}");
            }
            else
            {
                pc = pc + 4;
            }
        }

        public void Addi()
        {
            reg[rt] = reg[rs] + imm;
        }

        public void Slti()
        {
            reg[rt] = reg[rs] < imm ? 1 : 0;
        }

        public void Andi()
        {
            reg[rt] = reg[rs] & imm;
        }

        public void Ori()
        {
            reg[rt] = reg[rs] | imm;
        }


        public void Lw()
        {
            reg[rt] = DM[reg[rs] + imm];
        }

        public void Sw()
        {
            DM[reg[rs] + imm] = reg[rt];
        }

        public void Muli()
        {
            reg[rt] = reg[rs] * imm;
        }

        public void J()
        {
            pc = target + 4;
        }

        public void Jal()
        {
            // MessageBox.Show($"target {target}");
            // MessageBox.Show($"pc: {pc}");
            int raIndex = Convert.ToInt32(Registers.RegisterMap["$ra"], 2);
            reg[raIndex] = pc + 4;
            pc = target * 5;
            // MessageBox.Show($"reg: {reg[raIndex]}, pc: {pc}, ra: {raIndex}");
            // MessageBox.Show($"target {target}");
        }

        public string[] RegToHex()
        {
            List<string> hexArray = new List<string>();
            for (int i = 0; i < reg.Length; i++)
            {
                string hexString = "0x" + ToHexString(reg[i], 8);
                hexArray.Add(hexString);
            }
            return hexArray.ToArray();
        }

        public string[] DMToHex()
        {
            List<string> hexArray = new List<string>();
            for (int i = 0; i < DM.Length; i++)
            {
                string hexString = "0x" + ToHexString(DM[i], 8);
                hexArray.Add(hexString);
            }
            return hexArray.ToArray();
        }


        public string PCToHex()
        {
            return "0x" + ToHexString(pc, 8);
        }

        public string HiToHex()
        {
            return "0x" + ToHexString(hi, 8);
        }

        public string LoToHex()
        {
            return "0x" + ToHexString(lo, 8);
        }

        public int ParseInt32(string inputStr, int radix)
        {
            return signedInt(Convert.ToInt32(inputStr, radix));
        }

        private int signedInt(int unsigned)
        {
            byte[] uintBytes = BitConverter.GetBytes(unsigned);
            int signed = BitConverter.ToInt32(uintBytes, 0);
            return signed;
        }

        public string ToHexString(int num, int hexLen)
        {
            string binaryStr = ToBinString(num, hexLen * 4);
            string hexStr = Convert.ToInt32(binaryStr, 2).ToString("X");
            return hexStr.PadLeft(hexLen, '0');
        }

        public string ToBinString(int num, int binLen)
        {
            if (num == int.MinValue)
            {
                return new string('1', binLen);
            }
            string binaryStr = Math.Abs(num).ToString("B");
            binaryStr = binaryStr.PadLeft(binLen, '0');
            if (num < 0)
            {
                binaryStr = TwosComplement(binaryStr, binLen);
            }
            return binaryStr;
        }

        public string TwosComplement(string binaryStr, int length)
        {
            string paddedStr = binaryStr.PadLeft(length, '0');
            string invertedStr = new string(paddedStr.Select(bit => bit == '0' ? '1' : '0').ToArray());
            int carry = 1;
            StringBuilder result = new StringBuilder();

            for (int i = invertedStr.Length - 1; i >= 0; i--)
            {
                int sum = (invertedStr[i] - '0') + carry;
                if (sum == 2)
                {
                    result.Insert(0, '0');
                    carry = 1;
                }
                else
                {
                    result.Insert(0, sum);
                    carry = 0;
                }
            }
            return result.ToString().PadLeft(length, '0');
        }

        private void LogExecution(string operation)
        {
            string currentInstruction = instr_asm?.Trim() ?? "Unknown";
            string trace = $"PC: 0x{pc:X8} | Instruction: {currentInstruction}\n";

            // Register değişikliklerini logla
            if (rd != 0 && operation != "sw" && operation != "lw")
            {
                trace += $"Register Change: ${rd} = 0x{reg[rd]:X8}\n";
            }

            // Memory değişikliklerini logla (sw ve lw için)
            if (operation == "sw")
            {
                trace += $"Memory Write: M[0x{(reg[rs] + imm):X8}] = 0x{reg[rt]:X8}\n";
            }
            else if (operation == "lw")
            {
                trace += $"Memory Read: ${rt} = M[0x{(reg[rs] + imm):X8}] (0x{DM[reg[rs] + imm]:X8})\n";
            }

            trace += "----------------------------------------\n";
            executionTrace.Append(trace);
        }

        // Trace'i temizlemek için
        public void ClearTrace()
        {
            executionTrace.Clear();
        }

        // Trace'i almak için
        public string GetExecutionTrace()
        {
            return executionTrace.ToString();
        }
    }
}