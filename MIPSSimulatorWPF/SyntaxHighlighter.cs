using System;
using System.Linq;

namespace MIPSSimulatorWPF {
	public static class SyntaxHighlighter {

		public enum SyntaxType { PlainText, Operation, Number, Register, Comment };

		public static readonly char[ ] Delims = MIPSAssembler.Compiler.Delims;

		public static readonly char[ ] HexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
		

		public static SyntaxType GetSyntaxStyle(string text) {
			
			text = text.ToLower();
			if (text[0] == '$' && text.Length > 1 && MIPSAssembler.Utils.Registers.Contains(text.Substring(1))) {
				return SyntaxType.Register;
			}else if ( MIPSAssembler.Utils.Operations.Contains(text)) {
				return SyntaxType.Operation;
			}else if (MIPSAssembler.Utils.Registers.Contains(text)) {
			}else if( Char.IsDigit(text[0]) ) {
				if ( text.Length > 2 && text.Substring(0, 2) == "0x" )
					text = text.Substring(2);
				foreach (var c in text)
					if ( !HexDigits.Contains(c) )
						return SyntaxType.PlainText;
				return SyntaxType.Number;
			}

			return SyntaxType.PlainText;
		}

	}
}
