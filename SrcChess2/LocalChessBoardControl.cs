using System;
using System.Collections.Generic;
using System.IO;

namespace SrcChess2 {
    /// <summary>
    /// Override chess control to add information to the saved board
    /// </summary>
    internal class LocalChessBoardControl : ChessBoardControl {
        /// <summary>Father Window</summary>
        public  MainWindow  Father { get; set; }

        /// <summary>
        /// Class constructor
        /// </summary>
        public LocalChessBoardControl() : base() {
        }

        /// <summary>
        /// Load the game board
        /// </summary>
        /// <param name="reader">   Binary reader</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public override bool LoadGame(BinaryReader reader) {
            bool                        bRetVal;
            string                      strVersion;
            MainWindow.PlayingModeE     ePlayingMode;
                
            strVersion = reader.ReadString();
            if (strVersion == "SRCCHESS095") {
                bRetVal = base.LoadGame(reader);
                if (bRetVal) {
                    ePlayingMode            = (MainWindow.PlayingModeE)reader.ReadInt32();
                    Father.PlayingMode = ePlayingMode;
                } else {
                    bRetVal = false;
                }
            } else {
                bRetVal = false;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Save the game board
        /// </summary>
        /// <param name="writer">   Binary writer</param>
        public override void SaveGame(BinaryWriter writer) {
            writer.Write("SRCCHESS095");
            base.SaveGame(writer);
            writer.Write((int)Father.PlayingMode);
        }

        /// <summary>
        /// Create a new game using the specified list of moves
        /// </summary>
        /// <param name="chessBoardStarting">   Starting board or null if standard board</param>
        /// <param name="listMove">             List of moves</param>
        /// <param name="eNextMoveColor">       Color starting to play</param>
        /// <param name="strWhitePlayerName">   Name of the player playing white pieces</param>
        /// <param name="strBlackPlayerName">   Name of the player playing black pieces</param>
        /// <param name="eWhitePlayerType">     Type of player playing white pieces</param>
        /// <param name="eBlackPlayerType">     Type of player playing black pieces</param>
        /// <param name="spanPlayerWhite">      Timer for white</param>
        /// <param name="spanPlayerBlack">      Timer for black</param>
        public override void CreateGameFromMove(ChessBoard          chessBoardStarting,
                                                List<MoveExt>       listMove,
                                                ChessBoard.PlayerE  eNextMoveColor,
                                                string              strWhitePlayerName,
                                                string              strBlackPlayerName,
                                                PlayerTypeE         eWhitePlayerType,
                                                PlayerTypeE         eBlackPlayerType,
                                                TimeSpan            spanPlayerWhite,
                                                TimeSpan            spanPlayerBlack) {
            base.CreateGameFromMove(chessBoardStarting,
                                    listMove,
                                    eNextMoveColor,
                                    strWhitePlayerName,
                                    strBlackPlayerName,
                                    eWhitePlayerType,
                                    eBlackPlayerType,
                                    spanPlayerWhite,
                                    spanPlayerBlack);
            if (eWhitePlayerType == PlayerTypeE.Program) {
                if (eBlackPlayerType == PlayerTypeE.Program) {
                    Father.PlayingMode  = MainWindow.PlayingModeE.ComputerPlayBoth;
                } else {
                    Father.PlayingMode  = MainWindow.PlayingModeE.ComputerPlayWhite;
                }
            } else if (eBlackPlayerType == PlayerTypeE.Program) {
                Father.PlayingMode  = MainWindow.PlayingModeE.ComputerPlayBlack;
            } else {
                Father.PlayingMode  = MainWindow.PlayingModeE.PlayerAgainstPlayer;
            }
            Father.SetCmdState();
        }
    }
}
