using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmCreatePgnGame.xaml
    /// </summary>
    public partial class frmCreatePgnGame : Window {
        /// <summary>Array of move list</summary>
        public List<MoveExt>                MoveList { get; private set; }
        /// <summary>Board starting position</summary>
        public ChessBoard                   StartingChessBoard { get; private set; }
        /// <summary>Starting Color</summary>
        public ChessBoard.PlayerE           StartingColor { get; private set; }
        /// <summary>Name of the player playing white</summary>
        public string                       WhitePlayerName { get; private set; }
        /// <summary>Name of the player playing black</summary>
        public string                       BlackPlayerName { get; private set; }
        /// <summary>Player type (computer or human)</summary>
        public PlayerTypeE                  WhitePlayerType { get; private set; }
        /// <summary>Player type (computer or human)</summary>
        public PlayerTypeE                  BlackPlayerType { get; private set; }
        /// <summary>White player playing time</summary>
        public TimeSpan                     WhiteTimer { get; private set; }
        /// <summary>Black player playing time</summary>
        public TimeSpan                     BlackTimer { get; private set; }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public frmCreatePgnGame() {
            InitializeComponent();
            StartingColor   = ChessBoard.PlayerE.White;
        }

        /// <summary>
        /// Accept the content of the form
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            string              strGame;
            string              strError;
            PgnParser           parser;
            PgnGame             pgnGame;
            int                 iSkip;
            int                 iTruncated;
            
            strGame = textBox1.Text;
            if (String.IsNullOrEmpty(strGame)) {
                MessageBox.Show("No PGN text has been pasted.");
            } else {
                parser      = new PgnParser(false);
                parser.InitFromString(strGame);
                if (!parser.ParseSingle(false /*bIgnoreMoveListIfFen*/,
                                        out iSkip,
                                        out iTruncated,
                                        out pgnGame,
                                        out strError)) {
                    MessageBox.Show("The specified board is invalid - " + (strError ?? ""));
                } else if (iSkip != 0) {
                    MessageBox.Show("The game is incomplete. Paste another game.");
                } else if (iTruncated != 0) {
                    MessageBox.Show("The selected game includes an unsupported pawn promotion (only pawn promotion to queen is supported).");
                } else if (pgnGame.MoveExtList.Count == 0 && pgnGame.StartingChessBoard == null) {
                    MessageBox.Show("Game is empty.");
                } else {
                    MoveList            = pgnGame.MoveExtList;
                    StartingChessBoard  = pgnGame.StartingChessBoard;
                    StartingColor       = pgnGame.StartingColor;
                    WhitePlayerName     = pgnGame.WhitePlayer;
                    BlackPlayerName     = pgnGame.BlackPlayer;
                    WhitePlayerType     = pgnGame.WhiteType;
                    BlackPlayerType     = pgnGame.BlackType;
                    WhiteTimer          = pgnGame.WhiteSpan;
                    BlackTimer          = pgnGame.BlackSpan;
                    DialogResult        = true;
                    Close();
                }
            }
        }
    } // Class frmCreatePgnGame
} // Namespace
