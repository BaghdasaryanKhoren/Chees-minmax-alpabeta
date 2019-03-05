using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SrcChess2 {
    /// <summary>Pickup the colors use to draw the chess control</summary>
    public partial class frmBoardSetting : Window {
        /// <summary>Lite Cell Color</summary>
        public Color                                        LiteCellColor { get; private set; }
        /// <summary>Dark Cell Color</summary>
        public Color                                        DarkCellColor { get; private set; }
        /// <summary>White Piece Color</summary>
        public Color                                        WhitePieceColor { get; private set; }
        /// <summary>Black Piece Color</summary>
        public Color                                        BlackPieceColor { get; private set; }
        /// <summary>Background Color</summary>
        public Color                                        BackgroundColor { get; private set; }
        /// <summary>Selected PieceSet</summary>
        public PieceSet                                     PieceSet { get; private set; }
        /// <summary>List of Piece Sets</summary>
        private SortedList<string,PieceSet>                 m_listPieceSet;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public frmBoardSetting() {
            InitializeComponent();
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="colorLiteCell">    Lite Cells Color</param>
        /// <param name="colorDarkCell">    Dark Cells Color</param>
        /// <param name="colorWhitePiece">  White Pieces Color</param>
        /// <param name="colorBlackPiece">  Black Pieces Color</param>
        /// <param name="backGroundColor">  Main window background color</param>
        /// <param name="listPieceSet">     List of Piece Sets</param>
        /// <param name="pieceSet">         Current Piece Set</param>
        public frmBoardSetting(Color colorLiteCell, Color colorDarkCell, Color colorWhitePiece, Color colorBlackPiece, Color backGroundColor, SortedList<string, PieceSet> listPieceSet, PieceSet pieceSet) {
            InitializeComponent();
            LiteCellColor               = colorLiteCell;
            DarkCellColor               = colorDarkCell;
            WhitePieceColor             = colorWhitePiece;
            BlackPieceColor             = colorBlackPiece;
            BackgroundColor             = backGroundColor;
            m_listPieceSet              = listPieceSet;
            PieceSet                    = pieceSet;
            m_chessCtl.LiteCellColor    = colorLiteCell;
            m_chessCtl.DarkCellColor    = colorDarkCell;
            m_chessCtl.WhitePieceColor  = colorWhitePiece;
            m_chessCtl.BlackPieceColor  = colorBlackPiece;
            m_chessCtl.PieceSet         = pieceSet;
            Background                  = new SolidColorBrush(BackgroundColor);
            Loaded                     += new RoutedEventHandler(frmBoardSetting_Loaded);
            FillPieceSet();
        }

        /// <summary>
        /// Called when the form is loaded
        /// </summary>
        /// <param name="sender">   Sender Object</param>
        /// <param name="e">        Event parameter</param>
        private void frmBoardSetting_Loaded(object sender, RoutedEventArgs e) {
            customColorPickerLite.SelectedColor         = LiteCellColor;
            customColorPickerDark.SelectedColor         = DarkCellColor;
            customColorBackground.SelectedColor         = BackgroundColor;
            customColorPickerLite.SelectedColorChanged += new Action<Color>(customColorPickerLite_SelectedColorChanged);
            customColorPickerDark.SelectedColorChanged += new Action<Color>(customColorPickerDark_SelectedColorChanged);
            customColorBackground.SelectedColorChanged += new Action<Color>(customColorBackground_SelectedColorChanged);
        }

        /// <summary>
        /// Called when the dark cell color is changed
        /// </summary>
        /// <param name="color">    Color</param>
        private void customColorPickerDark_SelectedColorChanged(Color color) {
            DarkCellColor               = color;
            m_chessCtl.DarkCellColor    = DarkCellColor;
        }

        /// <summary>
        /// Called when the lite cell color is changed
        /// </summary>
        /// <param name="color">    Color</param>
        private void customColorPickerLite_SelectedColorChanged(Color color) {
            LiteCellColor               = color;
            m_chessCtl.LiteCellColor    = LiteCellColor;
        }

        /// <summary>
        /// Called when the background color is changed
        /// </summary>
        /// <param name="color">    Color</param>
        private void customColorBackground_SelectedColorChanged(Color color) {
            BackgroundColor             = color;
            Background                  = new SolidColorBrush(BackgroundColor);
        }


        /// <summary>
        /// Fill the combo box with the list of piece sets
        /// </summary>
        private void FillPieceSet() {
            int     iIndex;
            comboBoxPieceSet.Items.Clear();
            foreach (PieceSet pieceSet in m_listPieceSet.Values) {
                iIndex = comboBoxPieceSet.Items.Add(pieceSet.Name);
                if (pieceSet == PieceSet) {
                    comboBoxPieceSet.SelectedIndex  = iIndex;
                }
            }
        }

        /// <summary>
        /// Called when the reset to default button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void butResetToDefault_Click(object sender, RoutedEventArgs e) {
            LiteCellColor                       = Colors.Moccasin;
            DarkCellColor                       = Colors.SaddleBrown;
            BackgroundColor                     = Colors.SkyBlue;
            PieceSet                            = m_listPieceSet["leipzig"];
            Background                          = new SolidColorBrush(BackgroundColor);
            m_chessCtl.LiteCellColor            = LiteCellColor;
            m_chessCtl.DarkCellColor            = DarkCellColor;
            m_chessCtl.PieceSet                 = PieceSet;
            customColorPickerLite.SelectedColor = LiteCellColor;
            customColorPickerDark.SelectedColor = DarkCellColor;
            customColorBackground.SelectedColor = BackgroundColor;
            comboBoxPieceSet.SelectedItem       = PieceSet.Name;
        }

        /// <summary>
        /// Called when the PieceSet is changed
        /// </summary>
        /// <param name="sender">   Sender Object</param>
        /// <param name="e">        Event argument</param>
        private void comboBoxPieceSet_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int     iSelectedIndex;
            string  strVal;

            iSelectedIndex  = comboBoxPieceSet.SelectedIndex;
            if (iSelectedIndex != -1) {
                strVal              = comboBoxPieceSet.Items[iSelectedIndex] as string;
                PieceSet            = m_listPieceSet[strVal];
                m_chessCtl.PieceSet = PieceSet;
            }
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender">   Sender Object</param>
        /// <param name="e">        Event argument</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            DialogResult    = true;
            Close();
        }
    } // Class frmBoardSetting
} // Namespace
