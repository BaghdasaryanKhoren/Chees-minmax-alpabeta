using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SrcChess2 {
    /// <summary>
    /// Utility class to help handling PGN files. Help filtering PGN files or creating one from an existing board
    /// </summary>
    public class PgnUtil {

        /// <summary>
        /// Used when creating a PGN move
        /// </summary>
        [Flags]
        private enum PGNAmbiguity {
            /// <summary>No ambiguity in the move. Can use short notation</summary>
            NotFound            = 0,
            /// <summary>An ambiguity has been found. More than one move can be found if using short notation</summary>
            Found               = 1,
            /// <summary>Column must be specified to remove ambiguity</summary>
            ColMustBeSpecify    = 2,
            /// <summary>Row must be specified to remove ambiguity</summary>
            RowMustBeSpecify    = 4
        }
        
        /// <summary>Information use to filter a PGN file</summary>
        public class FilterClause {
            /// <summary>All ELO rating included if true</summary>
            public  bool                        m_bAllRanges;
            /// <summary>Includes unrated games if true</summary>
            public  bool                        m_bIncludesUnrated;
            /// <summary>If not all ELO rating included, hash of all ELO which must be included. Each value represent a range (value, value+99)</summary>
            public  Dictionary<int, int>        m_hashRanges;
            /// <summary>All players included if true</summary>
            public  bool                        m_bAllPlayers;
            /// <summary>Hash of all players to include if not all included</summary>
            public  Dictionary<string,string>   m_hashPlayerList;
            /// <summary>Includes all ending if true</summary>
            public  bool                        m_bAllEnding;
            /// <summary>true to include game winned by white player</summary>
            public  bool                        m_bEndingWhiteWinning;
            /// <summary>true to include game winned by black player</summary>
            public  bool                        m_bEndingBlackWinning;
            /// <summary>true to include draws game </summary>
            public  bool                        m_bEndingDraws;
        }
        
        /// <summary>
        /// Open an file for reading
        /// </summary>
        /// <param name="strInpFileName">   File name to open</param>
        /// <returns>
        /// Stream or null if unable to open the file.
        /// </returns>
        public static Stream OpenInpFile(string strInpFileName) {
            Stream  streamInp;
            
            try {
                streamInp = File.OpenRead(strInpFileName);
            } catch(System.Exception) {
                MessageBox.Show("Unable to open the file - " + strInpFileName);
                streamInp = null;
            }
            return(streamInp);
        }

        /// <summary>
        /// Creates a new file
        /// </summary>
        /// <param name="strOutFileName">   Name of the file to create</param>
        /// <returns>
        /// Stream or null if unable to create the file.
        /// </returns>
        public static StreamWriter CreateOutFile(string strOutFileName) {
            StreamWriter    streamRetVal;
            Stream          streamOut;
            
            try {
                streamOut       = File.Create(strOutFileName);
                streamRetVal    = new StreamWriter(streamOut, Encoding.GetEncoding(1252));
            } catch(System.Exception) {
                MessageBox.Show("Unable to create the file - " + strOutFileName);
                streamRetVal = null;
            }
            return(streamRetVal);
        }

        /// <summary>
        /// Write a PGN game in the specified output stream
        /// </summary>
        /// <param name="pgnBuffer">    PGN buffer</param>
        /// <param name="writer">       Text writer</param>
        /// <param name="pgnGame">      PGN game</param>
        private void WritePGN(PgnLexical pgnBuffer, TextWriter writer, PgnGame pgnGame) {
            writer.Write(pgnBuffer.GetStringAtPos(pgnGame.StartingPos, pgnGame.Length));
        }

        /// <summary>
        /// Gets the information about a PGN game
        /// </summary>
        /// <param name="rawGame">          Raw PGN game</param>
        /// <param name="strGameResult">    Result of the game</param>
        /// <param name="strGameDate">      Date of the game</param>
        private static void GetPGNGameInfo(PgnGame   rawGame,
                                           out string   strGameResult,
                                           out string   strGameDate) {
            if (!rawGame.attrs.TryGetValue("Result", out strGameResult)) {
                strGameResult = null;
            }
            if (!rawGame.attrs.TryGetValue("Date", out strGameDate)) {
                strGameDate = null;
            }
        }

        /// <summary>
        /// Scan the PGN stream to retrieve some informations
        /// </summary>
        /// <param name="pgnGames">         PGN games</param>
        /// <param name="setPlayerList">    Set to be filled with the players list</param>
        /// <param name="iMinELO">          Minimum ELO found in the games</param>
        /// <param name="iMaxELO">          Maximum ELO found in the games</param>
        /// <returns>
        /// List of raw games without the move list
        /// </returns>
        public void FillFilterList(List<PgnGame> pgnGames, HashSet<String> setPlayerList, ref int iMinELO, ref int iMaxELO) {
            int     iAvgELO;
            string  strPlayer;
            
            foreach (PgnGame pgnGame in pgnGames) {
                if (setPlayerList != null) {
                    strPlayer = pgnGame.WhitePlayer;
                    if (strPlayer != null && !setPlayerList.Contains(strPlayer)) {
                        setPlayerList.Add(strPlayer);
                    }
                    strPlayer = pgnGame.BlackPlayer;
                    if (strPlayer != null && !setPlayerList.Contains(strPlayer)) {
                        setPlayerList.Add(strPlayer);
                    }
                }
                if (pgnGame.WhiteELO != -1 && pgnGame.BlackELO != -1) {
                    iAvgELO = (pgnGame.WhiteELO + pgnGame.BlackELO) / 2;
                    if (iAvgELO > iMaxELO) {
                        iMaxELO = iAvgELO;
                    }
                    if (iAvgELO < iMinELO) {
                        iMinELO = iAvgELO;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the specified game must be retained accordingly to the specified filter
        /// </summary>
        /// <param name="rawGame">          PGN Raw game</param>
        /// <param name="iAvgELO">          Game average ELO</param>
        /// <param name="filterClause">     Filter clause</param>
        /// <returns>
        /// true if must be retained
        /// </returns>
        private bool IsRetained(PgnGame rawGame, int iAvgELO, FilterClause filterClause) {
            bool    bRetVal = true;
            string  strGameResult;
            string  strGameDate;
            
            if (iAvgELO == -1) {
                bRetVal = filterClause.m_bIncludesUnrated;
            } else if (filterClause.m_bAllRanges) {
                bRetVal = true;
            } else {
                iAvgELO = iAvgELO / 100 * 100;
                bRetVal = filterClause.m_hashRanges.ContainsKey(iAvgELO);
            }
            if (bRetVal) {
                if (!filterClause.m_bAllPlayers || !filterClause.m_bAllEnding) {
                    GetPGNGameInfo(rawGame, out strGameResult,out strGameDate);
                    if (!filterClause.m_bAllPlayers) {
                        if (!filterClause.m_hashPlayerList.ContainsKey(rawGame.BlackPlayer) &&
                            !filterClause.m_hashPlayerList.ContainsKey(rawGame.WhitePlayer)) {
                            bRetVal = false;
                        }
                    }
                    if (bRetVal && !filterClause.m_bAllEnding) {
                        if (strGameResult == "1-0") {
                            bRetVal = filterClause.m_bEndingWhiteWinning;
                        } else if (strGameResult == "0-1") {
                            bRetVal = filterClause.m_bEndingBlackWinning;
                        } else if (strGameResult == "1/2-1/2") {
                            bRetVal = filterClause.m_bEndingDraws;
                        } else {
                            bRetVal = false;
                        }
                    }
                }                
            }
            return(bRetVal);
        }

        /// <summary>
        /// Filter the content of the PGN file in the input stream to fill the output stream
        /// </summary>
        /// <param name="pgnParser">        PGN parser</param>
        /// <param name="rawGames">         List of PGN raw games without move list</param>
        /// <param name="textWriter">       Output stream. If null, just run to determine the result count.</param>
        /// <param name="filterClause">     Filter clause</param>
        /// <returns>
        /// Number of resulting games.
        /// </returns>
        public int FilterPGN(PgnParser pgnParser, List<PgnGame> rawGames, TextWriter textWriter, FilterClause filterClause) {
            int             iRetVal;
            int             iWhiteELO;
            int             iBlackELO;
            int             iAvgELO;
            
            iRetVal = 0;
            try {
                foreach (PgnGame rawGame in rawGames) {
                    iWhiteELO   = rawGame.WhiteELO;
                    iBlackELO   = rawGame.BlackELO;
                    iAvgELO     = (iWhiteELO != -1 && iBlackELO != -1) ? (iWhiteELO + iBlackELO) / 2 : -1;
                    if (IsRetained(rawGame, iAvgELO, filterClause)) {
                        if (textWriter != null) {
                            WritePGN(pgnParser.PGNLexical, textWriter, rawGame);
                        }
                        iRetVal++;
                    }
                }
                if (textWriter != null) {
                    textWriter.Flush();
                }
            } catch(System.Exception exc) {
                MessageBox.Show("Error writing in destination file.\r\n" + exc.Message);
                iRetVal = 0;
            }
            return(iRetVal);
        }

        /// <summary>
        /// Creates a PGN file as a subset of an existing one.
        /// </summary>
        /// <param name="pgnParser">    PGN parser</param>
        /// <param name="pgnGames">     Source PGN games</param>
        /// <param name="filterClause"> Filter clause</param>
        public void CreateSubsetPGN(PgnParser       pgnParser,
                                    List<PgnGame>   pgnGames,
                                    FilterClause    filterClause) {
            SaveFileDialog              saveDlg;
            StreamWriter                streamWriter;
            int                         iCount;
            
            saveDlg                     = new SaveFileDialog();
            saveDlg.AddExtension        = true;
            saveDlg.CheckPathExists     = true;
            saveDlg.DefaultExt          = "pgn";
            saveDlg.Filter              = "Chess PGN Files (*.pgn)|*.pgn";
            saveDlg.OverwritePrompt     = true;
            saveDlg.Title               = "PGN File to Create";
            if (saveDlg.ShowDialog() == true) {
                streamWriter = CreateOutFile(saveDlg.FileName);
                if (streamWriter != null) {
                    using(streamWriter) {
                        iCount = FilterPGN(pgnParser,
                                           pgnGames,
                                           streamWriter,
                                           filterClause);
                        MessageBox.Show("The file '" + saveDlg.FileName + "' has been created with " + iCount.ToString() + " game(s)");
                    }
                }
            }
        }

        /// <summary>
        /// Creates one or many PGN files as a subset of an existing one.
        /// </summary>
        /// <param name="wndParent">    Parent window</param>
        public void CreatePGNSubsets(Window wndParent) {
            OpenFileDialog              openDlg;
            List<PgnGame>               pgnGames;
            PgnParser                   pgnParser;
            HashSet<string>             setPlayer;
            String[]                    arrPlayer;
            int                         iMinELO;
            int                         iMaxELO;
            frmPgnFilter                frmPgnFilter;
            frmLoadPGNGames             frmLoadPGNGames;
            
            openDlg = new OpenFileDialog();
            openDlg.AddExtension        = true;
            openDlg.CheckFileExists     = true;
            openDlg.CheckPathExists     = true;
            openDlg.DefaultExt          = "pgn";
            openDlg.Filter              = "Chess PGN Files (*.pgn)|*.pgn";
            openDlg.Multiselect         = false;
            openDlg.Title               = "Open Source PGN File"; 
            if (openDlg.ShowDialog() == true) {
                setPlayer               = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                iMinELO                 = Int32.MaxValue;
                iMaxELO                 = Int32.MinValue;
                frmLoadPGNGames         = new frmLoadPGNGames(openDlg.FileName);
                frmLoadPGNGames.Owner   = wndParent;
                if (frmLoadPGNGames.ShowDialog() == true) {
                    pgnGames    = frmLoadPGNGames.PGNGames;
                    pgnParser   = frmLoadPGNGames.PGNParser;
                    if (pgnGames.Count == 0) {
                        MessageBox.Show("No games found in the file.");
                    } else {
                        FillFilterList(pgnGames, setPlayer, ref iMinELO, ref iMaxELO);
                        arrPlayer = new string[setPlayer.Count];
                        setPlayer.CopyTo(arrPlayer, 0);
                        Array.Sort(arrPlayer);
                        frmPgnFilter = new frmPgnFilter(pgnParser,
                                                        this,
                                                        pgnGames,
                                                        iMinELO,
                                                        iMaxELO,
                                                        arrPlayer,
                                                        openDlg.FileName);
                        frmPgnFilter.ShowDialog();
                    }
                }
            }
        }

        /// <summary>
        /// Gets Square Id from the PGN representation
        /// </summary>
        /// <param name="strMove">  PGN square representation.</param>
        /// <returns>
        /// square id (0-63)
        /// PGN representation
        /// </returns>
        public static int GetSquareIDFromPGN(string strMove) {
            int     iRetVal;
            Char    cChr1;
            Char    cChr2;
            
            if (strMove.Length != 2) {
                iRetVal = -1;
            } else {
                cChr1 = strMove.ToLower()[0];
                cChr2 = strMove[1];
                if (cChr1 < 'a' || cChr1 > 'h' || cChr2 < '1' || cChr2 > '8') {
                    iRetVal = -1;
                } else {
                    iRetVal = 7 - (cChr1 - 'a') + ((cChr2 - '0') << 3);
                }
            }            
            return(iRetVal);
        }

        /// <summary>
        /// Gets the PGN representation of a square
        /// </summary>
        /// <param name="iPos">         Absolute position of the square.</param>
        /// <returns>
        /// PGN representation
        /// </returns>
        public static string GetPGNSquareID(int iPos) {
            string  strRetVal;
            
            strRetVal = ((Char)('a' + 7 - (iPos & 7))).ToString() + ((Char)((iPos >> 3) + '1')).ToString();
            return(strRetVal);
        }

        /// <summary>
        /// Find all moves which end to the same position which can create ambiguity
        /// </summary>
        /// <param name="chessBoard">   Chessboard before the move has been done.</param>
        /// <param name="move">         Move to convert</param>
        /// <param name="eMovePlayer">  Player making the move</param>
        /// <returns>
        /// PGN move
        /// </returns>
        private static PGNAmbiguity FindMoveAmbiguity(ChessBoard chessBoard, Move move, ChessBoard.PlayerE eMovePlayer) {
            PGNAmbiguity        eRetVal = PGNAmbiguity.NotFound;
            ChessBoard.PieceE   ePieceMove;
            List<Move>          moveList;
            
            moveList   = chessBoard.EnumMoveList(eMovePlayer);
            ePieceMove = chessBoard[move.StartPos];
            foreach (Move moveTest in moveList) {
                if (moveTest.EndPos == move.EndPos) {
                    if (moveTest.StartPos == move.StartPos) {
                        if (moveTest.Type == move.Type) {
                            eRetVal |= PGNAmbiguity.Found;
                        }
                    } else {
                        if (chessBoard[moveTest.StartPos] == ePieceMove) {
                            if ((moveTest.StartPos & 7) != (move.StartPos & 7)) {
                                eRetVal |= PGNAmbiguity.ColMustBeSpecify;
                            } else {
                                eRetVal |= PGNAmbiguity.RowMustBeSpecify;
                            }
                        }
                    }
                }
            }
            return(eRetVal);
        }

        /// <summary>
        /// Gets a PGN move from a MovePosS structure and a chessboard.
        /// </summary>
        /// <param name="chessBoard">       Chessboard before the move has been done.</param>
        /// <param name="move">             Move to convert</param>
        /// <param name="bIncludeEnding">   true to include ending</param>
        /// <returns>
        /// PGN move
        /// </returns>
        public static string GetPGNMoveFromMove(ChessBoard chessBoard, MoveExt move, bool bIncludeEnding) {
            string              strRetVal;
            string              strStartPos;
            ChessBoard.PieceE   ePiece;
            PGNAmbiguity        eAmbiguity;
            ChessBoard.PlayerE  ePlayerToMove;
            
            if (move.Move.Type == Move.TypeE.Castle) {
                strRetVal = (move.Move.EndPos == 1 || move.Move.EndPos == 57) ? "O-O" : "O-O-O";
            } else {
                ePiece          = chessBoard[move.Move.StartPos] & ChessBoard.PieceE.PieceMask;
                ePlayerToMove   = chessBoard.CurrentPlayer;
                eAmbiguity      = FindMoveAmbiguity(chessBoard, move.Move, ePlayerToMove);
                switch(ePiece) {
                case ChessBoard.PieceE.King:
                    strRetVal = "K";
                    break;
                case ChessBoard.PieceE.Queen:
                    strRetVal = "Q";
                    break;
                case ChessBoard.PieceE.Rook:
                    strRetVal = "R";
                    break;
                case ChessBoard.PieceE.Bishop:
                    strRetVal = "B";
                    break;
                case ChessBoard.PieceE.Knight:
                    strRetVal = "N";
                    break;
                case ChessBoard.PieceE.Pawn:
                    strRetVal = "";
                    break;
                default:
                    strRetVal = "";
                    break;
                }
                strStartPos = GetPGNSquareID(move.Move.StartPos);
                if ((eAmbiguity & PGNAmbiguity.ColMustBeSpecify) == PGNAmbiguity.ColMustBeSpecify) {
                    strRetVal += strStartPos[0];
                }
                if ((eAmbiguity & PGNAmbiguity.RowMustBeSpecify) == PGNAmbiguity.RowMustBeSpecify) {
                    strRetVal += strStartPos[1];
                }
                if ((move.Move.Type & Move.TypeE.PieceEaten) == Move.TypeE.PieceEaten) {
                    if (ePiece == ChessBoard.PieceE.Pawn && 
                        (eAmbiguity & PGNAmbiguity.ColMustBeSpecify) == (PGNAmbiguity)0 &&
                        (eAmbiguity & PGNAmbiguity.RowMustBeSpecify) == (PGNAmbiguity)0) {
                        strRetVal += strStartPos[0];
                    }
                    strRetVal += 'x';
                }
                strRetVal += GetPGNSquareID(move.Move.EndPos);
                switch(move.Move.Type & Move.TypeE.MoveTypeMask) {
                case Move.TypeE.PawnPromotionToQueen:
                    strRetVal += "=Q";
                    break;
                case Move.TypeE.PawnPromotionToRook:
                    strRetVal += "=R";
                    break;
                case Move.TypeE.PawnPromotionToBishop:
                    strRetVal += "=B";
                    break;
                case Move.TypeE.PawnPromotionToKnight:
                    strRetVal += "=N";
                    break;
                case Move.TypeE.PawnPromotionToPawn:
                    strRetVal += "=P";
                    break;
                default:
                    break;
                }
            }
            chessBoard.DoMoveNoLog(move.Move);
            switch(chessBoard.GetCurrentResult()) {
            case ChessBoard.GameResultE.OnGoing:
                break;
            case ChessBoard.GameResultE.Check:
                strRetVal += "+";
                break;
            case ChessBoard.GameResultE.Mate:
                strRetVal += "#";
                if (bIncludeEnding) {
                    if (chessBoard.CurrentPlayer == ChessBoard.PlayerE.Black) {
                        strRetVal += " 1-0";
                    } else {
                        strRetVal += " 0-1";
                    }
                }
                break;
            case ChessBoard.GameResultE.ThreeFoldRepeat:
            case ChessBoard.GameResultE.FiftyRuleRepeat:
            case ChessBoard.GameResultE.TieNoMove:
            case ChessBoard.GameResultE.TieNoMatePossible:
                if (bIncludeEnding) {
                    strRetVal += " 1/2-1/2";
                }
                break;
            default:
                break;
            }
            chessBoard.UndoMoveNoLog(move.Move);
            return(strRetVal);
        }

        /// <summary>
        /// Generates FEN
        /// </summary>
        /// <param name="chessBoard">       Actual chess board (after the move)</param>
        /// <returns>
        /// PGN representation of the game
        /// </returns>
        public static string GetFENFromBoard(ChessBoard chessBoard) {
            StringBuilder               strBuilder;
            int                         iEmptyCount;
            ChessBoard.PieceE           ePiece;
            Char                        cPiece;
            ChessBoard.PlayerE          eNextMoveColor;
            ChessBoard.BoardStateMaskE  eBoardStateMask;
            int                         iEnPassant;
            int                         iHalfMoveClock;
            int                         iHalfMoveCount;
            int                         iFullMoveCount;
            bool                        bCastling;
            
            strBuilder      = new StringBuilder(512);
            eNextMoveColor  = chessBoard.CurrentPlayer;
            eBoardStateMask = chessBoard.ComputeBoardExtraInfo(eNextMoveColor, false);
            iEnPassant      = (int)(eBoardStateMask & ChessBoard.BoardStateMaskE.EnPassant);
            for (int iRow = 7; iRow >= 0; iRow--) {
                iEmptyCount = 0;
                for (int iCol = 7; iCol >= 0; iCol--) {
                    ePiece = chessBoard[(iRow << 3) + iCol];
                    if (ePiece == ChessBoard.PieceE.None) {
                        iEmptyCount++;
                    } else {
                        if (iEmptyCount != 0) {
                            strBuilder.Append(iEmptyCount.ToString());
                            iEmptyCount = 0;
                        }
                        switch(ePiece & ChessBoard.PieceE.PieceMask) {
                        case ChessBoard.PieceE.King:
                            cPiece = 'K';
                            break;
                        case ChessBoard.PieceE.Queen:
                            cPiece = 'Q';
                            break;
                        case ChessBoard.PieceE.Rook:
                            cPiece = 'R';
                            break;
                        case ChessBoard.PieceE.Bishop:
                            cPiece = 'B';
                            break;
                        case ChessBoard.PieceE.Knight:
                            cPiece = 'N';
                            break;
                        case ChessBoard.PieceE.Pawn:
                            cPiece = 'P';
                            break;
                        default:
                            cPiece = '?';
                            break;
                        }
                        if ((ePiece & ChessBoard.PieceE.Black) == ChessBoard.PieceE.Black) {
                            cPiece = Char.ToLower(cPiece);
                        }
                        strBuilder.Append(cPiece);
                    }
                }
                if (iEmptyCount != 0) {
                    strBuilder.Append(iEmptyCount.ToString());
                }
                if (iRow != 0) {
                    strBuilder.Append('/');
                }
            }
            strBuilder.Append(' ');
            strBuilder.Append((eNextMoveColor == ChessBoard.PlayerE.White) ? 'w' : 'b');
            strBuilder.Append(' ');
            bCastling = false;
            if ((eBoardStateMask & ChessBoard.BoardStateMaskE.WRCastling) == ChessBoard.BoardStateMaskE.WRCastling) {
                strBuilder.Append('K');
                bCastling = true;
            }
            if ((eBoardStateMask & ChessBoard.BoardStateMaskE.WLCastling) == ChessBoard.BoardStateMaskE.WLCastling) {
                strBuilder.Append('Q');
                bCastling = true;
            }
            if ((eBoardStateMask & ChessBoard.BoardStateMaskE.BRCastling) == ChessBoard.BoardStateMaskE.BRCastling) {
                strBuilder.Append('k');
                bCastling = true;
            }
            if ((eBoardStateMask & ChessBoard.BoardStateMaskE.BLCastling) == ChessBoard.BoardStateMaskE.BLCastling) {
                strBuilder.Append('q');
                bCastling = true;
            }
            if (!bCastling) {
                strBuilder.Append('-');
            }
            strBuilder.Append(' ');
            if (iEnPassant == 0) {
                strBuilder.Append('-');
            } else {
                strBuilder.Append(GetPGNSquareID(iEnPassant));
            }
            iHalfMoveClock  = chessBoard.MoveHistory.GetCurrentHalfMoveClock;
            iHalfMoveCount  = chessBoard.MovePosStack.PositionInList + 1;
            iFullMoveCount  = (iHalfMoveCount + 2) / 2;
            strBuilder.Append(" " + iHalfMoveClock.ToString() + " " + iFullMoveCount.ToString());
            return(strBuilder.ToString());
        }

        /// <summary>
        /// Generates the PGN representation of the board
        /// </summary>
        /// <param name="chessBoard">       Actual chess board (after the move)</param>
        /// <param name="bIncludeRedoMove"> true to include redo move</param>
        /// <param name="strEvent">         Event tag</param>
        /// <param name="strSite">          Site tag</param>
        /// <param name="strDate">          Date tag</param>
        /// <param name="strRound">         Round tag</param>
        /// <param name="strWhitePlayer">   White player's name</param>
        /// <param name="strBlackPlayer">   Black player's name</param>
        /// <param name="eWhitePlayerType"> White player's type</param>
        /// <param name="eBlackPlayerType"> Black player's type</param>
        /// <param name="spanWhitePlayer">  Timer for the white</param>
        /// <param name="spanBlackPlayer">  Timer for the black</param>
        /// <returns>
        /// PGN representation of the game
        /// </returns>
        public static string GetPGNFromBoard(ChessBoard     chessBoard,
                                             bool           bIncludeRedoMove,
                                             string         strEvent,
                                             string         strSite,
                                             string         strDate,
                                             string         strRound,
                                             string         strWhitePlayer,
                                             string         strBlackPlayer,
                                             PlayerTypeE    eWhitePlayerType,
                                             PlayerTypeE    eBlackPlayerType,
                                             TimeSpan       spanWhitePlayer,
                                             TimeSpan       spanBlackPlayer) {
            int             iMoveIndex;
            StringBuilder   strBuilder;
            StringBuilder   strBuilderLine;
            int             iOriIndex;
            int             iMoveCount;
            MovePosStack    movePosStack;
            MoveExt         move;
            string          strResult;
            
            movePosStack    = chessBoard.MovePosStack;
            iOriIndex       = movePosStack.PositionInList;
            iMoveCount      = (bIncludeRedoMove) ? movePosStack.Count : iOriIndex + 1;
            strBuilder      = new StringBuilder(10 * iMoveCount + 256);
            strBuilderLine  = new StringBuilder(256);
            switch(chessBoard.GetCurrentResult()) {
            case ChessBoard.GameResultE.Check:
            case ChessBoard.GameResultE.OnGoing:
                strResult = "*";
                break;
            case ChessBoard.GameResultE.Mate:
                strResult = (chessBoard.CurrentPlayer == ChessBoard.PlayerE.White) ? "0-1" : "1-0";
                break;
            case ChessBoard.GameResultE.FiftyRuleRepeat:
            case ChessBoard.GameResultE.ThreeFoldRepeat:
            case ChessBoard.GameResultE.TieNoMove:
            case ChessBoard.GameResultE.TieNoMatePossible:
                strResult = "1/2-1/2";
                break;
            default:
                strResult = "*";
                break;
            }
            chessBoard.UndoAllMoves();
            strBuilder.Append("[Event \"" + strEvent + "\"]\n");
            strBuilder.Append("[Site \"" + strSite + "\"]\n");
            strBuilder.Append("[Date \"" + strDate + "\"]\n");
            strBuilder.Append("[Round \"" + strRound + "\"]\n");
            strBuilder.Append("[White \"" + strWhitePlayer + "\"]\n");
            strBuilder.Append("[Black \"" + strBlackPlayer + "\"]\n");
            strBuilder.Append("[Result \"" + strResult + "\"]\n");
            if (!chessBoard.StandardInitialBoard) {
                strBuilder.Append("[SetUp \"1\"]\n");
                strBuilder.Append("[FEN \"" + GetFENFromBoard(chessBoard) + "\"]\n");
            }
            strBuilder.Append("[WhiteType \"" + ((eWhitePlayerType == PlayerTypeE.Human) ? "human" : "program") + "\"]\n");
            strBuilder.Append("[BlackType \"" + ((eBlackPlayerType == PlayerTypeE.Human) ? "human" : "program") + "\"]\n");
            strBuilder.Append("[TimeControl \"?:" + spanWhitePlayer.Ticks.ToString() + ":" + spanBlackPlayer.Ticks.ToString() + "\"]\n");
            strBuilder.Append('\n');
            iMoveIndex              = 0;
            strBuilderLine.Length   = 0;
            for (iMoveIndex = 0; iMoveIndex < iMoveCount; iMoveIndex++) {
                if (strBuilderLine.Length > 60) {
                    strBuilder.Append(strBuilderLine);
                    strBuilder.Append("\n");
                    strBuilderLine.Length = 0;
                }
                move = movePosStack[iMoveIndex];
                if ((iMoveIndex & 1) == 0) {
                    strBuilderLine.Append(((iMoveIndex + 1) / 2 + 1).ToString());
                    strBuilderLine.Append(". ");
                }
                strBuilderLine.Append(GetPGNMoveFromMove(chessBoard, move, true) + " ");
                chessBoard.RedoMove();
            }
            strBuilder.Append(strBuilderLine);
            strBuilder.Append('\n');
            return(strBuilder.ToString());
        }

        /// <summary>
        /// Generates the PGN representation of a series of moves
        /// </summary>
        /// <param name="chessBoard">   Actual chess board.</param>
        /// <returns>
        /// PGN representation of the game
        /// </returns>
        public static string[] GetPGNArrayFromMoveList(ChessBoard chessBoard) {
            string[]        arrRetVal;
            int             iOriPos;
            int             iMoveIndex;
            MovePosStack    moveStack;
            
            iOriPos      = chessBoard.MovePosStack.PositionInList;
            chessBoard.UndoAllMoves();
            moveStack    = chessBoard.MovePosStack;
            arrRetVal    = new string[moveStack.Count];
            iMoveIndex   = 0;
            foreach (MoveExt move in moveStack.List) {
                arrRetVal[iMoveIndex++] = GetPGNMoveFromMove(chessBoard, move, false);
                chessBoard.RedoMove();
            }
            chessBoard.SetUndoRedoPosition(iOriPos);
            return(arrRetVal);
        }
    } // Class PgnUtil
} // Namespace
