   M S B A M L   `   `   `  ���� Q  MLCMS_ODO_GPS_GENERATOR, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null?$clr-namespace:LCMS_ODO_GPS_GENERATORLCMS_ODO_GPS_GENERATOR  '    !LCMS_ODO_GPS_GENERATOR.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     6local$clr-namespace:LCMS_ODO_GPS_GENERATOR       � Title$
  LCMS��$	��500��$	��750��.���  � ColumnDefinitions �� $	��23* ��� $
��352* � � RowDefinitions �� $	��230 ��� $��* �.+��� $��0��$��	0,0,0,205q�$��2��$��2��.+��� $��	0 10 0 10q�$��Center=�$��23(�$
��Bold"�$��ODO-GPS Generator���� $��0�� �� Orientation$ 
Horizontal=�$��Center=�.+��� -    ��Name$ Btn_Procesar��$	��Top=�$��Center=�$	��100��$��10q�$��40��$��Procesar��$
��Bold"�$��14(��� $��Center=�$	��250��$ Vertical=�.+��� -   $  Btn_GenerarKML_Incidencias��$��Center=�$��0 10 0 5q�$	��180��$��40��$��Generar KML/Incidencias��$
��Bold"�$��14(��� $��Center=�$��10(�$
��Bold"�$)��#Al seleccionar fichero, el programa���� $��Center=�$��10(�$
��Bold"�$6��0recogerá la carpeta de una jornada (aaaa-mm-dd)���� $��Center=�$��10(�$
��Bold"�$��2024-08-05\1\Archivo1.xml��� -    �Name$ ProgressBar��$��20��$	��200��$��	0 10 40 5q�� -   $ ProgressBarKmlIncidencias�� � IsIndeterminate . $��20��$	��200��$��0 0 40 0q�]� $��10,10,10,10q�$��1����. $��2��.+��� $��Mensajes del sistema��$
��Bold"�$��14(�$	��Top=��� -    ��Name$ tbMensajesSistema��$
��Auto=�$
��auto�� �� TextWrapping$
 Wrap=�$��Stretch=�	 �� AcceptsReturn	 . 