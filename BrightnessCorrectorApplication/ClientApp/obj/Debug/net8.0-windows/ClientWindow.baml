   M S B A M L   `   `   `  ����  	ClientApp3    -DistributedImageProcessingClient.ClientWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Image Processing Client��5   	   $	��200��6)   $	��400��66   .��5      � $��10q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5	      $
��Auto �6   �� 5      $��* �6   .+�5   
   ��  �� Orientation$ Vertical=�6   $��0��6,   $��0,0,0,10q�69   .+�5      �� $��Server IP Address:��6   �� 5      -   5         ��Name$ IpAddressTextBox��5      $��	127.0.0.1��60   �� 5      $��Server Port:��6   $��0,10,0,0q�6,   �� 5      -   5        $ PortTextBox��5      $
��5000��6+   5      �� 5   
   $ 
Horizontal=�6   $��1��6.   $��0,0,0,10q�6;   .+�5      �� -   5         ��Name$ ConnectButton��5      $��Connect to Server��6,   $	��150��6H   6T   �� 5      -   5        $ DefaultIpButton��5      $��
Default IP��6.   $	��100��6C   $��10,0,0,0q�6O   6a   5      �� 5   
   -   5         ��Name$ StatusLabel��5      $��2��6)   $��Disconnected��66   $
��Bold"�6J   5      5       