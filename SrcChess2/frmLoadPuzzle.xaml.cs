using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmLoadPuzzle.xaml
    /// </summary>
    public partial class frmLoadPuzzle : Window {

        /// <summary>
        /// Puzzle item class use to fill the listview
        /// </summary>
        public class PuzzleItem {

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="iId">              Puzzle id</param>
            /// <param name="strDescription">   Description</param>
            /// <param name="bDone">            true if already been done</param>
            public PuzzleItem(int iId, string strDescription, bool bDone) {
                Id          = iId;
                Description = strDescription;
                Done        = bDone;
            }

            /// <summary>Puzzle id</summary>
            public  int     Id { get; private set; }
            /// <summary>Puzzle description</summary>
            public  string  Description { get; private set; }
            /// <summary>true if this puzzle has been done</summary>
            public  bool    Done { get; set; }
        }

        /// <summary>List of PGN Games</summary>
        static private List<PgnGame>    m_listPGNGame;
        /// <summary>PGN parser</summary>
        private PgnParser               m_pgnParser;
        /// <summary>Done mask</summary>
        private long[]                  m_plDoneMask;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="plDoneMask">   Mask of game which has been done</param>
        public frmLoadPuzzle(long[] plDoneMask) {
            List<PuzzleItem>    listPuzzleItem;
            PuzzleItem          puzzleItem;
            int                 iCount;
            bool                bDone;

            InitializeComponent();
            m_plDoneMask    = plDoneMask;
            m_pgnParser     = new PgnParser(false);
            if (m_listPGNGame == null) {
                BuildPuzzleList();
            }
            listPuzzleItem  = new List<PuzzleItem>(m_listPGNGame.Count);
            iCount          = 0;
            foreach (PgnGame pgnGame in m_listPGNGame) {
                if (plDoneMask == null) {
                    bDone = false;
                } else {
                    bDone = (plDoneMask[iCount / 64] & (1L << (iCount & 63))) != 0;
                }
                iCount++;
                puzzleItem  = new PuzzleItem(iCount, pgnGame.Event, bDone);
                listPuzzleItem.Add(puzzleItem);
            }
            listViewPuzzle.ItemsSource   = listPuzzleItem;
            listViewPuzzle.SelectedIndex = 0;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public frmLoadPuzzle() : this(null) {
        }

        /// <summary>
        /// Load PGN text from resource
        /// </summary>
        /// <returns>PGN text</returns>
        private string LoadPGN() {
            string                  strRetVal;
            Assembly                assem;
            System.IO.Stream        stream;
            System.IO.StreamReader  reader;

            assem   = GetType().Assembly;
            stream  = assem.GetManifestResourceStream("SrcChess2.111probs.pgn");
            reader  = new System.IO.StreamReader(stream, Encoding.ASCII);
            try {
                strRetVal   = reader.ReadToEnd();
            } finally {
                reader.Dispose();
            }
            return(strRetVal);
        }

        /// <summary>
        /// Build a list of puzzles using the PGN find in resource
        /// </summary>
        private void BuildPuzzleList() {
            string  strPGN;
            int     iSkippedCount;


            strPGN          = LoadPGN();
            m_pgnParser.InitFromString(strPGN);
            m_listPGNGame   = m_pgnParser.GetAllRawPGN(true /*bAttrList*/, false /*bMove*/, out iSkippedCount);
        }

        /// <summary>
        /// Gets the selected game
        /// </summary>
        public PgnGame Game {
            get {
                PgnGame             gameRetVal;
                ChessBoard          board;
                ChessBoard.PlayerE  ePlayer;

                gameRetVal                      = m_listPGNGame[listViewPuzzle.SelectedIndex];
                m_pgnParser.ParseFEN(gameRetVal.FEN, out ePlayer, out board);
                gameRetVal.StartingColor        = ePlayer;
                gameRetVal.StartingChessBoard   = board;
                return(gameRetVal);
            }
        }

        /// <summary>
        /// Returns the selected game index
        /// </summary>
        public int GameIndex {
            get {
                return(listViewPuzzle.SelectedIndex);
            }
        }

        /// <summary>
        /// Called when the OK button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        /// <summary>
        /// Called when the Cancel button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        /// <summary>
        /// Called when the Reset Done button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butResetDone_Click(object sender, RoutedEventArgs e) {
            List<PuzzleItem>    listPuzzleItem;

            if (MessageBox.Show("Are you sure you want to reset the Done state of all puzzles to false?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                for (int i = 0; i < m_plDoneMask.Length; i++) {
                    m_plDoneMask[i] = 0;
                }
                listPuzzleItem  = (List<PuzzleItem>)listViewPuzzle.ItemsSource;
                foreach (PuzzleItem item in listPuzzleItem) {
                    item.Done = false;
                }
                listViewPuzzle.ItemsSource = null;
                listViewPuzzle.ItemsSource = listPuzzleItem;
            }
        }

        /// <summary>
        /// Called when a selection is changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void listViewPuzzle_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            butOk.IsEnabled = listViewPuzzle.SelectedIndex != -1;
        }

        /// <summary>
        /// Called when a selection is double clicked
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void listViewPuzzle_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (listViewPuzzle.SelectedIndex != -1) {
                DialogResult = true;
            }
        }
    } // Class frmLoadPuzzle
} // Namespace
