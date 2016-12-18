//#define BigEndian
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace MIPSSimulatorWPF {

	public class TextBoxOutputter : TextWriter {

		private TextBox targetTextBox = null;

		public TextBoxOutputter(TextBox output ) {
			targetTextBox = output;
		}

		public override void Write( char value ) {
			base.Write(value);
			targetTextBox.Dispatcher.BeginInvoke(new Action(
				()=> {
					targetTextBox.AppendText(value.ToString( ));
					targetTextBox.ScrollToEnd( );
				}
				));
		}

		public override Encoding Encoding {
			get {
				return System.Text.Encoding.UTF8;
			}
		}
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	/// 
	public partial class MainWindow : Window {

		private DisplayInfo _displayInfo;

		private FileType _filetype;

		TextBoxOutputter outputter;

		public MainWindow( ) {
			InitializeComponent( );

			_displayInfo = new DisplayInfo( );

			this.DataContext = _displayInfo;

			_filetype = FileType.asm;

			outputter = new TextBoxOutputter(LogOutputTextBox);

			Console.SetOut(outputter);

			Console.WriteLine("LogOutput");

			UpdateTextRichTextBox( );

		}


		private void OpenCommand_Executed( object sender, RoutedEventArgs e ) {
			var dlg = new Microsoft.Win32.OpenFileDialog( );
			dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			dlg.DefaultExt = ".asm"; // Default file extension
			dlg.Filter = "Assembly|*.asm|COE|*.coe|Bin|*.bin"; // Filter files by extension
			if ( dlg.ShowDialog( ) == true ) {
				string filename = dlg.FileName;
				_displayInfo.Init( );
				switch ( filename.Split('.')[1].ToLower( ) ) {
					case "asm":
						_filetype = FileType.asm;
						_displayInfo.ParseFile_asm(filename);
						break;
					case "coe":
						_filetype = FileType.coe;
						_displayInfo.ParseFile_coe(filename);
						break;
					case "bin":
						_filetype = FileType.bin;
						_displayInfo.ParseFile_bin(filename);
						break;
					default:
						System.Windows.MessageBox.Show("Unknown File Type");
						return;
				}

				EditorRichBox.Document = _displayInfo.EditorDocument;

				_displayInfo.CurrentFile = filename;

				UpdateTextRichTextBox( );
			}

		}

		private void NewCommand_Executed( object sender, RoutedEventArgs e ) {

			_displayInfo.Init( );

			EditorRichBox.Document = new FlowDocument( );

		}

		private void SaveCommand_Executed( object sender, RoutedEventArgs e ) {

			if( string.IsNullOrEmpty(_displayInfo.CurrentFile) ) {  // If no file in using, create a new file
				var dlg = new Microsoft.Win32.SaveFileDialog( );
				dlg.FileName = "newfile"; // Default file name
				dlg.DefaultExt = ".asm"; // Default file extension
				dlg.Filter = "Assembly|*.asm"; // Filter files by extension
				Nullable<bool> result = dlg.ShowDialog( );
				if ( result == true )
					_displayInfo.CurrentFile = dlg.FileName;
			}
			// otherwise rewrite an existed file
			try {
				using ( StreamWriter file = new StreamWriter(File.Open(_displayInfo.CurrentFile, FileMode.OpenOrCreate)) ) {
					foreach ( Paragraph parag in EditorRichBox.Document.Blocks ) {
						var range = new TextRange(parag.ContentStart, parag.ContentEnd);
						file.WriteLine(range.Text);
						//file.WriteLine( );
					}
				}
			}
			catch( Exception except ) {
				System.Windows.MessageBox.Show("Save File Error:\n" + except.Message);
			}
			

		}

		private void  EditorRichBox_TextChanged( object sender, TextChangedEventArgs e ) {
			if ( EditorRichBox.Document == null )
				return;
			if ( _filetype != FileType.asm )
				return;

			EditorRichBox.TextChanged -= EditorRichBox_TextChanged;     // Important : Prevent an infinite loop of calling this TextChanged function
			EditorRichBox.BeginChange( );

			var caret = EditorRichBox.CaretPosition;
			int lenth = 0;
			var tmp = EditorRichBox.Document.ContentStart;
			while ( tmp != null && tmp.GetOffsetToPosition(caret) != 0 ) {
				lenth++;
				tmp = tmp.GetNextInsertionPosition(LogicalDirection.Forward);
			}

			//Console.WriteLine("length = " + lenth);

			//List<Paragraph> newparas = new List<Paragraph>( );

			//Task<List<Paragraph>> t = new Task<List<Paragraph>>(( ) => { return _displayInfo.HighlightDocs(EditorRichBox.Document); });
			//await Dispatcher.BeginInvoke(new Action(( ) => newparas = _displayInfo.HighlightDocs(EditorRichBox.Document)));
			//t.Start( );


			List<Paragraph> newparas = _displayInfo.HighlightDocs(EditorRichBox.Document);

			EditorRichBox.Document.Blocks.Clear( );
			foreach ( var para in newparas ) {
				if ( para == null )
					break;
				EditorRichBox.Document.Blocks.Add(para);
			}
			tmp = EditorRichBox.Document.ContentStart;
			for ( int i = 0; i < lenth; i++ )
				tmp = tmp.GetNextInsertionPosition(LogicalDirection.Forward);
			EditorRichBox.CaretPosition = tmp;
			EditorRichBox.EndChange( );
			EditorRichBox.TextChanged += EditorRichBox_TextChanged;
		}

		private void TabControl_SelectionChanged( object sender, SelectionChangedEventArgs e ) {
			if ( ViewEditor.IsSelected )
				EditMenu.Visibility = Visibility.Visible;
			else
				EditMenu.Visibility = Visibility.Collapsed;
		}

		private void MenuItem_Click( object sender, RoutedEventArgs e ) {
			var button = sender as MenuItem;

			switch( button.Header.ToString()) {
				case "Assembly":
				case "COE":
				case "Bin":
					OpenCommand_Executed(sender, e);
					break;
				case "_SaveAs":
					SaveAs_File( );
					break;
				case "_Single Step":
					UpdateTextRichTextBox(_displayInfo.SingleStep( ));
					break;
				case "_Run/Continue":
					_displayInfo.Run( );
					break;
				case "_Reinitialize":
					_displayInfo.Reinit( );
					UpdateTextRichTextBox( );
					break;
				case "_Stop":
					UpdateTextRichTextBox( );
					break;
				case "_About":
					System.Windows.MessageBox.Show("MIPS Simulator", "About");
					break;
            }
		}
		
		private void SaveAs_File( ) {
			var dlg = new Microsoft.Win32.SaveFileDialog( );
			dlg.FileName = "output"; // Default file name
			dlg.DefaultExt = ".asm"; // Default file extension
			dlg.Filter = "Assembly|*.asm|COE|*.coe|Bin|*.bin"; // Filter files by extension
			Nullable<bool> result = dlg.ShowDialog( );

			try {
				if ( result == true ) {
					string filename = dlg.FileName;
					string ext = filename.Split('.')[1].ToLower( );

					if ( ext == "coe" ) {
						using ( StreamWriter file = new StreamWriter(filename, true) ) {
							file.WriteLine("memory_initialization_radix=16;");
							file.WriteLine("memory_initialization_vector=");
							foreach ( var line in _displayInfo.InstCodeList ) {
								file.WriteLine(MIPSAssembler.Utils.NumtoHexStr(Convert.ToUInt32(line, 2), 8) + ",");
							}
						}
					} else if ( ext == "asm" ) {
						using ( StreamWriter file = new StreamWriter(filename, true) ) {
							foreach ( var line in _displayInfo.SourceCodeList ) {
								file.WriteLine(line);
							}
						}
					} else if ( ext == "bin" ) {
						using ( BinaryWriter file = new BinaryWriter(File.Open(filename, FileMode.CreateNew)) ) {
							foreach ( var line in _displayInfo.SourceCodeList ) {
#if BigEndian
								file.Write(Convert.ToUInt32(MIPSAssembler.Compiler.Encode(line), 2));
#else
								var int32 = Convert.ToUInt32(MIPSAssembler.Compiler.Encode(line), 2);
								for(int i=3; i>=0; i-- ) {
									file.Write((byte)( ( int32 >>i ) & 0xff ));
								}
#endif

							}
						}
					}

					if ( string.IsNullOrEmpty(_displayInfo.CurrentFile) ) {
						_displayInfo.CurrentFile = filename;
                    }
				}

			}catch(Exception except ) {

				System.Windows.MessageBox.Show("Save File Error:\n" + except.Message);

			}
			
			

		}

		private void UpdateTextRichTextBox( int currentLine = -1 ) {
			FlowDocument doc = new FlowDocument( );
			double pos = TextRichTextBox.VerticalOffset;
			foreach ( var line in _displayInfo.TextOutput.Split(new char[ ] { '\n' }, StringSplitOptions.RemoveEmptyEntries) ) {
				var newparag = new Paragraph( );
				if ( doc.Blocks.Count + 1 == currentLine ) {
					var span = new Span( );
					span.Inlines.Add(new Run(line));
					span.Background = new SolidColorBrush(Colors.LightBlue);
					newparag.Inlines.Add(span);
				} else {
					newparag.Inlines.Add(new Run(line));
				}
				doc.Blocks.Add(newparag);
			}
			TextRichTextBox.Document = doc;
			TextRichTextBox.ScrollToVerticalOffset(pos);
		}

		private void mainwindow_PreviewKeyDown( object sender, System.Windows.Input.KeyEventArgs e ) {
			
			switch ( e.Key ) {
				case Key.F5:
					_displayInfo.Run( );
					break;
				case Key.F6:
					_displayInfo.Reinit( );
					UpdateTextRichTextBox( );
					break;
				case Key.F7:
					UpdateTextRichTextBox( );
					break;
				case Key.F8:
					UpdateTextRichTextBox(_displayInfo.SingleStep( ));
					break;
			}

			return;
		}
	}

}
