using System;
using System.Collections.Generic;
using System.Linq;

namespace MIPSAssembler {
	public class Decompiler {                               // Translate inst_code into assembly

		#region PRIVATE_READONLY
		public static readonly KeyValuePair<int, int> _pos_opcode = new KeyValuePair<int, int>(0,6) ;
		public static readonly KeyValuePair<int, int> _pos_firstdreg = new KeyValuePair<int, int>(6, 5);
		public static readonly KeyValuePair<int, int> _pos_secondreg = new KeyValuePair<int, int>(11, 5);
		public static readonly KeyValuePair<int, int> _pos_thirdreg = new KeyValuePair<int, int>(16, 5);
		public static readonly KeyValuePair<int, int> _pos_funccode = new KeyValuePair<int, int>(26, 6);
		public static readonly KeyValuePair<int, int> _pos_immediate = new KeyValuePair<int, int>(16, 16);
		public static readonly KeyValuePair<int, int> _pos_jumpaddr = new KeyValuePair<int, int>(6, 26);
		public static readonly KeyValuePair<int, int> _pos_shift_val = new KeyValuePair<int, int>(21, 5);
		#endregion

		public static string Decode(string inst) {

			if( inst.Length * 4 == 32 || inst.ToUpper().StartsWith("0X") ) {       // hex
				inst = Utils.DectoBin(Convert.ToInt32(inst, 16), 32);
			}

			string result = "", opcode = inst.Substring(0, 6);
			var type = Utils.GetInstType(opcode);
			try {
				if ( type == Utils.InstType.ALU_R_TYPE && inst.Contains('1') ) {
					result += Utils.Func2Inst(inst.Substring(_pos_funccode.Key, _pos_funccode.Value)) + " ";
					if ( result[0] == 'j' ) { // jr
						result += "$" + Utils.BintoRegName(inst.Substring(_pos_firstdreg.Key, _pos_firstdreg.Value));

					} else if ( result.Length >= 3 && ( result.Substring(0, 3) == "srl" || result.Substring(0, 3) == "sll" ) ) {
						result += string.Format("${0}, ${1}, 0x{2:x}",
											Utils.BintoRegName(inst.Substring(_pos_thirdreg.Key, _pos_thirdreg.Value)),
											Utils.BintoRegName(inst.Substring(_pos_secondreg.Key, _pos_secondreg.Value)),
											Convert.ToUInt16(inst.Substring(_pos_shift_val.Key, _pos_shift_val.Value), 2));

					} else if ( result.Length >= 3 && ( result.Substring(0, 3) == "div" || result.Substring(0, 3) == "mul" ) ) {
						result += string.Format("${0}, ${1}",
											Utils.BintoRegName(inst.Substring(_pos_firstdreg.Key, _pos_firstdreg.Value)),
											Utils.BintoRegName(inst.Substring(_pos_secondreg.Key, _pos_secondreg.Value)));

					} else {
						result += string.Format("${0}, ${1}, ${2}",
											Utils.BintoRegName(inst.Substring(_pos_thirdreg.Key, _pos_thirdreg.Value)),
											Utils.BintoRegName(inst.Substring(_pos_firstdreg.Key, _pos_firstdreg.Value)),
											Utils.BintoRegName(inst.Substring(_pos_secondreg.Key, _pos_secondreg.Value)));
					}

				} else {
					result += Utils.Opcode2Inst(opcode) + " ";
					switch ( type ) {
						case Utils.InstType.ALU_I_TYPE:
							result += string.Format("${0}, ${1}, {2}",
											Utils.BintoRegName(inst.Substring(_pos_secondreg.Key, _pos_secondreg.Value)),
											Utils.BintoRegName(inst.Substring(_pos_firstdreg.Key, _pos_firstdreg.Value)),
											Convert.ToInt16(inst.Substring(_pos_immediate.Key, _pos_immediate.Value), 2));
							break;
						case Utils.InstType.BRANCH_TYPE:
							result += string.Format("${0}, ${1}, 0x{2:x}",
											Utils.BintoRegName(inst.Substring(_pos_firstdreg.Key, _pos_firstdreg.Value)),
											Utils.BintoRegName(inst.Substring(_pos_secondreg.Key, _pos_secondreg.Value)),
											Convert.ToInt16(inst.Substring(_pos_immediate.Key, _pos_immediate.Value))<<2);
							break;
						case Utils.InstType.J_TYPE:
							result += string.Format("0x{0:x}",Convert.ToInt32(inst.Substring(_pos_jumpaddr.Key, _pos_jumpaddr.Value), 2)<<2);
							break;
						case Utils.InstType.LOAD_TYPE:
						case Utils.InstType.STORE_TYPE:
							result += string.Format("${0}, 0x{1:x}(${2})",
											Utils.BintoRegName(inst.Substring(_pos_secondreg.Key, _pos_secondreg.Value)),
											Convert.ToInt16(inst.Substring(_pos_immediate.Key, _pos_immediate.Value), 2),
											Utils.BintoRegName(inst.Substring(_pos_firstdreg.Key, _pos_firstdreg.Value)));
							break;
						default:
							result = "nop";
							break;
					}
				}
			}
           catch( Exception e ) {
				Console.WriteLine(e.Message);
				result = "nop";
			}

			return result;
		}

	}
}
