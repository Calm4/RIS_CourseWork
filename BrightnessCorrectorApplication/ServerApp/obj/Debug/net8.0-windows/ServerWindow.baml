   M S B A M L   `   `   `  ����  	ServerApp3    -DistributedImageProcessingServer.ServerWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Image Processing Server��5   	   $	��500��6)   $	��800��66   .��5      �  � RowDefinitions 5   
   �� 5      $��* �6   �� 5	      $
��Auto �6   �� 5      $
��Auto �6    � ColumnDefinitions 5   
   �� 5      $��* �6   �� 5      $��* �6   .+�5   
   �� $��0��6   $��0��6%   $��10q�62   .+�5      �� $��Original Image��6   $
��Bold"�6.   $��0,0,0,10q�6@   �� 5      -   5         ��Name$ OriginalImage��5      $��Uniform=�6+   $	��300��6=   5      �� 5   
   $��1��6   $��0��6%   $��10q�62   .+�5      �� $��Processed Image��6   $
��Bold"�6/   $��0,0,0,10q�6A   �� 5      -   5        $ ProcessedImage��5      $��Uniform=�6,   $	��300��6>   �� 5      -   5         ��Name$ StatusLabel��5      $��Waiting for clients...��6-   $��0,10,0,0q�6K   5      �� 5"   
   $��0��6   $��2��6%   $��1��69   $��10q�6F   .+�5#      �� $��Connected Clients��6   $
��Bold"�61   $��0,0,0,10q�6C   �� 5$      -   5         ��Name$ 
ClientList��5$      $	��100��6*   $
��Auto=�67   ��5%      �� 5&      .R�5'      ��  �� Orientation$ 
Horizontal=�6%   .+�5(      �� $	��150��6D   ��6(   �� *
ClientName+6D   �� 5)      $	��100��6@   ��6(   �� *Status+6@   5*      5+      5-      5.      �� 51   
   $��0��6   $��2��6%   $��2��69   $��Center=�6F   $��10q�6c   .+�52      �� -   5         ��Name$ LoadImageButton��52      $��
Load Image��6.   $	��150��6C   ${�False��6m   53      54      55      