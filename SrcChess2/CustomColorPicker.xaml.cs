using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for CustomColorPicker.xaml
    /// </summary>
    public partial class CustomColorPicker : UserControl
    {
        /// <summary>
        /// SelectedColor event
        /// </summary>
        public event Action<Color> SelectedColorChanged;

        String _hexValue = string.Empty;

        /// <summary>
        /// Color in Hexadecimal
        /// </summary>
        public String HexValue
        {
            get { return _hexValue; }
            set { _hexValue = value; }
        }

        private Color selectedColor = Colors.Transparent;
        /// <summary>
        /// Selected Color
        /// </summary>
        public Color SelectedColor
        {
            get { return selectedColor; }
            set
            {
                if (selectedColor != value)
                {
                    selectedColor = value;
                    cp.CustomColor  = value;
                    Update();
                }
            }
        }

        bool _isContexMenuOpened = false;
        /// <summary>
        /// Class Ctor
        /// </summary>
        public CustomColorPicker()
        {
            InitializeComponent();
            b.ContextMenu.Closed += new RoutedEventHandler(ContextMenu_Closed);
            b.ContextMenu.Opened += new RoutedEventHandler(ContextMenu_Opened);
            b.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(b_PreviewMouseLeftButtonUp);
        }

        void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            _isContexMenuOpened = true;
        }

        void Update() {
            recContent.Fill = new SolidColorBrush(cp.CustomColor);
            HexValue        = string.Format("#{0}", cp.CustomColor.ToString().Substring(1));
            selectedColor   = cp.CustomColor;
        }

        void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            if (!b.ContextMenu.IsOpen)
            {
                if (SelectedColorChanged != null)
                {
                    SelectedColorChanged(cp.CustomColor);
                }
                Update();

            }
            _isContexMenuOpened = false;
        }

        void b_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isContexMenuOpened)
            {
                if (b.ContextMenu != null && b.ContextMenu.IsOpen == false)
                {
                    b.ContextMenu.PlacementTarget = b;
                    b.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                    ContextMenuService.SetPlacement(b, System.Windows.Controls.Primitives.PlacementMode.Bottom);
                    b.ContextMenu.IsOpen = true;
                }
            }
        }
    }
}
