using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Text;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmPgnGamePicker.xaml
    /// </summary>
    public partial class frmPgnGamePicker : Window {
        /// <summary>Item used to fill the description listbox so we can find the original index in the list after a sort</summary>
        private class PGNGameDescItem : IComparable<PGNGameDescItem> {
            /// <summary>Game description</summary>
            private string  m_strDesc;
            /// <summary>Original position index</summary>
            private int     m_iIndex;

            /// <summary>
            /// Class constructor
            /// </summary>
            /// <param name="strDesc">  Item description</param>
            /// <param name="iIndex">   Item index</param>
            public PGNGameDescItem(string strDesc, int iIndex) {
                m_strDesc = strDesc;
                m_iIndex  = iIndex;
            }

            /// <summary>
            /// Description of the item
            /// </summary>
            public string Description {
                get {
                    return(m_strDesc);
                }
            }

            /// <summary>
            /// Index of the item
            /// </summary>
            public int Index {
                get {
                    return(m_iIndex);
                }
            }

            /// <summary>
            /// IComparable interface
            /// </summary>
            /// <param name="other">    Item to compare with</param>
            /// <returns>
            /// -1, 0, 1
            /// </returns>
            public int CompareTo(PGNGameDescItem other) {
                return(String.Compare(m_strDesc, other.m_strDesc));
            }

            /// <summary>
            /// Return the description
            /// </summary>
            /// <returns>
            /// Description
            /// </returns>
            public override string ToString() {
                return(m_strDesc);
            }
        } // Class PGNGameDescItem

        /// <summary>List of moves for the current game</summary>
        public List<MoveExt>                MoveList { get; private set; }
        /// <summary>Selected game</summary>
        public string                       SelectedGame { get; private set; }
        /// <summary>Starting board. Null if standard board</summary>
        public ChessBoard                   StartingChessBoard { get; private set; }
        /// <summary>Starting color</summary>
        public ChessBoard.PlayerE           StartingColor { get; private set; }
        /// <summary>White Player Name</summary>
        public string                       WhitePlayerName { get; private set; }
        /// <summary>Black Player Name</summary>
        public string                       BlackPlayerName { get; private set; }
        /// <summary>White Player Type</summary>
        public PlayerTypeE                  WhitePlayerType { get; private set; }
        /// <summary>Black Player Type</summary>
        public PlayerTypeE                  BlackPlayerType { get; private set; }
        /// <summary>White Timer</summary>
        public TimeSpan                     WhiteTimer { get; private set; }
        /// <summary>Black Timer</summary>
        public TimeSpan                     BlackTimer { get; private set; }
        /// <summary>Utility class</summary>
        private PgnUtil                     m_pgnUtil;
        /// <summary>List of games</summary>
        private List<PgnGame>               m_pgnGames;
        /// <summary>PGN parser</summary>
        private PgnParser                   m_pgnParser;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public frmPgnGamePicker() {
            InitializeComponent();
            m_pgnUtil               = new PgnUtil();
            m_pgnParser             = new PgnParser(false /*bDiagnose*/);
            SelectedGame            = null;
            StartingColor           = ChessBoard.PlayerE.White;
            StartingChessBoard      = null;
            m_pgnGames              = new List<PgnGame>(65536);
        }

        /// <summary>
        /// Get the selected game content
        /// </summary>
        /// <returns>
        /// Game or null if none selected
        /// </returns>
        private string GetSelectedGame() {
            string  strRetVal;
            PgnGame pgnGame;
            int     iSelectedIndex;
            
            iSelectedIndex = listBoxGames.SelectedIndex;
            if (iSelectedIndex != -1) {
                pgnGame     = m_pgnGames[iSelectedIndex];
                strRetVal   = m_pgnParser.PGNLexical.GetStringAtPos(pgnGame.StartingPos, pgnGame.Length);
            } else {
                strRetVal = null;
            }
            return(strRetVal);
        }

        /// <summary>
        /// Refresh the textbox containing the selected game content
        /// </summary>
        private void RefreshGameDisplay() {
            SelectedGame        = GetSelectedGame();
            textBoxGame.Text    = (SelectedGame == null) ? "" : SelectedGame;
        }

        /// <summary>
        /// Get game description
        /// </summary>
        /// <param name="pgnGame">  PGN game</param>
        /// <returns></returns>
        protected virtual string GetGameDesc(PgnGame pgnGame) {
            StringBuilder   strb;

            strb    = new StringBuilder(128);
            strb.Append(pgnGame.WhitePlayer ?? "???");
            strb.Append(" against ");
            strb.Append(pgnGame.BlackPlayer ?? "???");
            strb.Append(" (");
            strb.Append((pgnGame.WhiteELO  == -1) ? "-" : pgnGame.WhiteELO.ToString());
            strb.Append("/");
            strb.Append((pgnGame.BlackELO  == -1) ? "-" : pgnGame.BlackELO.ToString());
            strb.Append(") played on ");
            strb.Append(pgnGame.Date ?? "???");
            strb.Append(". Result is ");
            strb.Append(pgnGame.GameResult ?? "???");
            return(strb.ToString());
        }

        /// <summary>
        /// Initialize the form with the content of the PGN file
        /// </summary>
        /// <param name="strFileName">  PGN file name</param>
        /// <returns>
        /// true if at least one game has been found.
        /// </returns>
        public bool InitForm(string strFileName) {
            bool    bRetVal;
            int     iIndex;
            string  strDesc;
            int     iSkippedCount;

            bRetVal = m_pgnParser.InitFromFile(strFileName);
            if (bRetVal) {
                m_pgnGames = m_pgnParser.GetAllRawPGN(true /*bAttrList*/, false /*bMoveList*/, out iSkippedCount);
                if (m_pgnGames.Count < 1) {
                    MessageBox.Show("No games found in the PGN File '" + strFileName + "'");
                    bRetVal = false;
                } else {
                    iIndex  = 0;
                    foreach (PgnGame pgnGame in m_pgnGames) {
                        strDesc =   (iIndex + 1).ToString().PadLeft(5, '0') + " - " + GetGameDesc(pgnGame);
                        listBoxGames.Items.Add(new PGNGameDescItem(strDesc, iIndex));
                        iIndex++;
                    }
                    listBoxGames.SelectedIndex = 0;
                    bRetVal                    = true;
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Called when a game is selected
        /// </summary>
        /// <param name="bNoMove">  true to ignore the move list</param>
        private void GameSelected(bool bNoMove) {
            string              strGame;
            PgnParser           parser;
            PgnGame             pgnGame;
            List<MoveExt>       listGame;
            string              strError;
            int                 iSkip;
            int                 iTruncated;
            
            strGame = GetSelectedGame();
            if (strGame != null) {
                listGame    = new List<MoveExt>(256);
                parser      = new PgnParser(false);
                parser.InitFromString(strGame);
                if (!parser.ParseSingle(bNoMove,
                                        out iSkip,
                                        out iTruncated,
                                        out pgnGame,
                                        out strError)) {
                    MessageBox.Show("The specified board is invalid - " + (strError ?? ""));
                } else if (iSkip != 0) {
                    MessageBox.Show("The game is incomplete. Select another game.");
                } else if (iTruncated != 0) {
                    MessageBox.Show("The selected game includes an unsupported pawn promotion (only pawn promotion to queen is supported).");
                } else if (pgnGame.MoveExtList.Count == 0 && pgnGame.StartingChessBoard == null) {
                    MessageBox.Show("Game is empty.");
                } else {
                    StartingChessBoard  = pgnGame.StartingChessBoard;
                    StartingColor       = pgnGame.StartingColor;
                    WhitePlayerName     = pgnGame.WhitePlayer;
                    BlackPlayerName     = pgnGame.BlackPlayer;
                    WhitePlayerType     = pgnGame.WhiteType;
                    BlackPlayerType     = pgnGame.BlackType;
                    WhiteTimer          = pgnGame.WhiteSpan;
                    BlackTimer          = pgnGame.BlackSpan;
                    MoveList            = pgnGame.MoveExtList;
                    DialogResult        = true;
                    Close();
                }
            }
        }

        /// <summary>
        /// Accept the content of the form
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void Button_Click(object sender, RoutedEventArgs e) {
            GameSelected(false /*bNoMove*/);
        }

        /// <summary>
        /// Accept the content of the form (but no move)
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void Button_Click_1(object sender, RoutedEventArgs e) {
            GameSelected(true /*bNoMove*/);
        }

        /// <summary>
        /// Called when the game selection is changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void listBoxGames_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RefreshGameDisplay();
        }
    } // Class frmPgnGamePicker
} // Namespace
