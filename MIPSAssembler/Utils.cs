using System;
using System.Collections.Generic;
using System.Linq;

namespace MIPSAssembler {
	public static class Utils {

		public static List<string> Operations {
			get { return _operations; }
		}

		public static List<string> Registers {
			get { return _NumtoRegName; }
		}

		public enum InstType { ALU_R_TYPE, ALU_I_TYPE, J_TYPE, LOAD_TYPE, STORE_TYPE, BRANCH_TYPE, UNKNOWN_TYPE };

		public static InstType GetInstType(string opcode) {
			if (!Char.IsDigit(opcode[0]))
				opcode = GetOpcode(opcode);
			
			int op = Convert.ToInt32(opcode, 2);
			if ( op == 0 ) {
				return InstType.ALU_R_TYPE;
			} else if ( op >= 0x20 && op <= 0x25 && op != 0x22 ) {
				return InstType.LOAD_TYPE;
			} else if ( op == 0x28 || op == 0x29 || op == 0x2b ) {
				return InstType.STORE_TYPE;
			} else if( op >= 0x08 && op <= 0x0F ) {
				return InstType.ALU_I_TYPE;
			}else if ( op >= 0x04 && op <= 0x07 ) {
				return InstType.BRANCH_TYPE;
			}else if( op == 0x02 || op == 0x03 ) {
				return InstType.J_TYPE;
			}
			return InstType.UNKNOWN_TYPE;
		}

		

		public static string NumtoHexStr(UInt32 num, int bits) {
			
			string result = Convert.ToString(num, 16);
			while( result.Length < bits ) {
				result = "0" + result;
			}
			return result;
		}

		public static int GetImmediate(string str) {
			if( str.Length >= 3 && str.ToLower().IndexOf("0x")>=0 ) {
					return Convert.ToInt32(str, 16);
			} else {
				return Convert.ToInt32(str);
			}
		}

		public static string DectoBin(int dec, int bits) {
			string result = "";
			try {
				result = Convert.ToString((UInt32)dec, 2);
				if( result.Length > bits ) {
					result = result.Substring(result.Length - bits);
				}
			}
			catch (OverflowException ) {
				// Ignore Overflow
			}
			
			while ( result.Length < bits )
				result = "0" + result;
			return result;
		}

		public static string BintoRegName(string inst_code) {
			return NumtoRegName(Convert.ToInt32(inst_code, 2));
		}

		public static string NumtoRegName(int num) {
			return _NumtoRegName[num];
		}

		public static string GetOpcode(string str) {
			return _Opcode[str.ToLower( )];
		}

		public static string GetFunctionCode(string str) {
			return _FunctionCode[str.ToLower( )];
		}

		public static string Opcode2Inst(string opcode) {
			foreach(var k in _Opcode) {
				if (k.Value == opcode)
					return k.Key;
			}
			return "nop";
		}

		public static string Func2Inst(string funccode) {
			foreach(var k in _FunctionCode) {
				if (k.Value == funccode)
					return k.Key;
			}
			return "nop";
		}

		public static int RegNametoNum(string str) {
			return _RegNametoNum[_NormalizeRegName(str)];
		}

		public static string RegNametoBin(string str) {
			return _RegNametoBin[ _NormalizeRegName(str) ];
		}

		private static string _NormalizeRegName(string str) {
			str = str.ToLower( );
			if ( str[0] == '$' )
				str = str.Substring(1);
			if ( str[0] >= '0' && str[0] <= '9' )
				str = Utils.NumtoRegName(Convert.ToInt32(str));
			return str;
		}


		#region PRIVATE_READONLY_DICT

		private static readonly List<string> _NumtoRegName = new List<string>( ) {
			"zero", "at", "v0", "v1", "a0", "a1", "a2", "a3",
			"t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
			"s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
			"t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra"
		};

		private static readonly Dictionary<string, string> _Opcode = new Dictionary<string, string>( ) {
			{"add" , "000000"},
			{"addi" , "001000"},
			{"addiu" , "001001"},
			{"addu" , "000000"},
			{"and" , "000000"},
			{"andi" , "001100"},
			{"beq" , "000100"},
			{"bne" , "000101"},
			{"div" , "000000"},
			{"divu" , "000000"},
			{"j" , "000010"},
			{"jal" , "000011"},
			{"jr" , "000000"},
			//{"lbu" , "100100"},
			//{"lhu" , "100101"},
			{"lui" , "001111"},
			{"lw" , "100011"},
			{"mfhi" , "000000"},
			{"mflo" , "000000"},
			//{"mfc0" , "010000"},
			//{"mult" , "000000"},
			//{"multu" , "000000"},
			{"nor" , "000000"},
			{"xor" , "000000"},
			{"xori" , "001110"},
			{"or" , "000000"},
			{"ori" , "001101"},
			{"sb" , "101000"},
			{"sh" , "101001"},
			{"slt" , "000000"},
			{"slti" , "001010"},
			{"sltiu" , "001011"},
			{"sltu" , "000000"},
			{"sll" , "000000"},
			{"srl" , "000000"},
			{"sra" , "000000"},
			{"sub" , "000000"},
			{"subu" , "000000"},
			{"sw" , "101011"},
			{"nop", "00000000000000000000000000000000" },
		};

		private static readonly Dictionary<string, string> _FunctionCode = new Dictionary<string, string>( ) {
			{"add" , "100000"},
			{"addu" , "100001"},
			{"and" , "100100"},
			{"div" , "011010"},
			{"divu" , "011011"},
			{"jr" , "001000"},
			{"mfhi" , "010000"},
			{"mflo" , "010010"},
			{"mult" , "011000"},
			{"multu" , "011001"},
			{"nor" , "100111"},
			{"xor" , "100110"},
			{"or" , "100101"},
			{"slt" , "101010"},
			{"sltu" , "101011"},
			{"sll" , "000000"},
			{"srl" , "000010"},
			{"sra" , "000011"},
			{"sub" , "100010"},
			{"subu" , "100011"},
			{"syscall", "001100" }
		};

		private static readonly List<string> _operations = _Opcode.Keys.ToList().Union( _FunctionCode.Keys.ToList()).ToList();

		#endregion


		#region REGNAME_TRANSLATE
		private static readonly Dictionary<string, int> _RegNametoNum = new Dictionary<string, int>( ) {
			{"zero",0 },
			{"r0", 0},
			{"at", 1},
			{"v0", 2},
			{"v1", 3},
			{"a0", 4},
			{"a1", 5},
			{"a2", 6},
			{"a3", 7},
			{"t0", 8},
			{"t1", 9},
			{"t2", 10},
			{"t3", 11},
			{"t4", 12},
			{"t5", 13},
			{"t6", 14},
			{"t7", 15},
			{"s0", 16},
			{"s1", 17},
			{"s2", 18},
			{"s3", 19},
			{"s4", 20},
			{"s5", 21},
			{"s6", 22},
			{"s7", 23},
			{"t8", 24},
			{"t9", 25},
			{"k0", 26},
			{"k1", 27},
			{"gp", 28},
			{"sp", 29},
			{"fp", 30},
			{"ra", 31},
			
		};

		private static readonly Dictionary<string, string> _RegNametoBin = new Dictionary<string, string>( ) {
			{"zero","00000" },
			{"r0", "00000"},
			{"at", "00001"},
			{"v0", "00010"},
			{"v1", "00011"},
			{"a0", "00100"},
			{"a1", "00101"},
			{"a2", "00110"},
			{"a3", "00111"},
			{"t0", "01000"},
			{"t1", "01001"},
			{"t2", "01010"},
			{"t3", "01011"},
			{"t4", "01100"},
			{"t5", "01101"},
			{"t6", "01110"},
			{"t7", "01111"},
			{"s0", "10000"},
			{"s1", "10001"},
			{"s2", "10010"},
			{"s3", "10011"},
			{"s4", "10100"},
			{"s5", "10101"},
			{"s6", "10110"},
			{"s7", "10111"},
			{"t8", "11000"},
			{"t9", "11001"},
			{"k0", "11010"},
			{"k1", "11011"},
			{"gp", "11100"},
			{"sp", "11101"},
			{"fp", "11110"},
			{"ra", "11111"},
			
		};

		#endregion

	}
}
