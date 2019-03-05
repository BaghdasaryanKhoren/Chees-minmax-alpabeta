using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace SrcChess2 {
    //
    //  PGN BNF
    //
    //  <PGN-database>              ::= {<PGN-game>}
    //  <PGN-game>                  ::= <tag-section> <movetext-section>
    //  <tag-section>               ::= {<tag-pair>}
    //  <tag-pair>                  ::= '[' <tag-name> <tag-value> ']'
    //  <tag-name>                  ::= <identifier>
    //  <tag-value>                 ::= <string>
    //  <movetext-section>          ::= <element-sequence> <game-termination>
    //  <element-sequence>          ::= {<element>}
    //  <element>                   ::= <move-number-indication> | <SAN-move> | <numeric-annotation-glyph>
    //  <move-number-indication>    ::= Integer {'.'}
    //  <recursive-variation>       ::= '(' <element-sequence> ')'
    //  <game-termination>          ::= '1-0' | '0-1' | '1/2-1/2' | '*'

    /// <summary>Parser exception</summary>
    [Serializable]
    public class PgnParserException : System.Exception {
        /// <summary>Code which is in error</summary>
        public string   CodeInError;
        /// <summary>Array of move position</summary>
        public short[]  MoveList;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="strMsg">           Error Message</param>
        /// <param name="strCodeInError">   Code in error</param>
        /// <param name="ex">               Inner exception</param>
        public          PgnParserException(string strMsg, string strCodeInError, Exception ex) : base(strMsg, ex) { CodeInError = strCodeInError; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="strMsg">           Error Message</param>
        /// <param name="strCodeInError">   Code in error</param>
        public          PgnParserException(string strMsg, string strCodeInError) : this(strMsg, strCodeInError, null) {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="strMsg">           Error Message</param>
        public          PgnParserException(string strMsg) : this(strMsg, "", null) {}

        /// <summary>
        /// Constructor
        /// </summary>
        public          PgnParserException() : this("", "", null) {}

        /// <summary>
        /// Unserialize additional data
        /// </summary>
        /// <param name="info">     Serialization Info</param>
        /// <param name="context">  Context Info</param>
        protected PgnParserException(SerializationInfo info, StreamingContext context) : base(info, context) {
            CodeInError = info.GetString("CodeInError");
            MoveList    = (short[])info.GetValue("MoveList", typeof(short[]));
        }

        /// <summary>
        /// Serialize the additional data
        /// </summary>
        /// <param name="info">     Serialization Info</param>
        /// <param name="context">  Context Info</param>
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) {
            base.GetObjectData(info, context);
            info.AddValue("CodeInError",    CodeInError);
            info.AddValue("MoveList",       MoveList);
        }
    } // Class PgnParserException

    /// <summary>Class implementing the parsing of a PGN file. PGN is a standard way of recording chess games.</summary>
    public class PgnParser {

        /// <summary>
        /// Parsing Phase
        /// </summary>
        public enum ParsingPhaseE {
            /// <summary>No phase set yet</summary>
            None            = 0,
            /// <summary>Openning a file</summary>
            OpeningFile     = 1,
            /// <summary>Reading the file content into memory</summary>
            ReadingFile     = 2,
            /// <summary>Raw parsing the PGN file</summary>
            RawParsing      = 3,
            /// <summary>Creating the book</summary>
            CreatingBook    = 10,
            /// <summary>Processing is finished</summary>
            Finished        = 255
        }

        /// <summary>true to cancel the parsing job</summary>
        private static bool     m_bJobCancelled;
        /// <summary>Board use to play as we decode</summary>
        private ChessBoard      m_chessBoard;
        /// <summary>true to diagnose the parser. This generate exception when a move cannot be resolved</summary>
        private bool            m_bDiagnose;
        /// <summary>PGN Lexical Analyser</summary>
        private PgnLexical      m_pgnLexical;

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="bDiagnose">    true to diagnose the parser</param>
        public PgnParser(bool bDiagnose) {
            m_chessBoard    = new ChessBoard();
            m_bDiagnose     = bDiagnose;
            m_pgnLexical    = null;
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="chessBoard">   Chessboard to use</param>
        public PgnParser(ChessBoard chessBoard) {
            m_chessBoard    = chessBoard;
            m_bDiagnose     = false;
            m_pgnLexical    = null;
        }

        /// <summary>
        /// Initialize the parser using the content of a PGN file
        /// </summary>
        /// <param name="strFileName">  File name</param>
        /// <returns>true if succeed, false if failed</returns>
        public bool InitFromFile(string strFileName) {
            bool    bRetVal;

            if (m_pgnLexical == null) {
                m_pgnLexical = new PgnLexical();
            }
            bRetVal = m_pgnLexical.InitFromFile(strFileName);
            return(bRetVal);
        }

        /// <summary>
        /// Initialize the parser using a PGN text
        /// </summary>
        /// <param name="strText">  PGN Text</param>
        public void InitFromString(string strText) {
            if (m_pgnLexical == null) {
                m_pgnLexical = new PgnLexical();
            }
            m_pgnLexical.InitFromString(strText);
        }

        /// <summary>
        /// Initialize from a PGN buffer object
        /// </summary>
        /// <param name="pgnLexical">    PGN Lexical Analyser</param>
        public void InitFromPGNBuffer(PgnLexical pgnLexical) {
            m_pgnLexical = pgnLexical;
        }

        /// <summary>
        /// PGN buffer
        /// </summary>
        public PgnLexical PGNLexical {
            get {
                return(m_pgnLexical);
            }
        }

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(long lStartPos, int iLength) {
            string  strRetVal;

            strRetVal = m_pgnLexical.GetStringAtPos(lStartPos, iLength);
            return(strRetVal);
        }

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <param name="tok">  Token</param>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(PgnLexical.Token tok) {
            string  strRetVal;

            strRetVal = m_pgnLexical.GetStringAtPos(tok.lStartPos, tok.iSize);
            return(strRetVal);
        }

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <param name="pgnGame">    PGN game</param>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(PgnGame pgnGame) {
            return(GetCodeInError(pgnGame.StartingPos, pgnGame.Length));
        }

        /// <summary>
        /// Callback for 
        /// </summary>
        /// <param name="cookie">           Callback cookie</param>
        /// <param name="ePhase">           Parsing phase OpeningFile,ReadingFile,RawParsing,AnalysingMoves</param>
        /// <param name="iFileIndex">       File index</param>
        /// <param name="iFileCount">       Number of files to parse</param>
        /// <param name="strFileName">      File name</param>
        /// <param name="iGameProcessed">   Game processed since the last update</param>
        /// <param name="iGameCount">       Game count</param>
        public delegate void delProgressCallBack(object cookie, ParsingPhaseE ePhase, int iFileIndex, int iFileCount, string strFileName, int iGameProcessed, int iGameCount);

        /// <summary>
        /// Decode a move
        /// </summary>
        /// <param name="pgnGame">      PGN game</param>
        /// <param name="strPos">       Position</param>
        /// <param name="iStartCol">    Returns the starting column found in move if specified (-1 if not)</param>
        /// <param name="iStartRow">    Returns the starting row found in move if specified (-1 if not)</param>
        /// <param name="iEndPos">      Returns the ending position of the move</param>
        private void DecodeMove(PgnGame pgnGame, string strPos, out int iStartCol, out int iStartRow, out int iEndPos) {
            Char    cChr1;
            Char    cChr2;
            Char    cChr3;
            Char    cChr4;

            switch(strPos.Length) {
            case 2:
                cChr1   = strPos[0];
                cChr2   = strPos[1];
                if (cChr1 < 'a' || cChr1 > 'h' ||
                    cChr2 < '1' || cChr2 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                iStartCol   = -1;
                iStartRow   = -1;
                iEndPos     = (7 - (cChr1 - 'a')) + ((cChr2 - '1') << 3);
                break;
            case 3:
                cChr1   = strPos[0];
                cChr2   = strPos[1];
                cChr3   = strPos[2];
                if (cChr1 >= 'a' && cChr1 <= 'h') {
                    iStartCol   = 7 - (cChr1 - 'a');
                    iStartRow   = -1;
                } else if (cChr1 >= '1' && cChr1 <= '8') {
                    iStartCol   = -1;
                    iStartRow   = (cChr1 - '1');
                } else {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                if (cChr2 < 'a' || cChr2 > 'h' ||
                    cChr3 < '1' || cChr3 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                iEndPos     = (7 - (cChr2 - 'a')) + ((cChr3 - '1') << 3);
                break;
            case 4:
                cChr1   = strPos[0];
                cChr2   = strPos[1];
                cChr3   = strPos[2];
                cChr4   = strPos[3];
                if (cChr1 < 'a' || cChr1 > 'h' ||
                    cChr2 < '1' || cChr2 > '8' ||
                    cChr3 < 'a' || cChr3 > 'h' ||
                    cChr4 < '1' || cChr4 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                iStartCol   = 7 - (cChr1 - 'a');
                iStartRow   = (cChr2 - '1');
                iEndPos     = (7 - (cChr3 - 'a')) + ((cChr4 - '1') << 3);
                break;
            default:
                throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
            }
        }

        /// <summary>
        /// Find a castle move
        /// </summary>
        /// <param name="pgnGame">          PGN game</param>
        /// <param name="ePlayerColor">     Color moving</param>
        /// <param name="bShortCastling">   true for short, false for long</param>
        /// <param name="iTruncated">       Truncated count</param>
        /// <param name="strMove">          Move</param>
        /// <param name="move">             Returned moved if found</param>
        /// <returns>
        /// Moving position (Starting Position + Ending Position * 256) or -1 if error
        /// </returns>
        private short FindCastling(PgnGame pgnGame, ChessBoard.PlayerE ePlayerColor, bool bShortCastling, ref int iTruncated, string strMove, ref MoveExt move) {
            short       nRetVal = -1;
            int         iWantedDelta;
            int         iDelta;
            List<Move>  arrMovePos;

            arrMovePos      = m_chessBoard.EnumMoveList(ePlayerColor);
            iWantedDelta    = bShortCastling ? 2 : -2;
            foreach (Move moveTmp in arrMovePos) {
                if ((moveTmp.Type & Move.TypeE.MoveTypeMask) == Move.TypeE.Castle) {
                    iDelta = ((int)moveTmp.StartPos & 7) - ((int)moveTmp.EndPos & 7);
                    if (iDelta == iWantedDelta) {
                        nRetVal = (short)(moveTmp.StartPos + (moveTmp.EndPos << 8));
                        move        = new MoveExt(moveTmp);
                        m_chessBoard.DoMove(move);
                    }
                }
            }
            if (nRetVal == -1) {
                if (m_bDiagnose) {
                    throw new PgnParserException("Unable to find compatible move - " + strMove, GetCodeInError(pgnGame));
                }
                iTruncated++;
            }
            return(nRetVal);
        }

        /// <summary>
        /// Find a move using the specification
        /// </summary>
        /// <param name="pgnGame">          PGN game</param>
        /// <param name="ePlayerColor">     Color moving</param>
        /// <param name="ePiece">           Piece moving</param>
        /// <param name="iStartCol">        Starting column of the move or -1 if not specified</param>
        /// <param name="iStartRow">        Starting row of the move or -1 if not specified</param>
        /// <param name="iEndPos">          Ending position of the move</param>
        /// <param name="eMoveType">        Type of move. Use for discriminating between different pawn promotion.</param>
        /// <param name="strMove">          Move</param>
        /// <param name="iTruncated">       Truncated count</param>
        /// <param name="move">             Move position</param>
        /// <returns>
        /// Moving position (Starting Position + Ending Position * 256) or -1 if error
        /// </returns>
        private short FindPieceMove(PgnGame pgnGame, ChessBoard.PlayerE ePlayerColor, ChessBoard.PieceE ePiece, int iStartCol, int iStartRow, int iEndPos, Move.TypeE eMoveType, string strMove, ref int iTruncated, ref MoveExt move) {
            short       nRetVal = -1;
            List<Move>  arrMovePos;
            int         iCol;
            int         iRow;
            
            ePiece      = ePiece | ((ePlayerColor == ChessBoard.PlayerE.Black) ? ChessBoard.PieceE.Black : ChessBoard.PieceE.White);
            arrMovePos  = m_chessBoard.EnumMoveList(ePlayerColor);
            foreach (Move moveTmp in arrMovePos) {
                if ((int)moveTmp.EndPos == iEndPos && m_chessBoard[(int)moveTmp.StartPos] == ePiece) {
                    if (eMoveType == Move.TypeE.Normal || (moveTmp.Type & Move.TypeE.MoveTypeMask) == eMoveType) {
                        iCol = (int)moveTmp.StartPos & 7;
                        iRow = (int)moveTmp.StartPos >> 3;
                        if ((iStartCol == -1 || iStartCol == iCol) &&
                            (iStartRow == -1 || iStartRow == iRow)) {
                            if (nRetVal != -1) {
                                throw new PgnParserException("More then one piece found for this move - "  + strMove, GetCodeInError(pgnGame));
                            }
                            move        = new MoveExt(moveTmp);
                            nRetVal     = (short)((int)moveTmp.StartPos + ((int)moveTmp.EndPos << 8));
                            m_chessBoard.DoMove(move);
                        }
                    }
                }
            }            
            if (nRetVal == -1) {
                if (m_bDiagnose) {
                    throw new PgnParserException("Unable to find compatible move - " + strMove, GetCodeInError(pgnGame));
                }
                iTruncated++;
            }
            return(nRetVal);
        }

        /// <summary>
        /// Convert a SAN position into a moving position
        /// </summary>
        /// <param name="pgnGame">          PGN game</param>
        /// <param name="ePlayerColor">     Color moving</param>
        /// <param name="strMove">          Move</param>
        /// <param name="nPos">             Returned moving position (-1 if error, Starting position + Ending position * 256</param>
        /// <param name="iTruncated">       Truncated count</param>
        /// <param name="move">             Move position</param>
        private void CnvSANMoveToPosMove(PgnGame pgnGame, ChessBoard.PlayerE ePlayerColor, string strMove, out short nPos, ref int iTruncated, ref MoveExt move) {
            string              strPureMove;
            int                 iIndex;
            int                 iStartCol;
            int                 iStartRow;
            int                 iEndPos;
            int                 iOfs;
            ChessBoard.PieceE   ePiece;
            Move.TypeE          eMoveType;
            
            eMoveType   = Move.TypeE.Normal;
            nPos        = 0;
            strPureMove = strMove.Replace("x", "").Replace("#", "").Replace("ep","").Replace("+", "");
            iIndex      = strPureMove.IndexOf('=');
            if (iIndex != -1) {
                if (strPureMove.Length > iIndex + 1) {
                    switch(strPureMove[iIndex+1]) {
                    case 'Q':
                        eMoveType = Move.TypeE.PawnPromotionToQueen;
                        break;
                    case 'R':
                        eMoveType = Move.TypeE.PawnPromotionToRook;
                        break;
                    case 'B':
                        eMoveType = Move.TypeE.PawnPromotionToBishop;
                        break;
                    case 'N':
                        eMoveType = Move.TypeE.PawnPromotionToKnight;
                        break;
                    case 'P':
                        eMoveType = Move.TypeE.PawnPromotionToPawn;
                        break;
                    default:
                        nPos = -1;
                        iTruncated++;
                        break;
                    }
                    if (nPos != -1) {
                        strPureMove = strPureMove.Substring(0, iIndex);
                    }
                } else {
                    nPos = -1;
                    iTruncated++;
                }
            }
            if (nPos == 0) {
                if (strPureMove == "O-O") {
                    nPos = FindCastling(pgnGame, ePlayerColor, true /*bShortCastling*/, ref iTruncated, strMove, ref move);
                } else if (strPureMove == "O-O-O") {
                    nPos = FindCastling(pgnGame, ePlayerColor, false /*bShortCastling*/, ref iTruncated, strMove, ref move);
                } else {
                    iOfs = 1;
                    switch(strPureMove[0]) {
                    case 'K':   // King
                        ePiece = ChessBoard.PieceE.King;
                        break;
                    case 'N':   // Knight
                        ePiece = ChessBoard.PieceE.Knight;
                        break;
                    case 'B':   // Bishop
                        ePiece = ChessBoard.PieceE.Bishop;
                        break;
                    case 'R':   // Rook
                        ePiece = ChessBoard.PieceE.Rook;
                        break;
                    case 'Q':   // Queen
                        ePiece = ChessBoard.PieceE.Queen;
                        break;
                    default:    // Pawn
                        ePiece = ChessBoard.PieceE.Pawn;
                        iOfs   = 0;
                        break;
                    }
                    DecodeMove(pgnGame, strPureMove.Substring(iOfs), out iStartCol, out iStartRow, out iEndPos);
                    nPos = FindPieceMove(pgnGame, ePlayerColor, ePiece, iStartCol, iStartRow, iEndPos, eMoveType, strMove, ref iTruncated, ref move);
                }
            }
        }

        /// <summary>
        /// Convert a list of SAN positions into a moving positions
        /// </summary>
        /// <param name="pgnGame">          PGN game</param>
        /// <param name="eColorToPlay">     Color to play</param>
        /// <param name="arrRawMove">       Array of PGN moves</param>
        /// <param name="pnMoveList">       Returned array of moving position (Starting Position + Ending Position * 256)</param>
        /// <param name="listMovePos">      Returned the list of move if not null</param>
        /// <param name="iSkip">            Skipped count</param>
        /// <param name="iTruncated">       Truncated count</param>
        private void CnvSANMoveToPosMove(PgnGame pgnGame, ChessBoard.PlayerE eColorToPlay, List<string> arrRawMove, out short[] pnMoveList, List<MoveExt> listMovePos, ref int iSkip, ref int iTruncated) {
            List<short> arrMoveList;
            MoveExt     move;
            short       nPos;
            
            move             = new MoveExt(ChessBoard.PieceE.None, 0, 0, Move.TypeE.Normal, "", -1, -1, 0, 0);
            arrMoveList      = new List<short>(256);
            try {
                foreach (string strMove in arrRawMove) {
                    CnvSANMoveToPosMove(pgnGame, eColorToPlay, strMove, out nPos, ref iTruncated, ref move);
                    if (nPos != -1) {
                        arrMoveList.Add(nPos);
                        if (listMovePos != null) {
                            listMovePos.Add(move);
                        }
                        eColorToPlay = (eColorToPlay == ChessBoard.PlayerE.Black) ? ChessBoard.PlayerE.White : ChessBoard.PlayerE.Black;
                    } else {
                        break;
                    }
                }
            } catch(PgnParserException ex) {
                ex.MoveList = (arrMoveList == null) ? null : arrMoveList.ToArray();
                throw;
            }
            pnMoveList = arrMoveList.ToArray();
        }

        /// <summary>
        /// Parse FEN definition into a board representation
        /// </summary>
        /// <param name="strFEN">           FEN</param>
        /// <param name="eColorToMove">     Return the color to move</param>
        /// <param name="eBoardStateMask">  Return the mask of castling info</param>
        /// <param name="iEnPassant">       Return the en passant position or 0 if none</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        private bool ParseFEN(string strFEN, out ChessBoard.PlayerE eColorToMove, out ChessBoard.BoardStateMaskE eBoardStateMask, out int iEnPassant) {
            bool                bRetVal = true;
            string[]            arrCmd;
            string[]            arrRow;
            string              strCmd;
            int                 iPos;
            int                 iLinePos;
            int                 iBlankCount;
            ChessBoard.PieceE   ePiece;
            
            eBoardStateMask = (ChessBoard.BoardStateMaskE)0;
            iEnPassant      = 0;
            eColorToMove    = ChessBoard.PlayerE.White;
            arrCmd          = strFEN.Split(' ');
            if (arrCmd.Length != 6) {
                bRetVal = false;
            } else {
                arrRow = arrCmd[0].Split('/');
                if (arrRow.Length != 8) {
                    bRetVal = false;
                } else {
                    iPos = 63;
                    foreach (string strRow in arrRow) {
                        iLinePos = 0;
                        foreach (char cChr in strRow) {
                            ePiece = ChessBoard.PieceE.None;
                            switch(cChr) {
                            case 'P':
                                ePiece = ChessBoard.PieceE.Pawn | ChessBoard.PieceE.White;
                                break;
                            case 'N':
                                ePiece = ChessBoard.PieceE.Knight | ChessBoard.PieceE.White;
                                break;
                            case 'B':
                                ePiece = ChessBoard.PieceE.Bishop | ChessBoard.PieceE.White;
                                break;
                            case 'R':
                                ePiece = ChessBoard.PieceE.Rook | ChessBoard.PieceE.White;
                                break;
                            case 'Q':
                                ePiece = ChessBoard.PieceE.Queen | ChessBoard.PieceE.White;
                                break;
                            case 'K':
                                ePiece = ChessBoard.PieceE.King | ChessBoard.PieceE.White;
                                break;
                            case 'p':
                                ePiece = ChessBoard.PieceE.Pawn | ChessBoard.PieceE.Black;
                                break;
                            case 'n':
                                ePiece = ChessBoard.PieceE.Knight | ChessBoard.PieceE.Black;
                                break;
                            case 'b':
                                ePiece = ChessBoard.PieceE.Bishop | ChessBoard.PieceE.Black;
                                break;
                            case 'r':
                                ePiece = ChessBoard.PieceE.Rook | ChessBoard.PieceE.Black;
                                break;
                            case 'q':
                                ePiece = ChessBoard.PieceE.Queen | ChessBoard.PieceE.Black;
                                break;
                            case 'k':
                                ePiece = ChessBoard.PieceE.King | ChessBoard.PieceE.Black;
                                break;
                            default:
                                if (cChr >= '1' && cChr <= '8') {
                                     iBlankCount = Int32.Parse(cChr.ToString());
                                     if (iBlankCount + iLinePos <= 8) {
                                        for (int iIndex = 0; iIndex < iBlankCount; iIndex++) {
                                            m_chessBoard[iPos--] = ChessBoard.PieceE.None;
                                        }
                                        iLinePos += iBlankCount;
                                    }
                                } else {
                                    bRetVal = false;
                                }
                                break;
                            }
                            if (bRetVal && ePiece != ChessBoard.PieceE.None) {
                                if (iLinePos < 8) {
                                    m_chessBoard[iPos--] = ePiece;
                                    iLinePos++;
                                } else {
                                    bRetVal = false;
                                }
                            }
                        }
                        if (iLinePos != 8) {
                            bRetVal = false;
                        }
                    }
                    if (bRetVal) {
                        strCmd  = arrCmd[1];
                        if (strCmd == "w") {
                            eColorToMove = ChessBoard.PlayerE.White;
                        } else if (strCmd == "b") {
                            eColorToMove = ChessBoard.PlayerE.Black;
                        } else {
                            bRetVal = false;
                        }
                        strCmd = arrCmd[2];
                        if (strCmd != "-") {
                            for (int iIndex = 0; iIndex < strCmd.Length; iIndex++) {
                                switch(strCmd[iIndex]) {
                                case 'K':
                                    eBoardStateMask |= ChessBoard.BoardStateMaskE.WRCastling;
                                    break;
                                case 'Q':
                                    eBoardStateMask |= ChessBoard.BoardStateMaskE.WLCastling;
                                    break;
                                case 'k':
                                    eBoardStateMask |= ChessBoard.BoardStateMaskE.BRCastling;
                                    break;
                                case 'q':
                                    eBoardStateMask |= ChessBoard.BoardStateMaskE.BLCastling;
                                    break;
                                }
                            }
                        }
                        strCmd = arrCmd[3];
                        if (strCmd == "-") {
                            iEnPassant = 0;
                        } else {
                            iEnPassant = PgnUtil.GetSquareIDFromPGN(strCmd);
                            if (iEnPassant == -1) {
                                iEnPassant = 0;
                            }
                        }
                    }
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Parse FEN definition into a board representation
        /// </summary>
        /// <param name="strFEN">           FEN</param>
        /// <param name="eStartingColor">   Return the color to move</param>
        /// <param name="chessBoard">       Return the chess board represented by this FEN</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ParseFEN(string strFEN, out ChessBoard.PlayerE eStartingColor, out ChessBoard chessBoard) {
            bool                        bRetVal;
            ChessBoard.BoardStateMaskE  eBoardMask;
            int                         iEnPassant;

            m_chessBoard.OpenDesignMode();
            bRetVal = ParseFEN(strFEN, out eStartingColor, out eBoardMask, out iEnPassant);
            m_chessBoard.CloseDesignMode(eStartingColor, eBoardMask, iEnPassant);
            chessBoard = bRetVal ? m_chessBoard.Clone() : null;
            return(bRetVal);
        }

        /// <summary>
        /// Parse PGN moves
        /// </summary>
        /// <param name="listSANMove">  Returned list of attributes for this game. Can be null to skip move section</param>
        /// <param name="bFEN">         true if FEN present</param>
        /// <param name="bBadMoveFound">true if a bad move has been found</param>
        /// <remarks>
        ///     movetext-section        ::= element-sequence game-termination
        ///     element-sequence        ::= {element}
        ///     element                 ::= move-number-indication | SAN-move | numeric-annotation-glyph
        ///     move-number-indication  ::= Integer {'.'}
        ///     recursive-variation     ::= '(' element-sequence ')'
        ///     game-termination        ::= '1-0' | '0-1' | '1/2-1/2' | '*'
        ///  </remarks>
        private string ParseMoves(List<string> listSANMove, bool bFEN, out bool bBadMoveFound) {
            string              strRetVal = null;
            int                 iPlyIndex;
            PgnLexical.Token    tok;

            iPlyIndex       = 2;
            tok             = m_pgnLexical.GetNextToken();
            bBadMoveFound   = false;
            switch(tok.eType) {
            case PgnLexical.TokenTypeE.TOK_Integer:
            case PgnLexical.TokenTypeE.TOK_Symbol:
            case PgnLexical.TokenTypeE.TOK_NAG:
                while (tok.eType != PgnLexical.TokenTypeE.TOK_EOF && tok.eType != PgnLexical.TokenTypeE.TOK_Termination) {
                    switch(tok.eType) {
                    case PgnLexical.TokenTypeE.TOK_Integer:
                        if (!bFEN && tok.iValue != iPlyIndex / 2) {
                            throw new PgnParserException("Bad move number", GetCodeInError(tok));
                        }
                        break;
                    case PgnLexical.TokenTypeE.TOK_Dot:
                        break;
                    case PgnLexical.TokenTypeE.TOK_Symbol:
                        if (listSANMove != null) {
                            listSANMove.Add(tok.strValue);
                        }
                        iPlyIndex++;
                        break;
                    case PgnLexical.TokenTypeE.TOK_UnknownToken:
                        if (listSANMove != null) {
                            listSANMove.Add(tok.strValue);
                        }
                        iPlyIndex++;
                        bBadMoveFound = true;
                        break;
                    case PgnLexical.TokenTypeE.TOK_NAG:
                        break;
                    }
                    tok = m_pgnLexical.GetNextToken();
                }
                m_pgnLexical.AssumeToken(PgnLexical.TokenTypeE.TOK_Termination, tok);
                strRetVal  = tok.strValue;
                break;
            case PgnLexical.TokenTypeE.TOK_Termination:
                break;
            default:
                m_pgnLexical.PushToken(tok);
                break;
            }
            return(strRetVal);
        }

        /// <summary>
        /// Parse PGN attributes
        /// </summary>
        /// <param name="attrs">    Returned list of attributes for this game</param>
        ///     tag-section     ::= {tag-pair}
        ///     tag-pair        ::= '[' tag-name tag-value ']'
        ///     tag-name        ::= identifier
        ///     tag-value       ::= string
        private void ParseAttrs(ref Dictionary<string, string>  attrs) {
            PgnLexical.Token tok;
            PgnLexical.Token tokName;
            PgnLexical.Token tokValue;

            tok =   m_pgnLexical.GetNextToken();
            while (tok.eType == PgnLexical.TokenTypeE.TOK_OpenSBracket) {
                tokName     = m_pgnLexical.AssumeToken(PgnLexical.TokenTypeE.TOK_Symbol);
                tokValue    = m_pgnLexical.AssumeToken(PgnLexical.TokenTypeE.TOK_String);
                m_pgnLexical.AssumeToken(PgnLexical.TokenTypeE.TOK_CloseSBracket);
                if (attrs == null && tokName.strValue == "FEN") {
                    attrs = new Dictionary<string, string>();
                }
                if (attrs != null) {
                    attrs.Add(tokName.strValue, tokValue.strValue);
                }
                tok = m_pgnLexical.GetNextToken();
            }
            m_pgnLexical.PushToken(tok);
        }

        /// <summary>
        /// Parse a PGN text
        /// </summary>
        /// <param name="bAttrList">    Game to be filled with attributes and moves</param>
        /// <param name="bMoveList">    Game to be filled with attributes and moves</param>
        /// <param name="bBadMoveFound">true if a bad move has been found</param>
        /// <returns>
        /// true if a game has been found, false if none
        /// </returns>
        /// <remarks>
        ///     PGN-game        ::= tag-section movetext-section
        ///     tag-section     ::= tag-pair
        /// </remarks>
        private PgnGame ParsePGN(bool bAttrList, bool bMoveList, out bool bBadMoveFound) {
            PgnGame             pgnGame;
            PgnLexical.Token    tok;

            bBadMoveFound   = false;
            tok             = m_pgnLexical.PeekToken();
            if (tok.eType == PgnLexical.TokenTypeE.TOK_EOF) {
                pgnGame = null;
            } else {
                pgnGame             = new PgnGame(bAttrList, bMoveList);
                pgnGame.StartingPos = tok.lStartPos;
                string strGame      = GetCodeInError(pgnGame.StartingPos, 1);
                if (strGame[0] != '[') {
                    System.Windows.MessageBox.Show("Oops! Game doesn't begin with '[' Pos=" + pgnGame.StartingPos.ToString());
                }
                if (pgnGame.attrs != null) {
                    pgnGame.attrs.Clear();
                }
                if (pgnGame.sanMoves != null) {
                    pgnGame.sanMoves.Clear();
                }
                ParseAttrs(ref pgnGame.attrs);
                ParseMoves(pgnGame.sanMoves, pgnGame.FEN != null, out bBadMoveFound);
                tok = m_pgnLexical.PeekToken();
                pgnGame.Length = (int)(tok.lStartPos - pgnGame.StartingPos);
            }
            return(pgnGame);
        }

        /// <summary>
        /// Analyze the PGN games to find the non-ambiguous move list
        /// </summary>
        /// <param name="pgnGame">              Game being analyzed</param>
        /// <param name="bIgnoreMoveListIfFEN"> Ignore the move list if FEN is found</param>
        /// <param name="bFillMoveExtList">     Fills the move extended list if true</param>
        /// <param name="iSkip">                Number of games skipped</param>
        /// <param name="iTruncated">           Number of games truncated</param>
        /// <param name="strError">             Error if any</param>
        /// <returns>
        /// false if invalid board
        /// </returns>
        /// <remarks>
        /// 
        /// The parser understand an extended version of the [TimeControl] tag:
        /// 
        ///     [TimeControl "?:123:456"]   where 123 = white tick count, 456 = black tick count (100 nano-sec unit)
        ///
        /// The parser also understand the following standard tags:
        /// 
        ///     [White] [Black] [FEN] [WhiteType] [BlackType]
        /// 
        /// </remarks>
        public bool AnalyzePGN(PgnGame      pgnGame,
                               bool         bIgnoreMoveListIfFEN,
                               bool         bFillMoveExtList,
                               ref int      iSkip,
                               ref int      iTruncated,
                               out string   strError) {
            bool                bRetVal = true;
            string              strFEN;
            List<string>        listRawMove;
            short[]             pnMoveList;
            ChessBoard.PlayerE  eStartingColor;
            ChessBoard          chessBoardStarting;

            strError    = null;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            chessBoardStarting  = null;
            eStartingColor      = ChessBoard.PlayerE.White;
            strFEN              = pgnGame.FEN;
            listRawMove         = pgnGame.sanMoves;
            m_chessBoard.ResetBoard();
            if (strFEN != null) {
                bRetVal = ParseFEN(strFEN, out eStartingColor, out chessBoardStarting);
                if (bRetVal) {
                    pgnGame.StartingColor       = eStartingColor;
                    pgnGame.StartingChessBoard  = chessBoardStarting;
                } else {
                    strError = "Error parsing the FEN attribute";
                }
            }
            pgnGame.MoveExtList = bFillMoveExtList ? new List<MoveExt>(bIgnoreMoveListIfFEN ? 0 : 256) : null;
            if (bRetVal && !(strFEN != null && bIgnoreMoveListIfFEN)) {
                if (listRawMove.Count == 0 && chessBoardStarting == null) {
                    iSkip++;
                }
                try {
                    CnvSANMoveToPosMove(pgnGame, eStartingColor, listRawMove, out pnMoveList, pgnGame.MoveExtList, ref iSkip, ref iTruncated);
                    pgnGame.MoveList = pnMoveList;
                } catch(PgnParserException ex) {
                    strError    = ex.Message + "\r\n\r\n" + ex.CodeInError;
                    bRetVal     = false;
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Parse if its a FEN line. FEN have only one line and must have 7 '/' which is highly improbable for a PGN text
        /// </summary>
        /// <param name="eStartingColor">   Return the color to move</param>
        /// <param name="chessBoard">       Return the chessboard represent by this FEN</param>
        /// <returns>
        /// true if its a FEN text, false if not
        /// </returns>
        private bool ParseIfFENLine(out ChessBoard.PlayerE eStartingColor, out ChessBoard chessBoard) {
            bool    bRetVal = false;

            eStartingColor  = ChessBoard.PlayerE.White;
            chessBoard      = null;
            if (m_pgnLexical.IsOnlyFEN()) {
                bRetVal = ParseFEN(m_pgnLexical.GetStringAtPos(0, (int)m_pgnLexical.TextSize) , out eStartingColor, out chessBoard);
            }
            return(bRetVal);
        }

        /// <summary>
        /// Parse a single PGN/FEN game
        /// </summary>
        /// <param name="bIgnoreMoveListIfFEN"> Ignore the move list if FEN is found</param>
        /// <param name="iSkip">                Number of games skipped</param>
        /// <param name="iTruncated">           Number of games truncated</param>
        /// <param name="pgnGame">              Returned PGN game</param>
        /// <param name="strError">             Error if any</param>
        /// <returns>
        /// false if the board specified by FEN is invalid.
        /// </returns>
        public bool ParseSingle(bool        bIgnoreMoveListIfFEN,
                                out int     iSkip,
                                out int     iTruncated,
                                out PgnGame pgnGame,
                                out string  strError) {
            bool                bRetVal = true;
            bool                bBadMoveFound;
            ChessBoard.PlayerE  eStartingColor;
            ChessBoard          chessBoardStarting;

            strError = null;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            iSkip       = 0;
            iTruncated  = 0;
            if (ParseIfFENLine(out eStartingColor, out chessBoardStarting)) {
                pgnGame                     = new PgnGame(true /*bAttrList*/, true /*bMoveList*/);
                pgnGame.StartingColor       = eStartingColor;
                pgnGame.StartingChessBoard  = chessBoardStarting;
                bRetVal                     = true;
                pgnGame.SetDefaultValue();
            } else {
                pgnGame = ParsePGN(true /*bAttrList*/, true /*bMoveList*/, out bBadMoveFound);
                if (pgnGame != null) {
                    if (bBadMoveFound) {
                        throw new PgnParserException("PGN contains a bad move\r\n\r\n" + GetCodeInError(pgnGame));
                    }
                    pgnGame.SetDefaultValue();
                    bRetVal  = AnalyzePGN(pgnGame,
                                          bIgnoreMoveListIfFEN,
                                          true /*bFillMoveExtList*/,
                                          ref iSkip,
                                          ref iTruncated,
                                          out strError);
                } else {
                    pgnGame = new PgnGame(true /*bAttrList*/, true /*bMoveList*/);
                    bRetVal = true;
                    pgnGame.SetDefaultValue();
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Gets the list of all raw PGN in the specified text
        /// </summary>
        /// <param name="bAttrList">    true to create attributes list</param>
        /// <param name="bMoveList">    true to create move list</param>
        /// <param name="iSkippedCount">Number of game which has been skipped because of bad move</param>
        /// <param name="callback">     Callback</param>
        /// <param name="cookie">       Cookie for callback</param>
        public List<PgnGame> GetAllRawPGN(bool bAttrList, bool bMoveList, out int iSkippedCount, delProgressCallBack callback, object cookie) {
            List<PgnGame>   pgnGames;
            PgnGame         pgnGame;
            int             iBufferCount;
            int             iBufferPos;
            int             iOldBufferPos;
            bool            bBadMoveFound;

            iSkippedCount   = 0;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            m_bJobCancelled = false;
            pgnGames        = new List<PgnGame>(1000000);
            iBufferCount    = m_pgnLexical.BufferCount;
            iBufferPos      = 0;
            iOldBufferPos   = 0;
            if (callback != null) {
                callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, 0, iBufferCount);
            }
            while (!m_bJobCancelled && (pgnGame = ParsePGN(bAttrList, bMoveList, out bBadMoveFound)) != null && !m_bJobCancelled) {
                if (bBadMoveFound) {
                    iSkippedCount++;
                } else {
                    if (pgnGame.sanMoves == null || pgnGame.sanMoves.Count != 0) {
                        pgnGames.Add(pgnGame);
                    }
                    if (callback != null) {
                        iBufferPos  = m_pgnLexical.CurrentBufferPos;
                        if (iBufferPos != iOldBufferPos) {
                            iOldBufferPos = iBufferPos;
                            if ((iBufferPos % 100) == 0) {
                                callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, iBufferPos, iBufferCount);
                            }
                        }
                    }
                }
            }
            if (callback != null) {
                callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, m_pgnLexical.CurrentBufferPos, iBufferCount);
            }
            return(pgnGames);
        }

        /// <summary>
        /// Gets the list of all raw PGN in the specified text
        /// </summary>
        /// <param name="bMoveList">    true to create move list</param>
        /// <param name="bAttrList">    true to create attributes list</param>
        /// <param name="iSkippedCount">Number of games skipped because of bad moves</param>
        public List<PgnGame> GetAllRawPGN(bool bAttrList, bool bMoveList, out int iSkippedCount) {
            return(GetAllRawPGN(bAttrList, bMoveList, out iSkippedCount, null /*callback*/, null /*cookie*/));
        }

        /// <summary>
        /// Analyze the games in the list in multiple threads
        /// </summary>
        /// <param name="pgnGames">     List of games</param>
        /// <param name="iSkip">        Skip count</param>
        /// <param name="iTruncated">   Truncated count</param>
        /// <param name="iThreadCount"> Thread count</param>
        /// <param name="strError">     Error if any</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        private bool AnalyzeInParallel(List<PgnGame> pgnGames, ref int iSkip, ref int iTruncated, int iThreadCount, out string strError) {
            bool    bRetVal                 = true;
            int     iSkipLocalCount         = 0;
            int     iTruncatedLocalCount    = 0;
            string  strLocalError           = null;

            Parallel.For(0, iThreadCount, (iThreadIndex) => {
                PgnGame     pgnGame;
                int         iIndex;
                int         iStart;
                int         iGamePerThread;
                int         iSkipCount = 0;
                int         iTruncatedCount = 0;
                PgnParser   parser;
                string      strErrorTmp;

                parser          = new PgnParser(false);
                parser.InitFromPGNBuffer(m_pgnLexical);
                iGamePerThread  = pgnGames.Count / iThreadCount;
                iStart          = iThreadIndex * iGamePerThread;
                iIndex          = iStart;
                while (iIndex < iStart + iGamePerThread && !m_bJobCancelled) {
                    pgnGame = pgnGames[iIndex];
                    if (parser.AnalyzePGN(pgnGame,
                                          true /*bIgnoreMoveListIfFEN*/,
                                          false /*bFillMoveExtList*/,
                                          ref iSkipCount,
                                          ref iTruncatedCount,
                                          out strErrorTmp)) {
                        lock (pgnGames) {
                            iSkipLocalCount      += iSkipCount;
                            iTruncatedLocalCount += iTruncatedCount;
                        }
                    } else {
                        lock(pgnGames) {
                            strLocalError = strErrorTmp ?? "unknown";
                            CancelParsingJob();
                            bRetVal = false;
                        }
                    }
                    iIndex++;
                }
            });
            iSkip      += iSkipLocalCount;
            iTruncated += iTruncatedLocalCount;
            strError    = strLocalError;
            return(bRetVal);
        }

        /// <summary>
        /// Parse a PGN text file. The move list are returned as a list of array of int. Each int encoding the starting position in the first 8 bits and the ending position in the second 8 bits
        /// </summary>
        /// <param name="listMoveList">         List of moves</param>
        /// <param name="callback">             Delegate callback (can be null)</param>
        /// <param name="cookie">               Cookie for the callback</param>
        /// <param name="iSkip">                Number of games skipped</param>
        /// <param name="iTruncated">           Number of games truncated</param>
        /// <param name="strError">             Error if any</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ParseAllPGNMoveList(List<short[]> listMoveList, delProgressCallBack callback, object cookie, out int iSkip, out int iTruncated, out string strError) {
            bool            bRetVal = true;
            List<PgnGame>   pgnGames;
            PgnGame         pgnGame;
            int             iThreadCount;
            int             iBufferCount;
            int             iBufferPos;
            int             iOldBufferPos;
            bool            bBadMoveFound;
            const int       iGamePerThread = 4096;
            int             iBatchSize;
            int             iTextSizeInMB;

            strError = null;
            if (m_pgnLexical == null) {
                throw new MethodAccessException("Must initialize the parser first");
            }
            iSkip       = 0;
            iTruncated  = 0;
            if (m_bJobCancelled) {
                bRetVal         = false;
            } else {
                iThreadCount    = System.Environment.ProcessorCount;
                iBufferCount    = m_pgnLexical.BufferCount;
                iBufferPos      = 0;
                iOldBufferPos   = 0;
                iTextSizeInMB   = (int)(m_pgnLexical.TextSize / 1048576);
                if (callback != null) {
                    callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, 0, iTextSizeInMB);
                }
                if (iThreadCount == 1) {
                    while (bRetVal && !m_bJobCancelled && (pgnGame = ParsePGN(false /*bAttrList*/, true /*bMoveList*/, out bBadMoveFound)) != null) {
                        if (bBadMoveFound || pgnGame.FEN != null) {
                            iSkip++;
                        } else if (pgnGame.sanMoves.Count != 0) {
                            bRetVal = AnalyzePGN(pgnGame,
                                                 true /*bIgnoreMoveListIfFEN*/,
                                                 false /*bFillMoveExtList*/,
                                                 ref iSkip,
                                                 ref iTruncated,
                                                 out strError);
                            if (bRetVal) {
                                if (pgnGame.MoveList != null) {
                                    listMoveList.Add(pgnGame.MoveList);
                                }
                                if (callback != null) {
                                    iBufferPos  = m_pgnLexical.CurrentBufferPos;
                                    if (iBufferPos != iOldBufferPos) {
                                        iOldBufferPos = iBufferPos;
                                        if ((iBufferPos % 100) == 0) {
                                            callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, iBufferPos, iTextSizeInMB);
                                        }
                                    }
                                }
                                m_pgnLexical.FlushOldBuffer();
                            }
                        }
                    }
                } else {
                    iBatchSize  = iThreadCount * iGamePerThread;
                    pgnGames    = new List<PgnGame>(iBatchSize);
                    while (bRetVal && !m_bJobCancelled && (pgnGame = ParsePGN(false /*bAttrList*/, true /*bMoveList*/, out bBadMoveFound)) != null) {
                        if (bBadMoveFound && pgnGame.FEN != null) {
                            iSkip++;
                        } else if (pgnGame.sanMoves.Count != 0) {
                            pgnGames.Add(pgnGame);
                            if (pgnGames.Count == iBatchSize) {
                                bRetVal = AnalyzeInParallel(pgnGames, ref iSkip, ref iTruncated, iThreadCount, out strError);
                                if (bRetVal) {
                                    foreach (PgnGame pgnGameTmp in pgnGames) {
                                        if (pgnGameTmp.MoveList != null) {
                                            listMoveList.Add(pgnGameTmp.MoveList);
                                        }
                                    }
                                    pgnGames.Clear();
                                    m_pgnLexical.FlushOldBuffer();
                                    if (callback != null) {
                                        iBufferPos  = m_pgnLexical.CurrentBufferPos;
                                        if (iBufferPos != iOldBufferPos) {
                                            iOldBufferPos = iBufferPos;
                                            callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, iBufferPos, iTextSizeInMB);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (bRetVal) {
                        foreach (PgnGame pgnGameTmp in pgnGames) {
                            bRetVal = AnalyzePGN(pgnGameTmp,
                                                 true /*bIgnoreMoveListIfFEN*/,
                                                 false /*bFillMoveExtList*/,
                                                 ref iSkip,
                                                 ref iTruncated,
                                                 out strError);
                            if (bRetVal) {
                                if (pgnGameTmp.MoveList != null) {
                                    listMoveList.Add(pgnGameTmp.MoveList);
                                }
                            }
                        }
                    }
                }
                if (callback != null) {
                    callback(cookie, ParsingPhaseE.RawParsing, 0, 0, null, iBufferCount, iTextSizeInMB);
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Parse a series of PGN games
        /// </summary>
        /// <param name="arrFileNames">     Array of file name</param>
        /// <param name="bMinimizeMemory">  true if no need to keep the attributes</param>
        /// <param name="callback">         Delegate callback (can be null)</param>
        /// <param name="cookie">           Cookie for the callback</param>
        /// <param name="listMoveList">     List of move list array</param>
        /// <param name="iTotalSkipped">    Number of games skipped because of error</param>
        /// <param name="iTotalTruncated">  Number of games truncated</param>
        /// <param name="strError">         Returned error if return value is false</param>
        /// <returns>true if succeed, false if error</returns>
        public static bool ExtractMoveListFromMultipleFiles(string[] arrFileNames, bool bMinimizeMemory, delProgressCallBack callback, object cookie, out List<short[]> listMoveList, out int iTotalSkipped, out int iTotalTruncated, out string strError) {
            bool            bRetVal = true;
            int             iFileIndex;
            string          strFileName;
            int             iSkip;
            int             iTruncated;
            PgnParser       parser;

            m_bJobCancelled = false;
            iTotalSkipped   = 0;
            iTotalTruncated = 0;
            strError        = null;
            iFileIndex      = 0;
            listMoveList    = new List<short[]>(1000000);
            while (iFileIndex < arrFileNames.Length && strError == null && !m_bJobCancelled) {
                strFileName = arrFileNames[iFileIndex++];
                if (callback != null) {
                    callback(cookie, ParsingPhaseE.OpeningFile, iFileIndex, arrFileNames.Length, strFileName, 0, 0);
                }
                parser = new PgnParser(false);
                if (callback != null) {
                    callback(cookie, ParsingPhaseE.ReadingFile, iFileIndex, arrFileNames.Length, strFileName, 0, 0);
                }
                if (parser.InitFromFile(strFileName)) {
                    bRetVal = parser.ParseAllPGNMoveList(listMoveList, callback, cookie, out iSkip, out iTruncated, out strError);
                    if (bRetVal) {
                        iTotalSkipped   += iSkip;
                        iTotalTruncated += iTruncated;
                    }
                } else {
                    strError = "Error loading file";
                }
            }
            if (strError == null && m_bJobCancelled) {
                strError = "Cancelled by the user";
            }
            bRetVal = (strError == null);
            return(bRetVal);
        }

        /// <summary>
        /// Call to cancel the parsing job
        /// </summary>
        public static void CancelParsingJob() {
            m_bJobCancelled = true;
        }

        /// <summary>
        /// true if job has been cancelled
        /// </summary>
        public static bool IsJobCancelled {
            get {
                return(m_bJobCancelled);
            }
        }

        /// <summary>
        /// Apply a SAN move to the board
        /// </summary>
        /// <param name="pgnGame">  PGN game</param>
        /// <param name="strSAN">   SAN move</param>
        /// <param name="move">     Converted move</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ApplySANMoveToBoard(PgnGame pgnGame, string strSAN, out MoveExt move) {
            bool    bRetVal;
            short   nPos;
            int     iTruncated = 0;

            move = new MoveExt(ChessBoard.PieceE.None, 0, 0, Move.TypeE.Normal, "", -1, -1, 0, 0);
            if (!String.IsNullOrEmpty(strSAN)) {
                try {
                    CnvSANMoveToPosMove(pgnGame, 
                                        m_chessBoard.CurrentPlayer,
                                        strSAN,
                                        out nPos,
                                        ref iTruncated,
                                        ref move);
                    bRetVal = (iTruncated == 0);
                } catch(PgnParserException) {
                    bRetVal = false;
                }
            } else {
                bRetVal = false;
            }
            return(bRetVal);
        }
    } // Class PgnParser
} // Namespace
