using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using MIPSAssembler;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace MIPSSimulatorWPF {

	public enum FileType { bin, coe, asm }

	public struct SyntaxTag {
		public TextPointer start, end;
		public SyntaxHighlighter.SyntaxType type;
		public SyntaxTag( TextPointer s, TextPointer e, SyntaxHighlighter.SyntaxType t ) {
			start = s; end = e; type = t;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public sealed class CallerMemberNameAttribute : Attribute {
	}

	public class DisplayInfo : INotifyPropertyChanged {

		private static string[ ] FrameworkCodes = {
			"lw $a0, 0($sp) # argc ",
			"addiu $a1, $sp, 4 # argv ",
			"addiu $a2, $a1, 4 # envp ",
			"sll $v0, $a0, 2 ",
			"addu $a2, $a2, $v0 ",
			"jal main ",
			"nop",
			"li $v0, 10 ",
			"syscall # syscall 10 (exit) "
		};

		private static string _textTitle = "\t\t\t\t\t\tUser Text Segment \n";

		private static string _dataTitle = "User data segment";

		public string CurrentFile {
			set; get;
		}

		public string WindowTitle {
			get {
				return "MIPS Simulator " + CurrentFile;
			}
		}

		private Compiler _compiler;

		private Decompiler _decompiler;

		private string _registersOutput = "RegistersOutput";

		public string RegistersOutput {
			get {
				return _registersOutput;
			}
			private set {
				_registersOutput = value;
				OnPropertyChanged( );
			}
		}
		
		private string _dataOutput = "DataOutput";

		public string DataOutput {
			get {
				return _dataOutput;
			}
			private set {
				_dataOutput = value;
				OnPropertyChanged( );
			}
		}

		private string _textOutput = "TextOutput";

		public string TextOutput {
			get {
				return _textOutput;
			}
			private set {
				_textOutput = value;
				OnPropertyChanged( );
			}
		}

		private List<string> instCodeList;

		public List<string> InstCodeList {
			get {
				return instCodeList;
			}
			private set {
				instCodeList = value;
			}
		}

		private List<string> sourceCodeList;

		public List<string> SourceCodeList {
			get {
				return sourceCodeList;
			}
			private set {
				sourceCodeList = value;
			}
		}

		private FlowDocument _document;

		public FlowDocument EditorDocument {
			get {
				return _document;
			}
		}

		public DisplayInfo( ) {
			Init( );
		}

		public void Init( ) {

			_compiler = new Compiler( );

			_decompiler = new Decompiler( );

			InstCodeList = new List<string>( );

			SourceCodeList = new List<string>( );

			TextOutput = _textTitle + "\n";

			DataOutput = _dataTitle + "[10000000]..[10040000]" + "\n";

			DataOutput += "[10000000]..[1003ffff]  00000000";

			_document = new FlowDocument( );

			_updateTextOutput(FrameworkCodes.ToList( ));

			_updateRegisterOutput( );

			_updateDataOutput( );

		}

		#region PRIVATE_UPDATE
		private void _updateDataOutput( ) {						// pending to be improved

			DataOutput = "User Data Segment \n" ;
			int j, k;

			foreach(var dataseg in _compiler.DataSegs) {
				var addrstart = dataseg.addr;
				UInt32 addrend = addrstart + (UInt32)dataseg.size;     // sizeof(byte) == 0x08
				while ( ( addrend & 0x0F ) != 0 )
					addrend++;

				DataOutput += string.Format("[{0:x8}] ... [{1:x8}]", addrstart, addrend) + "\n";

				j = 0;
				k = 0;
				while ( addrstart < addrend ) {
					DataOutput += string.Format("[{0:x8}]", addrstart) + "\t";
					do {
						for ( int t = 0; t < 4; t++ ) {
							DataOutput += string.Format("{0:x2}", j < dataseg.size ? dataseg.data.ElementAt(j++) : 0 );
						}
						addrstart += 0x04;
						DataOutput += "  ";
					} while ( ( addrstart & 0x0F ) != 0 && ( addrstart < addrend ) );
					DataOutput += "\t";
					for ( ; k < j; k++ ) {
						if ( (Int32)dataseg.data.ElementAt(k) > 32 && (Int32)dataseg.data.ElementAt(k) < 127 )
							DataOutput += Convert.ToChar(dataseg.data.ElementAt(k));
						else
							DataOutput += '.';
					}
					DataOutput += "\n";
				}
				DataOutput += "\n";
			}

			DataOutput += "\n\n\nUser Stack Segment\n";
			var stackseg = _compiler.StackSeg;
			var addrStart = _compiler.Registers[Utils.RegNametoNum("sp")] - 0x10;
			var addrEnd = _compiler.StackSegEnd;
			while ( ( addrStart & 0x0F ) != 0 )
				addrStart--;

			DataOutput += string.Format("[{0:x8}] ... [{1:x8}]", addrStart, addrEnd) + "\n";
			j = 0;
			k = 0;
			while ( addrStart < addrEnd ) {
				DataOutput += string.Format("[{0:x8}]", addrStart) + "\t";
				do {
					for ( int t = 0; t < 4 && t < stackseg.data[j++]; t++ ) {
						DataOutput += string.Format("{0:x2}", stackseg.data.ElementAt(j++));
					}
					addrStart += 0x04;
					DataOutput += "  ";
				} while ( ( addrStart & 0x0F ) != 0 && ( addrStart < addrEnd ) );
				DataOutput += "\t";
				for ( ; k < j; k++ ) {
					if ( (Int32)stackseg.data.ElementAt(k) > 32 && (Int32)stackseg.data.ElementAt(k) < 127 )
						DataOutput += Convert.ToChar(stackseg.data.ElementAt(k));
					else
						DataOutput += '.';
				}
				DataOutput += "\n";
			}

			//if ( addrStart < addrEndActual )
			//	DataOutput += string.Format("[{0:x8}] ... [{1:x8}] \t 00000000\n", addrStart, addrEndActual);

		}

		private void _updateRegisterOutput( ) {
			string format = "{0} = 0x{1:X}\n", format2 = "R{0} [{1}] = 0x{2:X}\n", result = "";
			result += string.Format(format, "PC", _compiler.PC);
			result += "\n";
			result += string.Format(format, "HI", _compiler.HI);
			result += string.Format(format, "LO", _compiler.LO);
			result += "\n";
			for ( int i = 0; i < Compiler.register_count; i++ ) {
				result += string.Format(format2, i, Utils.NumtoRegName(i), _compiler.Registers[i]);
			}
			RegistersOutput = result;
		}

		private void _updateTextOutput( List<string> Lines ) {
			

			int i = 0;

			TextOutput = string.Empty;

			_compiler.Init(Lines);

			UInt32 addr = _compiler.CodeSegStart;

			foreach ( var originline in _compiler.OriginLines ) {
				string instcode = Compiler.Encode(_compiler.ExecuteLines[i]);
				InstCodeList.Add(instcode);
				SourceCodeList.Add(originline);
				TextOutput += string.Format("[{0}] {1}  {2,-20} \t\t\t; {3}\n",
											Utils.NumtoHexStr(addr, 8),
											Utils.NumtoHexStr(Convert.ToUInt32(instcode, 2), 8),
											_compiler.ExecuteLines[i],
											originline);
				i++;
				addr += 4;
			}

		}

		private void _updateEditorDocument( List<string> Lines ) {

			foreach ( var line in Lines ) {
				var parag = new Paragraph( );
				parag.Inlines.Add(line);
				_document.Blocks.Add(parag);
			}

		}
#endregion

		#region PARSE_FILES
		public void ParseFile_asm(string filename ) {
			List<string> Lines = FrameworkCodes.ToList( );
			List<string> OrigLines = new List<string>( );
			CurrentFile = filename;

			try {

				OrigLines = File.ReadAllLines(filename).ToList( );

				Lines.AddRange(OrigLines);

				_updateEditorDocument(OrigLines);

				_updateTextOutput(Lines);

				_updateDataOutput( );
			}

			catch ( Exception except ) {
				System.Windows.MessageBox.Show("Parse File Failed :\n" + except.Message);
				return;
			}			

		}

		public void ParseFile_bin(string filename ) {
			List<string> Lines = FrameworkCodes.ToList( );
			List<string> OrigLines = new List<string>( );
			CurrentFile = filename;

			try {
				Byte[ ] bytes = File.ReadAllBytes(filename);

				for ( int i = 0; i + 3 < bytes.Length; i++ ) {
					string bincode = "";
					for ( int j = 0; j < 4; j++ ) {

						bincode += Convert.ToString(bytes[i + j], 2).PadLeft(8, '0');
					}
					OrigLines.Add(bincode);

					Lines.Add(Decompiler.Decode(bincode));
				}


				Lines.AddRange(Lines);

				_updateEditorDocument(OrigLines);

				_updateTextOutput(Lines);

				_updateDataOutput( );
			}
			catch ( Exception except ) {
				System.Windows.MessageBox.Show("Parse File Failed :\n" + except.Message);
				return;
			}

		}

		public void ParseFile_coe(string filename ) {
			List<string> Lines = FrameworkCodes.ToList( );
			List<string> OrigLines = File.ReadAllLines(filename).ToList();
			CurrentFile = filename;

			int datawidth = 0, radix = 0;

			try {
				for ( int i = 0; i < OrigLines.Count; i++ ) {
					var tmpline = OrigLines[i].Split(';')[0].ToLower( ); ;
					if ( tmpline.Length == 0 )
						continue;
					if ( tmpline.StartsWith("memory_initialization_radix") ) {
						switch ( Convert.ToInt32(tmpline.Split(new char[ ] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries)[1]) ) {
							case 2:
								datawidth = 32;
								radix = 2;
								break;
							//case 8:
							//	datawidth = 2;
							// radix = 8;
							// break;
							case 16:
								datawidth = 8;
								radix = 16;
								break;
						}
					}
					if ( tmpline.StartsWith("memory_initialization_vector") ) {
						var tmplst = tmpline.Split(new char[ ] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
						if ( tmplst.Length > 1 )
							tmpline = tmplst[1];
						else
							tmpline = string.Empty;
						while ( !tmpline.Contains(';') ) {
							tmpline += OrigLines[++i].Replace(",", "").Replace(" ", "");
						}
						tmpline = tmpline.Replace(";", "");
						for ( int j = 0; j + datawidth < tmpline.Length; j += datawidth ) {
							var tmpstr = Convert.ToString(Convert.ToUInt32(tmpline.Substring(j, datawidth), radix), 2).PadLeft(32, '0');
							Lines.Add(Decompiler.Decode(tmpstr));
						}
					}
				}

				_updateEditorDocument(OrigLines);

				_updateTextOutput(Lines);

				_updateDataOutput( );

			}
			catch ( Exception except ) {
				System.Windows.MessageBox.Show("Parse File Failed :\n" + except.Message);
				return;
			}
			
		}

		#endregion

		#region HIGHLIGHT_WORK
		public Span _getSyntaxSpan(string word, SyntaxHighlighter.SyntaxType style) {
			#region ApplySpanStyle
			Span s = new Span(new Run(word));
			switch ( style ) {
				case SyntaxHighlighter.SyntaxType.Operation:
					s.Foreground = new SolidColorBrush(Colors.Blue);
					s.FontWeight = FontWeights.Bold;
					break;

				case SyntaxHighlighter.SyntaxType.Comment:
					s.Foreground = new SolidColorBrush(Colors.DarkGray);
					break;

				case SyntaxHighlighter.SyntaxType.Number:
					s.Foreground = new SolidColorBrush(Colors.Orange);
					break;

				case SyntaxHighlighter.SyntaxType.Register:
					s.Foreground = new SolidColorBrush(Colors.Green);
					s.FontWeight = FontWeights.Bold;
					break;

				case SyntaxHighlighter.SyntaxType.PlainText:
					// Do Nothing
					
                    break;
			}
			return s;
			#endregion
		}
		/*
		public void _subwork(FlowDocument doc, int start, int size, out List<Paragraph> res) {
			res = new List<Paragraph>( );
			size = ( doc.Blocks.Count - ( start + size ) < 0 ? doc.Blocks.Count - (start+size) : size );
			for ( int j = start; j < start+size; j++ ) {
				var parag = (Paragraph)doc.Blocks.ElementAt(j);
				var newParag = new Paragraph( );
				var text = new TextRange(parag.ContentStart, parag.ContentEnd).Text;
				var style = SyntaxHighlighter.SyntaxType.PlainText;
				int sindex = 0, eindex = 0;
				string word = "";

				for ( int i = 0; i <= text.Length; i++ ) {
					style = SyntaxHighlighter.SyntaxType.PlainText;
					word = "";

					if ( i == text.Length ) {
						eindex = text.Length;
						if ( eindex - sindex > 0 ) {
							word = text.Substring(sindex);
							style = SyntaxHighlighter.GetSyntaxStyle(text.Substring(sindex, eindex - sindex));
							newParag.Inlines.Add(_getSyntaxSpan(word, style));
						}
						break;
					} else if ( text[i] == '#' ) {
						eindex = text.Length;
						if ( text.IndexOf('\n') >= 0 )
							eindex = text.IndexOf('\n');
						eindex = i;
						word = text.Substring(i);
						newParag.Inlines.Add(_getSyntaxSpan(word, SyntaxHighlighter.SyntaxType.Comment));
						break;
					} else if ( Char.IsWhiteSpace(text[i]) || SyntaxHighlighter.Delims.Contains(text[i]) ) {
						eindex = i;
						if ( eindex - sindex > 0 ) {
							word = text.Substring(sindex, eindex - sindex);
							style = SyntaxHighlighter.GetSyntaxStyle(word);
							newParag.Inlines.Add(_getSyntaxSpan(word, SyntaxHighlighter.GetSyntaxStyle(word)));
						}
						sindex = i + 1;
						newParag.Inlines.Add(text[i].ToString( ));
					}
				}
				res.Add(newParag);
			}
			return ;
		}
		*/

		public List<Paragraph> HighlightDocs( FlowDocument doc ) {          // [To Be OPTIMIZE]
			var newParaList = new List<Paragraph>( );
			var documentRange = new TextRange(doc.ContentStart, doc.ContentEnd);
			List<Paragraph>[ ] resList = new List<Paragraph>[5];
			//int partition = 5;
			//int sizePerTask = (doc.Blocks.Count + 5) / partition;
			//List<Task> TaskList = new List<Task>( );
			//for(int i=0; i<partition; i++ ) {
			//	var copyOfi = i;
			//	var lastTask = new Task(new Action(( ) => _subwork(doc, copyOfi * sizePerTask, sizePerTask, out resList[copyOfi])));
			//	lastTask.Start( );
			//	TaskList.Add(lastTask);
			//}

			//Task.WaitAll(TaskList.ToArray( ));

			for ( int j = 0; j < doc.Blocks.Count; j++ ) {
				var parag = (Paragraph)doc.Blocks.ElementAt(j);
				var newParag = new Paragraph( );
				var text = new TextRange(parag.ContentStart, parag.ContentEnd).Text.Replace("\t", "    ");
				var style = SyntaxHighlighter.SyntaxType.PlainText;
				int sindex = 0, eindex = 0;
				string word = "";

				for ( int i = 0; i <= text.Length; i++ ) {
					style = SyntaxHighlighter.SyntaxType.PlainText;
					word = "";

					if ( i == text.Length ) {
						eindex = text.Length;
						if ( eindex - sindex > 0 ) {
							word = text.Substring(sindex);
							style = SyntaxHighlighter.GetSyntaxStyle(text.Substring(sindex, eindex - sindex));
							newParag.Inlines.Add(_getSyntaxSpan(word, style));
						}
						break;
					} else if ( text[i] == '#' ) {
						eindex = text.Length;
						if ( text.IndexOf('\n') >= 0 )
							eindex = text.IndexOf('\n');
						eindex = i;
						word = text.Substring(i);
						newParag.Inlines.Add(_getSyntaxSpan(word, SyntaxHighlighter.SyntaxType.Comment));
						break;
					} else if ( Char.IsWhiteSpace(text[i]) || SyntaxHighlighter.Delims.Contains(text[i]) ) {
						eindex = i;
						if ( eindex - sindex > 0 ) {
							word = text.Substring(sindex, eindex - sindex);
							style = SyntaxHighlighter.GetSyntaxStyle(word);
							newParag.Inlines.Add(_getSyntaxSpan(word, SyntaxHighlighter.GetSyntaxStyle(word)));
						}
						sindex = i + 1;
						newParag.Inlines.Add(text[i].ToString( ));
					}
				}
				newParaList.Add(newParag);
			}

			return newParaList;
		}
		#endregion

		#region SIMULATION_WORK
		public int SingleStep( ) {					// return the next line number
			if ( !_compiler.IsReadySimulation )
				return -1;
			try {
				_compiler.SingleStep( );
			}

			catch ( Exception e ) {
				System.Windows.MessageBox.Show("Simulation Error:\n" + e.Message);
			}

			_updateRegisterOutput( );

			_updateDataOutput( );

			return (int)(_compiler.PC + 4 - _compiler.CodeSegStart) / 4;
		}

		public int Run( ) {

			//_compiler.ReInit( );

			try {
				while ( _compiler.IsReadySimulation ) {
					_compiler.SingleStep( );
					
					System.Threading.Thread.Sleep(1000);
				}
			}
			catch ( Exception e ) {
				System.Windows.MessageBox.Show("Simulation Error:\n" + e.Message);
			}
			
			_updateRegisterOutput( );

			_updateDataOutput( );

			return (int)( _compiler.PC + 4 - _compiler.CodeSegStart ) / 4;
		}

		public void Reinit( ) {

			_compiler.ReInit( );

			_updateDataOutput( );

			_updateRegisterOutput( );

		}

		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged( [CallerMemberName] string propertyname = null ) {
			// 传入的参数为属性的名称,错了则无法Binding, 可以用特性 CallerMemberName 方便地避免
			// 应由属性的【set访问器调用】
			if ( PropertyChanged != null ) {
				PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
			}
		}

	}
}
