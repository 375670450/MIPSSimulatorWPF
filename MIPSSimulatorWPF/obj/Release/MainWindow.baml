   M S B A M L   `   `   `  ���� K  GMIPSSimulatorWPF, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null3clr-namespace:MIPSSimulatorWPFMIPSSimulatorWPF  !    MIPSSimulatorWPF.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     0localclr-namespace:MIPSSimulatorWPF     :xctk)http://schemas.xceed.com/wpf/xaml/toolkit     -     �Name$  
mainwindow��$��721.6��$	��896�� � SizeToContent$ WidthAndHeight=� � Title ��  �� Path$ WindowTitle� �� Mode$ OneWay=� � 	Resources  �� 
TargetType �� Property �� Value��%d   '��       �� " ��.���  � ��$ 16(���  � ��$ Courier New)���  � ��$
 Auto=�	 J� CommandBindings	 )  #System.Windows.Input.CommandBinding  -   
   Command$
 ApplicationCommands.New��  -   $
 ApplicationCommands.Open��  -   $
 ApplicationCommands.Save��.��� #��� �� � RowDefinitions �� $
��Auto ��� $��30��$��30���� $	��500���� $
��Auto ��� $	��100��$	��150��.+�o� -    o�Name$
 menu��$��0��$��13.333(�$��26��$	��Top=�.@�m� $��_File��.@�m� $��_NewFile�� m� InputGestureText$ Ctrl+N�� m� Command$ ApplicationCommands.New��m� $��	_LoadFile��$ Ctrl+O��$ ApplicationCommands.Open��m� $��_Save��$ Ctrl+S��$ ApplicationCommands.Save���� m� -   $��_SaveAs��.@�m� $��_Print��$ ApplicationCommands.Print���� m� $��_Exit��$ Alt+F4��$ ApplicationCommands.Close��m� -    m�Name$ EditMenu��$��_Edit��.@�m� $��_Undo��$ Ctrl+Z��$ ApplicationCommands.Undo��m� $
��_Cut��$ Ctrl+X��$ ApplicationCommands.Cut��m� $��_Copy��$ Ctrl+C��$ ApplicationCommands.Copy��m� $��_Paste��$ Ctrl+V��$ ApplicationCommands.Paste��m� $��_Delete��$	 Del��$  ApplicationCommands.Delete��m� $��_Find��$ Ctrl+F��$ ApplicationCommands.Find��m� $��_Replace��$ Ctrl+H��$! ApplicationCommands.Replace��m� $��
_SelectAll��$ Ctrl+A��$# ApplicationCommands.SelectAll��m� $��_Build��.@�m� -   $��Assembly��m� -	   $	��COE��m� -
   $	��Bin��m� $��
_Simulator��.@�m� -   $��_Run/Continue��$ F5��m� -   $��_Reinitialize��$ F6��m� -   $��_Stop��$ F7��m� -   $��_Single Step��$ F8��m� $��_Help��.@�m� -   $��_About��� -   $��Icons��$��1��$	��100��$
��Left=�$��0,0,0,0q� � ColumnDefinitions �� �� �� �� .+�m� -   $��_Run/Continue��$��0�� � ToolTip$ Run/Continue/F5�� m� Icon �� $��./ico/run.png��m� -   $��_Reinitialize��$��1��$ Reinitialize/F6�� �� $��./ico/reinit.png��m� -   $��_Stop��$��2��$ Stop/F7�� �� $��./ico/stop.png��m� -   $��_Single Step��$��3��$ Single Step/F8�� �� $��./ico/next.png��� $��2�� �� $	��200��$	��300���� $��5 ��� $	��500��.+��� -    ��Name$ RegisterStatusTextBox��$��0��$��Stretch=�$��10q�$	��200�� �� TextWrapping$
 Wrap=� �� 
IsReadOnly . $��2������ $ RegistersOutput�$ OneWay=��� $��1��$��5��$��Center=�$��Stretch=� �� ShowsPreview . �� -   $��10q�$��2��$��20��$��15(�$��0q�.@��� -    ��Name$ ViewText��$
��Text��$��50��.���� -    ��Name$ TextRichTextBox��$��Stretch=� . $	��400����. $
��Auto=�$
��Auto=� ��%3   'J�       �� " J�.���  �@��Margin$ 0q��� -   $ ViewData��$
��Data��$��50��$��0q�$��26��$��0,-2,0,0q�$	��Top=�.���� -   $ DataTextBox��$��Stretch=�$
 Wrap=�$	��400�� . ���� $ 
DataOutput�$ OneWay=��� -   $ 
ViewEditor��$��Editor��.���� -   $��EditorRichBox����. $
��Auto=�$
��Auto=� �� AcceptsReturn .  �� 
AcceptsTab .  ��%3   'J�       �� " J�.���  �@��Margin$ 0q��� $��3��$��5��$��Stretch=�$��Center=� . � $��4��.+��� -   $ LogOutputTextBox��$��10,10,10,10q�$
 Wrap=����	#FF2DC500 . 