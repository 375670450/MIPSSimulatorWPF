using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MIPSAssembler {
	class Program {
		static string filepath = @"C:\Users\37567\Documents\huibian1.asm";

		static void export( ) {
			var lines = File.ReadAllLines(filepath);

			Compiler cp = new Compiler( );
			cp.Init(lines.ToList());
			
			using(var output = new StreamWriter("output.txt", true) ) {
				foreach ( var line in cp.ExecuteLines ) {
					var code = Compiler.Encode(line);
					output.Write("OPcode=6'b{0}", code.Substring(0, 6));
					if( Utils.GetInstType(code.Substring(0,6)) == Utils.InstType.ALU_R_TYPE ) 
						output.Write("; Fun=6'b{0}", code.Substring(Decompiler._pos_funccode.Key, Decompiler._pos_funccode.Value));
					else
						output.Write("; Fun=6'b000000");
					output.Write(";     // {0}\n#100;", line);
					output.WriteLine( );
				}

				
			}



		}

        public static void Main( ) {
			
			Compiler cp = new Compiler( );
			Decompiler dcp = new Decompiler( );

			while ( true ) {
				string str = Console.ReadLine( );

				if( str.Split().Length == 1 ) {        // decompile
					Console.WriteLine( Decompiler.Decode(str.Trim()) );
				} else {
					Console.WriteLine("inst_field = " + Compiler.Encode(str.Trim( )) + ";");
				}

			}
			Console.ReadKey( );
			//return;
		}
		
	}
}
