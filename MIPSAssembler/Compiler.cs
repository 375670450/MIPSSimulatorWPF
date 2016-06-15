using System;
using System.Collections.Generic;
using System.Linq;

namespace MIPSAssembler {

	public class MIPSAssemblerException : Exception {
		public MIPSAssemblerException( ) : base(){
			
		}

		public MIPSAssemblerException(string str) : base("MIPSAssembler Exception: " + str) {
			
		}

	}

	public class Compiler {

		#region STATIC_READONLY

		public UInt32 CodeSegStart = 0x00040000;

		//public UInt32 DataSegStart = 0xFFFFFFFF;

		public readonly UInt32 StackSegEnd = 0x7FFFFFFF;

		public static readonly int register_count = 32;

		private static readonly char[ ] _delims = { ' ', ',', '(', ')','\t',';','\'',':'  };

		#endregion

		#region PRIVATE_FILEDS

		private struct ControlSignalStruct {

			#region NECESSARY_SIGNALS
			public ALUOperation ALU_Control;
			public DataToRegSrc DatatoReg;

			public bool Branch;
			public bool Jump;
			public bool RegWrite;
			public bool Jal;
			public bool RegDst;
			public bool ALUSrc_B;
			public bool ExtSign;

			public bool MemRead;
			public bool MemWrite;
			public bool MemtoReg;
			#endregion

			#region SIMULATION_NEEDS
			public bool ALU_Unsigned;
			public int DataWidth;
			public bool IsBeq;
			#endregion

			public void init( ) {
				ALU_Control = ALUOperation.ERROR;
				DatatoReg = DataToRegSrc.ALURes;
				RegDst = RegWrite = Jal = ALUSrc_B = false;
				MemRead = MemtoReg = MemWrite = false;
				ExtSign = Branch = Jump = false;

				ALU_Unsigned = false;
				IsBeq = false;
				DataWidth = 0;
			}

			public void print( ) {
				Console.WriteLine("ALU_Control = " + ALU_Control);
				Console.WriteLine("DatatoReg = " + DatatoReg);
				Console.WriteLine("Branch = " + Branch);
				Console.WriteLine("Jump = " + Jump);
				Console.WriteLine("RegWrite = " + RegWrite);
				Console.WriteLine("Jal = " + Jal);
				Console.WriteLine("RegDst = " + RegDst);
				Console.WriteLine("ALUSrc_B = " + ALUSrc_B);
				Console.WriteLine("ExtSign = " + ExtSign);
				Console.WriteLine("MemRead = " + MemRead);
				Console.WriteLine("MemWrite = " + MemWrite);
				Console.WriteLine("MemtoReg = " + MemtoReg);
				Console.WriteLine("ALU_Unsigned = " + ALU_Unsigned);
				Console.WriteLine("DataWidth = " + DataWidth);
            }

		}

		private enum ALUOperation : byte{
			AND, OR, ADD, XOR, NOR, SRL, SUB, SLT,
			SLL, MFHI, MFLO, MULT, DIV, ERROR
		}

		private enum DataToRegSrc : byte {
			ALURes, Mem, Imm, RetAddr
		}

		private enum DataByteCount : int {
			BYTE = 1,
			WORD = 2,
			DOUBLEWORD = 4
		}

		private static readonly UInt32 defStackSize = 0x1000;

		private static readonly UInt32 defDatasegSize = 0x1000;      // 2KB

		public struct DataSeg {
			public bool isSegStart;
			public UInt32 addr;
			public int size;
			public byte[ ] data ;
			public string name;

			public DataSeg(UInt32 _addr, bool _is = false) {
				addr = _addr;
				size = 0;
				name = string.Empty;
				data = new byte[defDatasegSize];
				isSegStart = _is;
            }

		}
		private Dictionary<string, UInt32> segAddr;

		private List<DataSeg> dataSegs;

		private Dictionary<string, UInt32> constants;

		private List<string> executeLines;

		private List<string> originLines;

		private DataSeg stackSeg;

		#region DEBUG_VARIABLES

		//private ControlSignalStruct ctrlSignal;.

		private Dictionary<UInt32, bool> isBreakPoint;

		private bool isReadySimulation = false;

		#endregion

		#endregion

		public bool IsReadySimulation {
			private set {
				isReadySimulation = value;
			}
			get {
				return isReadySimulation;
			}
		}

		public static char[] Delims {
			get { return _delims; }
		}

		public List<string> ExecuteLines {
			get {
				return executeLines;
			}
		}

		public List<string> OriginLines {
			get {
				return originLines;
			}
		}

		public DataSeg StackSeg {
			get {
				return stackSeg;
			}
		}

		public List<DataSeg> DataSegs {
			get {
				List<DataSeg> displaySegs = new List<DataSeg>( );

				for(int i=0; i<dataSegs.Count; i++ ) {
					if( dataSegs.ElementAt(i).isSegStart ) {
						var tmpseg = dataSegs.ElementAt(i);
                        for (int j= i+1; j<dataSegs.Count && !dataSegs.ElementAt(j).isSegStart; j++ ) {
							tmpseg.size += dataSegs.ElementAt(j).size;
							tmpseg.data = tmpseg.data.ToList( ).Concat(dataSegs.ElementAt(j).data).ToArray();
						}
						displaySegs.Add(tmpseg);
					}
				}
				return displaySegs;
			}
			private set {
				dataSegs = value;
			}
		}

		public UInt32[ ] Registers = new UInt32[32];

		public UInt32 PC {
			set; get;
		}

		public UInt32 HI { set; get; }
		public UInt32 LO { set; get; }

		public Compiler( ) {
			
		}

		public void Init( List<string> _lines ) {

			executeLines = new List<string>( );

			originLines = new List<string>( );             // Original lines displayed in the SourceCode view

			isBreakPoint = new Dictionary<UInt32, bool>( );

			constants = new Dictionary<string, UInt32>( );

			segAddr = new Dictionary<string, UInt32>( ) {
				//{"main", CodeSegStart },
			};

			dataSegs = new List<DataSeg>( );

			stackSeg = new DataSeg(StackSegEnd - defStackSize);

			//ctrlSignal.init( );
			PreCompile(_lines);

			ReInit( );

		}

		public void ReInit( ) {
			
			for ( int i = 0; i < register_count; i++ ) {
				Registers[i] = 0;
			}

			PC = CodeSegStart;
			LO = HI = 0;

			IsReadySimulation = true;

			//if ( DataSegStart == 0xFFFFFFFF )
			//	DataSegStart = 0x0;
			
			Registers[29] = StackSegEnd - 0x04;                // init $sp ( stack increase from the end of data segment )

		}

		public List<byte> ParseDataSegment( string datastr ) {

			var datalist = datastr.Split(new char[ ] { ',', ' ',';' }, StringSplitOptions.RemoveEmptyEntries).ToList( );
			var type = datalist.ElementAt(0);
			List<byte> result = new List<byte>( );

			if ( !type.Any(char.IsDigit) ) {     // if type not contains digit
				datalist = datalist.Skip(1).ToList( );
			} else {	
				type = "dd";					// default data type
			}

			if ( type == "equ" ) {
				constants.Add(datalist.ElementAt(0), Convert.ToUInt32(datalist.ElementAt(2), 16));

			} else if ( type.StartsWith("res") ) {                  // reserved data space definition
				var datawidth = type[3] == 'b' ? DataByteCount.BYTE
										: type[3] == 'w' ? DataByteCount.WORD
										: type[3] == 'd' ? DataByteCount.DOUBLEWORD: 0;
				var datasize = Convert.ToInt32(datalist.ElementAt(0)) * (int)datawidth;
				while ( datasize > 0 ) {
					datasize--;
					result.Add(0);
				}
			} else {
				foreach ( var data in datalist ) {
					if ( data.Contains('\'') ) {
						foreach ( char ch in data.Trim('\'') ) {
							result.Add(Convert.ToByte(ch));
						}
					} else {
						UInt64 tmp = data.Contains("0x") ? Convert.ToUInt64(data, 16) : Convert.ToUInt64(data, 10);
						int bytecounts = 0;
						if ( type == "db" ) {               // byte
							bytecounts = (int)DataByteCount.BYTE;
						} else if ( type == "dw" ) {        // word
							bytecounts = (int)DataByteCount.WORD;
						} else if ( type == "dd" ) {        // double word
							bytecounts = (int)DataByteCount.DOUBLEWORD;
						}
						for ( int i = 0; i < bytecounts; i++ ) {
							result.Add((byte)tmp);
							tmp >>= 8;
						}
					}
				}
			}
			

			return result;
		}

		private void PreCompile(List<string> _lines) {        // Remove comments and build segment address

			string tmpline;
			List<string> actualLines = new List<string>( );
			UInt32 addrstart = 0;
			int i = 0, j = 0;

			// Remove blank lines and white spaces
			_lines = (from l in _lines									
					  where !string.IsNullOrWhiteSpace(l)
					  select l.Trim( ).ToLower( ).Split(new char[ ] { '/' }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("\t", " ") ).ToList( );

			// Initialize data definition
			for ( i=0; i<_lines.Count; i++) {
				tmpline = _lines[i];
				
				if ( tmpline.Contains("#baseaddre") ) {
					var list = tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries);
                    CodeSegStart = Convert.ToUInt32(tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries)[1],16);
				}
				else if( tmpline.Contains("#dataaddre") ) {
					var str = tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries)[1];
                    addrstart = Convert.ToUInt32(tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries)[1],16);
      //              if ( DataSegStart == 0xFFFFFFFF )
						//DataSegStart = addrstart;


					for ( j= i+1; j < _lines.Count && !_lines[j].Contains('#');j++ ) {
						DataSeg newdataseg = new Compiler.DataSeg(addrstart, j == (i+1) );
						var tmpdata = new List<byte>();
						tmpline = _lines[j];
						//addrstart = Convert.ToUInt32(tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries)[1], 16);
						
						if ( tmpline.Contains(':') ) {
							newdataseg.name = tmpline.Substring(0, tmpline.IndexOf(':'));
							tmpline = tmpline.Substring(tmpline.IndexOf(':')+1).Trim();
						}

						while ( !tmpline.Contains(';') )
							tmpline += _lines[++j];
						foreach(byte data in ParseDataSegment(tmpline) ) {
							//newdataseg.data[newdataseg.size++] = data;
							tmpdata.Add(data);
						}
						newdataseg.size = tmpdata.Count;
						newdataseg.data = tmpdata.ToArray( );
						dataSegs.Add(newdataseg);
						addrstart += (UInt32)newdataseg.size;
					}
					i = j-1;
				} else {
					actualLines.Add(tmpline);
				}
			}

			actualLines = ( from l in actualLines
							where l.IndexOf('#') != 0
					   select l.IndexOf('#') < 0 ? l : l.Substring(0, l.IndexOf('#'))
					   ).ToList( );
			var tmpaddr = CodeSegStart;
			// ReplacePseudoCodes
			for ( i = 0; i < actualLines.Count; i++ ) {
				tmpline = actualLines[i];
				if ( tmpline.IndexOf(':') >= 0 ) {		
					string segname = tmpline.Substring(0, tmpline.IndexOf(':'));
                    segAddr.Add(segname, tmpaddr);
					tmpline = tmpline.Substring(tmpline.IndexOf(':')+1);
				}

				if (tmpline.Length == 0)
					continue;
				originLines.Add(tmpline);
				tmpaddr += 4;

				if ( tmpline.IndexOf("move") >= 0 ) {
					var param = tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries);
					executeLines.Add("add " + param[1] + ", " + param[2] + ", " + "$zero");

				} else if ( tmpline.IndexOf("li") >= 0 ) {
					var param = tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries);
					executeLines.Add("addi " + param[1] + ", $zero, " + param[2]);

				} else if ( tmpline.IndexOf("la") >= 0 ) {
					var param = tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries);
					executeLines.Add("addi " + param[1] + ", $zero, " + param[2]);

				} else if ( tmpline.IndexOf("bge") >= 0 ) {
					var param = tmpline.Split(_delims, StringSplitOptions.RemoveEmptyEntries);
					executeLines.Add("slt $at, " + param[1] + ", " + param[2]);
					executeLines.Add("beq $at, $zero, " + param[3]);
					OriginLines.Add(string.Empty);
				} else {
					executeLines.Add(tmpline);
				}
				
			}

			ReplaceSegNames( );
			
			return ;
        }

		public static string Encode(string command) {  // Encode a command to an instruction code string

			var list = command.Split(_delims, StringSplitOptions.RemoveEmptyEntries);
			string result = "";
			try {
				string opcode = Utils.GetOpcode(list[0]);
				Utils.InstType type = Utils.GetInstType(opcode);
				if (type == Utils.InstType.ALU_R_TYPE) {
					if (list[0][0] == 'j') {       // jr
						result += Utils.GetOpcode(list[0]) + Utils.RegNametoBin(list[1]);
					} else if (list[0].Length >= 3 && (list[0].Substring(0, 3) == "srl" || list[0].Substring(0, 3) == "sll")) {          // shamt != 0, rs=0
						result += Utils.GetOpcode(list[0]) + "00000" + Utils.RegNametoBin(list[2])
										+ Utils.RegNametoBin(list[1]) + Utils.DectoBin(Utils.GetImmediate(list[3]), 5);
					} else if (list[0].Length >= 3 && (list[0].Substring(0, 3) == "div" || list[0].Substring(0, 3) == "mul")) { // mul/div
						result += Utils.GetOpcode(list[0]) + Utils.RegNametoBin(list[1]) + Utils.RegNametoBin(list[2]);
					} else {                    // shamt = 0, ts!=0
						result += Utils.GetOpcode(list[0]) + Utils.RegNametoBin(list[2]) + Utils.RegNametoBin(list[3])
										+ Utils.RegNametoBin(list[1]);
					}
					while (result.Length < 26)
						result += "0";
					result += Utils.GetFunctionCode(list[0]);

				} else if (type == Utils.InstType.ALU_I_TYPE ) {

					if( list[0].Length >= 3 && list[0].Substring(0,3) == "lui" ) {
						result += Utils.GetOpcode(list[0]) + "00000" + Utils.RegNametoBin(list[1]) + Utils.DectoBin(Utils.GetImmediate(list[2]), 16);
					} else {
						result += Utils.GetOpcode(list[0]) + Utils.RegNametoBin(list[2]) + Utils.RegNametoBin(list[1]);
						result += Utils.DectoBin(Utils.GetImmediate(list[3]), 16);
					}

				} else if (type ==Utils.InstType.BRANCH_TYPE ) {
					result += Utils.GetOpcode(list[0]) + Utils.RegNametoBin(list[1]) + Utils.RegNametoBin(list[2]);
					result += Utils.DectoBin(Utils.GetImmediate(list[3]) >> 2, 16);

				} else if (type == Utils.InstType.J_TYPE) {      // j/jal, Reconstruct address
					result += Utils.GetOpcode(list[0]) + Utils.DectoBin(Utils.GetImmediate(list[1]) >> 2, 26);

				} else if (type == Utils.InstType.LOAD_TYPE || type == Utils.InstType.STORE_TYPE) {
					result += Utils.GetOpcode(list[0]) + Utils.RegNametoBin(list[3]) + Utils.RegNametoBin(list[1])
									+ Utils.DectoBin(Utils.GetImmediate(list[2]), 16);

				}

			}
			catch ( Exception ) {
				// Interrupt
				return Utils.GetOpcode("nop");
			}
			return result;

		}

		#region SIMULATION_WORKS

		
		private void cpu_control(string inst31_26, string inst5_0, bool zero, out ControlSignalStruct ctrlsig) {
			ctrlsig = new ControlSignalStruct( );
			ctrlsig.init( );

			// Decode inst(31:26) - opcode

			switch ( inst31_26 ) {
				case "000000":      // R-Type
					ctrlsig.RegWrite = true;
					ctrlsig.RegDst = true;
					break;
				case "001000":      // addi
					ctrlsig.RegWrite = true;
					ctrlsig.ExtSign = true;
					ctrlsig.ALUSrc_B = true;    // imm32
					ctrlsig.ALU_Control = ALUOperation.ADD;
					break;
				case "001001":      // addiu
					ctrlsig.ALU_Unsigned = true;
					ctrlsig.RegWrite = true;
					ctrlsig.ALUSrc_B = true;    // imm32
					ctrlsig.ALU_Control = ALUOperation.ADD;
					break;
				case "001100":      // andi
					ctrlsig.RegWrite = true;
					ctrlsig.ALUSrc_B = true;    // imm32
					ctrlsig.ALU_Control = ALUOperation.AND;
					break;
				case "001101":      // ori
					ctrlsig.RegWrite = true;
					ctrlsig.ALUSrc_B = true;    // imm32
					ctrlsig.ALU_Control = ALUOperation.OR;
					break;
				case "001110":      // xori
					ctrlsig.RegWrite = true;
					ctrlsig.ALUSrc_B = true;    // imm32
					ctrlsig.ALU_Control = ALUOperation.XOR;
					break;
				case "000100":      // beq
					ctrlsig.IsBeq = true;
					ctrlsig.ALU_Control = ALUOperation.SUB;
					ctrlsig.Branch = true;
					break;
				case "000101":      // bne
					
					ctrlsig.ALU_Control = ALUOperation.SUB;
					ctrlsig.Branch = true;
					break;
				case "000010":      // j
					ctrlsig.Jump = true;
					ctrlsig.Branch = true;
					break;
				case "000011":      // jal
									// J-Type
					ctrlsig.RegWrite = true;
					ctrlsig.Jump = true;
					ctrlsig.Jal = true;
					ctrlsig.Branch = true;
					ctrlsig.DatatoReg = DataToRegSrc.RetAddr;
					break;
				case "001111":      // lui
					ctrlsig.DatatoReg = DataToRegSrc.Imm;
					ctrlsig.RegWrite = true;
					break;
				case "100011":      // lw
									// Load
					ctrlsig.ALUSrc_B = true;
					ctrlsig.MemtoReg = true;
					ctrlsig.MemRead = true;
					ctrlsig.RegWrite = true;
					ctrlsig.DataWidth = 32;
					ctrlsig.ALU_Control = ALUOperation.ADD;
					break;
				case "101000":      // sb
					ctrlsig.MemWrite = true;
					ctrlsig.ALUSrc_B = true;
					ctrlsig.DataWidth = 8;
					ctrlsig.ALU_Control = ALUOperation.ADD;
					break;
				case "101001":
					ctrlsig.MemWrite = true;
					ctrlsig.ALUSrc_B = true;
					ctrlsig.DataWidth = 16;
					ctrlsig.ALU_Control = ALUOperation.ADD;
					break;
                case "101011":      // sw
									// Save
					ctrlsig.MemWrite = true;
					ctrlsig.ALUSrc_B = true;
					ctrlsig.DataWidth = 32;
					ctrlsig.ALU_Control = ALUOperation.ADD;
					break;
				case "001010":      // slti
					ctrlsig.ALU_Control = ALUOperation.SLT;
					ctrlsig.RegWrite = true;
					ctrlsig.ALUSrc_B = true;
					break;
				case "001011":       // sltiu
					ctrlsig.ALU_Control = ALUOperation.SLT;
					ctrlsig.RegWrite = true;
					ctrlsig.ALUSrc_B = true;
					ctrlsig.ALU_Unsigned = true;
					break;
				default:
					throw new MIPSAssemblerException("CPU Control (Opcode) Error");
			}

			// Decode inst(5:0) - function code
			if ( inst31_26 == "000000" ) {
			
				switch ( inst5_0 ) {
					case "100001":  // addu
						ctrlsig.ALU_Unsigned = true;
						ctrlsig.ALU_Control = ALUOperation.ADD;
						break;
					case "100000":
						ctrlsig.ALU_Control = ALUOperation.ADD;
						break;
					case "100100":
						ctrlsig.ALU_Control = ALUOperation.AND;
						break;
					case "100111":
						ctrlsig.ALU_Control = ALUOperation.NOR;
						break;
					case "100101":
						ctrlsig.ALU_Control = ALUOperation.OR;
						break;
					case "101010":
						ctrlsig.ALU_Control = ALUOperation.SLT;
						break;
					case "101011":  // sltu
						ctrlsig.ALU_Unsigned = true;
						ctrlsig.ALU_Control = ALUOperation.SLT;
						break;
					case "100010":
						ctrlsig.ALU_Control = ALUOperation.SUB;
						break;
					case "100011":
						ctrlsig.ALU_Unsigned = true;
						ctrlsig.ALU_Control = ALUOperation.SUB;
						break;
					case "100110":
						ctrlsig.ALU_Control = ALUOperation.XOR;
						break;
					case "001000":      // jr
						ctrlsig.ALU_Control = ALUOperation.ERROR;
						ctrlsig.Jump = true;
						break;
					case "000000":      // sll
						ctrlsig.ALU_Control = ALUOperation.SLL;
						ctrlsig.ALUSrc_B = true;
						break;
					case "000010":      // srl
						ctrlsig.ALU_Control = ALUOperation.SRL;
						ctrlsig.ALUSrc_B = true;
						break;
					default:
						throw new MIPSAssemblerException("CPU Control (Function Code) Error");
				}
			}
			return;
		}

		private void data_path(string inst, ControlSignalStruct ctrlsignal, 
												out string pc_out, out bool zero ) {
			pc_out = string.Empty;
			Console.WriteLine(Decompiler.Decode(inst));
			//ctrlsignal.print( );
			string res = string.Empty;
			var PC_4 = PC + 4;
			var jumpaddr = (Convert.ToString(PC_4, 2).PadLeft(32,'0').Substring(0, 4) + 
										inst.Substring(Decompiler._pos_jumpaddr.Key, Decompiler._pos_jumpaddr.Value)).PadRight(32,'0');
			var reg1 = ( ctrlsignal.ALU_Control == ALUOperation.SLL || ctrlsignal.ALU_Control == ALUOperation.SRL ? 
								  inst.Substring(Decompiler._pos_secondreg.Key, Decompiler._pos_secondreg.Value) 
								: inst.Substring(Decompiler._pos_firstdreg.Key, Decompiler._pos_firstdreg.Value) );
			var reg2 = inst.Substring(Decompiler._pos_secondreg.Key, Decompiler._pos_secondreg.Value);
			var read_data1 = Convert.ToString(Registers[Convert.ToInt32(reg1, 2)], 2).PadLeft(32,'0');
			var read_data2 = Convert.ToString(Registers[Convert.ToInt32(reg2, 2)], 2).PadLeft(32,'0');
			var imm32 = inst.Substring(Decompiler._pos_immediate.Key, Decompiler._pos_immediate.Value)
										.PadLeft(32,inst.ElementAt(Decompiler._pos_immediate.Key));     // Ext_32
			var branch_offset = imm32.Substring(0, 30).PadRight(32, '0');
			var write_reg = ctrlsignal.RegDst ? inst.Substring(Decompiler._pos_thirdreg.Key, Decompiler._pos_thirdreg.Value)
										: ctrlsignal.Jal ? "11111" : inst.Substring(Decompiler._pos_secondreg.Key, Decompiler._pos_secondreg.Value);
			var write_data = string.Empty;
			var mem_data = string.Empty;

			alu(	read_data1,
					ctrlsignal.ALUSrc_B ? imm32 : read_data2,
					ctrlsignal.ALU_Control, ctrlsignal.ALU_Unsigned, 
					out zero, out res);
			dataMemory(res,
								read_data2,
								ctrlsignal.MemWrite, ctrlsignal.MemRead, ctrlsignal.DataWidth,
								out mem_data);



			if ( ctrlsignal.RegWrite ) {			// register file

				switch ( ctrlsignal.DatatoReg ) {
					case DataToRegSrc.ALURes: write_data = res; break;
					case DataToRegSrc.Mem: write_data = mem_data; break;
					case DataToRegSrc.Imm:              // lui
						write_data = inst.Substring(Decompiler._pos_immediate.Key, Decompiler._pos_immediate.Value).PadRight(32, '0');
						break;
					case DataToRegSrc.RetAddr: write_data = Convert.ToString( PC + 4 , 2).PadLeft(32,'0'); break;      // jr-ret, write_addr = $ra
				}

				Registers[Convert.ToInt32(write_reg, 2)] = Convert.ToUInt32(write_data, 2);
				Console.WriteLine("Register $" + Utils.BintoRegName(write_reg) + " = " + Convert.ToString(Convert.ToUInt32(write_data, 2),16));
			}

			UInt32 tempPC = 0;
			switch ( (Convert.ToInt32(ctrlsignal.Jump) << 1 )+ Convert.ToInt32(ctrlsignal.Branch) ) {
				case 0: tempPC = PC_4; break;
				case 1: tempPC = PC_4 + ( (ctrlsignal.IsBeq && zero) || (!ctrlsignal.IsBeq && !zero) ? Convert.ToUInt32(branch_offset, 2) : 0 ) ; break;
				case 2: tempPC = Convert.ToUInt32(read_data1, 2); break;			// jr	
				case 3: tempPC = Convert.ToUInt32(jumpaddr, 2); break;
			}

			pc_out = Convert.ToString(tempPC, 2).PadLeft(32,'0');
			Console.WriteLine("PC_Out = 0x" + Convert.ToString(Convert.ToUInt32(pc_out, 2), 16));
			return;

		}
		

		private void alu(string _a, string _b, ALUOperation alu_control, bool isUnsigned,
									out bool zero, out string _result ) {
			UInt32 a = Convert.ToUInt32(_a, 2), b = Convert.ToUInt32(_b, 2), result = 0;
			switch ( alu_control ) {
				case ALUOperation.AND:
					result = a & b;
					break;
				case ALUOperation.OR:
					result = a | b;
					break;
				case ALUOperation.ADD:
					result = a + b;
					if ( !isUnsigned && (int)a > 0 && (int)b > 0 && ((int)result < (int)a || (int)result < (int)b)  )	// only signed add may overflow
						throw new MIPSAssemblerException("ALU Addition Overflow");
					break;
				case ALUOperation.XOR:
					result = a ^ b;
					break;
				case ALUOperation.NOR:
					result = ~( a | b );
					break;
				case ALUOperation.SRL:
					result = a >> Convert.ToByte(_b.Substring(21, 5), 2);
					break;
				case ALUOperation.SLL:
					result = a << Convert.ToByte(_b.Substring(21, 5), 2);
					break;
				case ALUOperation.SUB:
					result = a - b;
					if( result > a && isUnsigned)			// only unsigned sub may overflow
						throw new MIPSAssemblerException("ALU Addition Overflow");
					break;
				case ALUOperation.SLT:
					result = (UInt32)( a < b ? 1 : 0 );
					break;
				default:
					//throw new MIPSAssemblerException("ALU Control error");
					result = 0;
					break;
			}
			_result = Convert.ToString(result, 2).PadLeft(32,'0');
			zero = ( result == 0 );
			return;	
		}

		private void dataMemory(string _addr, string write_data, bool MemWrite, bool MemRead, int datawidth,	// datawidth = 8/16/32
													out string read_data) {
			read_data = string.Empty;
			if ( !MemRead && !MemRead )
				return;
			UInt32 addr = Convert.ToUInt32(_addr, 2);
			DataSeg targetSeg = new DataSeg(0xFFFFFFFF);
			foreach ( var dataseg in dataSegs ) {
				if ( addr >= dataseg.addr && addr < ( dataseg.addr + dataseg.size ) ) {
					targetSeg = dataseg;
				}
			}
			if ( addr >= stackSeg.addr && addr < stackSeg.addr + defStackSize )
				targetSeg = stackSeg;

			if( targetSeg.addr == 0xFFFFFFFF ) {
				throw new Exception("Data Segment Not Found");
			}

			if ( MemRead ) {
				for ( int i = 0; i < datawidth / 8; i++ ) {
					read_data += Convert.ToString(targetSeg.data[(int)( addr - targetSeg.addr + i )], 2);
				}
				Console.WriteLine("read_data at 0x" + Convert.ToString(addr, 16) + " = " + read_data);
			}	
			if( MemWrite ) {

				for ( int i = 0; i < datawidth / 8; i++ ) {
					targetSeg.data[(int)( addr - targetSeg.addr + i )] = Convert.ToByte(write_data.Substring(i * 8, 8), 2);
				}
				Console.WriteLine("Data at 0x" + Convert.ToString(addr, 16) + " = " + write_data);

			}
			return;
		}



		public void SingleStep( ) {
			ControlSignalStruct ctrlSignal = new ControlSignalStruct();

			try {

				int lineNum = (int)( PC - CodeSegStart ) / 4;

				if ( lineNum >= executeLines.Count ) {
					IsReadySimulation = false;
					return;
				}

				string inst = Compiler.Encode(executeLines[lineNum]);              // data_in => data in memory

				string pc_out;

				bool zero = false;

				Console.Write("PC = 0x" + Convert.ToString(PC, 16) + " : ");

				cpu_control(inst.Substring(0, 6), inst.Substring(26, 6), zero, out ctrlSignal);
				data_path(inst, ctrlSignal, out pc_out, out zero);

				PC = Convert.ToUInt32(pc_out, 2);

				Console.WriteLine("=======================");

			}catch( Exception except ) {

				Console.WriteLine("Single Step Error :\n" + except.Message);

				throw except;

			}


			return ;
		}


		#endregion

		#region PRIVATE_METHODS
		private void ReplaceSegNames( ) {
			var replace = new Func<Dictionary<string, UInt32>, int, bool>((dic, i) => {
				foreach ( var k in dic ) {
					// Specially calculate the offset for branch inst
					if( executeLines[i].Split(_delims).Contains(k.Key) )
					if ( executeLines[i][0] == 'b' ) {
						if( k.Value - ( CodeSegStart + ( i + 1 ) * 4 ) > 0 )
							executeLines[i] = executeLines[i].Replace(k.Key, "0x" + Convert.ToString(( k.Value - ( CodeSegStart + ( i + 1 ) * 4 ) ) & 0x3ffff, 16));
						else
							executeLines[i] = executeLines[i].Replace(k.Key, string.Format("-{0}", ( ( CodeSegStart + ( i + 1 ) * 4 ) - k.Value )));
					}
					executeLines[i] = executeLines[i].Replace(k.Key, "0x" + Convert.ToString(k.Value, 16));
				};
				return true;
			});

			for (int i=0 ; i<executeLines.Count ; i++ ) {
				replace(segAddr, i);
				replace(constants, i);
				foreach(var dataseg in dataSegs ) {
					if( executeLines[i].Split(_delims).Contains(dataseg.name) ) {
						executeLines[i] = executeLines[i].Replace(dataseg.name, "0x" + Convert.ToString(dataseg.addr, 16));
					}
				}
			}
		}
		#endregion

	}
}
