using System;
using System.Collections.Generic;
using MIPS_Simulator1;

public static class Compiler
{
    public static List<string> CompileToHex(List<string> assemblyCode)
    {
        var machineCode = new List<string>();
        var labels = CollectLabels(assemblyCode);
        int addressCounter = 0;

        foreach (var instruction in assemblyCode)
        {
            if (!instruction.EndsWith(":"))
            {
                var compiledInstruction = CompileInstruction(instruction, labels, addressCounter);
                if (!string.IsNullOrEmpty(compiledInstruction))
                {
                    uint parsedValue = unchecked((uint)Convert.ToUInt32(compiledInstruction, 2));
                    var hexCode = parsedValue.ToString("X8");
                    machineCode.Add(hexCode);
                    addressCounter++;
                }
            }
        }
        //for (int i = 0; i < machineCode.Count; i++)
        //{
            //MessageBox.Show(Convert.ToString(machineCode[i]));
       // }
        return machineCode;
    }
    public static string machineCodeText;
    public static List<string> CompileToBin(List<string> assemblyCode)
    {
        var machineCode = new List<string>();
        var labels = CollectLabels(assemblyCode);

        int addressCounter = 0;
        foreach (var instruction in assemblyCode)
        {
            if (!instruction.EndsWith(":"))
            {
                machineCode.Add(CompileInstruction(instruction, labels, addressCounter));
                addressCounter++;
            }
        }
        
        machineCodeText = string.Join(Environment.NewLine, machineCode);
    
        return machineCode;
    }

    private static Dictionary<string, int> CollectLabels(List<string> assemblyCode)
    {
        var labels = new Dictionary<string, int>();
        int addressCounter = 0;

        foreach (var line in assemblyCode)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine.EndsWith(":"))
            {
                string label = trimmedLine.TrimEnd(':').Trim();
                if (!labels.ContainsKey(label))
                {
                    labels[label] = addressCounter;
                }
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                addressCounter++;
            }
        }
        return labels;
    }

    public static string CompileInstruction(string instruction, Dictionary<string, int> labels, int currentAddress)
    {
        var parsedInstruction = InstructionParser.ParseInstruction(instruction);

        switch (parsedInstruction.Category)
        {
            case "Register":
            case "MultDiv":
            case "RJump":
                return CompileRTypeInstruction(parsedInstruction);
            case "Immediate":
            case "LoadStore":
                return CompileITypeInstruction(parsedInstruction);
            case "Branch":
                return CompileBranchInstruction(parsedInstruction, labels, currentAddress);
            case "Jump":
                return CompileJTypeInstruction(parsedInstruction, labels, currentAddress);
            case "MoveFrom":
                return CompileMoveFromInstruction(parsedInstruction);
            case "Label":
                return null;
            default:
                throw new Exception($"Unknown instruction category: {parsedInstruction.Category}");
        }
    }

    private static string CompileBranchInstruction(Instruction parsedInstruction, Dictionary<string, int> labels, int currentAddress)
    {
        var opcodeValue = Instructions.ITypeInstructions[parsedInstruction.Opcode].Opcode;
        if (!labels.ContainsKey(parsedInstruction.TargetLabel))
        {
            throw new Exception($"Undefined label: {parsedInstruction.TargetLabel}");
        }
        int targetAddress = labels[parsedInstruction.TargetLabel];
        int offset = (targetAddress - (currentAddress + 1)) / 4; 
        var rsValue = Registers.RegisterMap[parsedInstruction.Rs];
        var rtValue = Registers.RegisterMap[parsedInstruction.Rt];
        string offsetBinary = Convert.ToString((short)offset, 2).PadLeft(16, '0'); 
        return opcodeValue + rsValue + rtValue + offsetBinary;
    }

    private static string CompileRTypeInstruction(Instruction parsedInstruction)
    {
        var opcodeValue = Instructions.RTypeInstructions[parsedInstruction.Opcode].Opcode;
        var functValue = Instructions.RTypeInstructions[parsedInstruction.Opcode].Funct;

        switch (parsedInstruction.Category)
        {
            case "Register":
                return opcodeValue +
                       Registers.RegisterMap[parsedInstruction.Rs] +
                       Registers.RegisterMap[parsedInstruction.Rt] +
                       Registers.RegisterMap[parsedInstruction.Rd] +
                       "00000" +
                       functValue;
            case "MultDiv":
                return opcodeValue +
                       Registers.RegisterMap[parsedInstruction.Rs] +
                       Registers.RegisterMap[parsedInstruction.Rt] +
                       "00000" +
                       functValue;
            case "RJump":
                return opcodeValue +
                       Registers.RegisterMap[parsedInstruction.Rs] +
                       "0000000000" +
                       functValue;
            default:
                throw new Exception($"Invalid R-Type instruction: {parsedInstruction}");
        }
    }

    private static string CompileMoveFromInstruction(Instruction parsedInstruction)
    {
        var opcodeValue = Instructions.RTypeInstructions[parsedInstruction.Opcode].Opcode;
        var functValue = Instructions.RTypeInstructions[parsedInstruction.Opcode].Funct;

        return opcodeValue + "0000000000" + Registers.RegisterMap[parsedInstruction.Rd] + functValue;

    }

    private static string CompileITypeInstruction(Instruction parsedInstruction)
    {
        var opcodeValue = Instructions.ITypeInstructions[parsedInstruction.Opcode].Opcode;
        return opcodeValue +
               Registers.RegisterMap[parsedInstruction.Rs] +
               Registers.RegisterMap[parsedInstruction.Rt] +
               ConvertImmediateToBinary(parsedInstruction.Immediate, 16);
    }

    private static string CompileJTypeInstruction(Instruction parsedInstruction, Dictionary<string, int> labels, int currentAddress)
    {
        var opcodeValue = Instructions.JTypeInstructions[parsedInstruction.Opcode].Opcode;
        if (parsedInstruction.TargetLabel != null)
        {
            if (!labels.ContainsKey(parsedInstruction.TargetLabel))
            {
                throw new Exception($"Undefined label: {parsedInstruction.TargetLabel}");
            }
            int targetAddress = labels[parsedInstruction.TargetLabel];
            return opcodeValue + Convert.ToString(targetAddress >> 2, 2).PadLeft(26, '0'); 
        }
        else
        {
            int targetAddress = int.Parse(parsedInstruction.Immediate);
            return opcodeValue + Convert.ToString(targetAddress, 2).PadLeft(26, '0');
        }
    }

    public static string ConvertImmediateToBinary(string immediate, int length)
    {
        if (immediate.StartsWith('-'))
        { //MessageBox.Show(Convert.ToString((int)Math.Pow(2, length) + int.Parse(immediate), 2).PadLeft(length, '0'));
            return Convert.ToString((int)Math.Pow(2, length) + int.Parse(immediate), 2).PadLeft(length, '0');
        }
        else if (immediate.StartsWith("0x"))
        { //MessageBox.Show(Convert.ToString(int.Parse(immediate.Substring(2), System.Globalization.NumberStyles.HexNumber), 2).PadLeft(length, '0'));
            return Convert.ToString(int.Parse(immediate.Substring(2), System.Globalization.NumberStyles.HexNumber), 2).PadLeft(length, '0');
        }
        else
        { //MessageBox.Show(Convert.ToString(int.Parse(immediate), 2).PadLeft(length, '0'));
            return Convert.ToString(int.Parse(immediate), 2).PadLeft(length, '0');
        }
    }
}
