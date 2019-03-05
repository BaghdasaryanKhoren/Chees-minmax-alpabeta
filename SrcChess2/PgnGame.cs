using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SrcChess2 {

    /// <summary>Type of player (human of computer program)</summary>
    public enum PlayerTypeE {
        /// <summary>Player is a human</summary>
        Human,
        /// <summary>Player is a computer program</summary>
        Program
    };

    /// <summary>
    /// PGN raw game. Attributes and undecoded move list
    /// </summary>
    public class PgnGame {

        /// <summary>
        /// Attribute which has been read
        /// </summary>
        [Flags]
        private enum AttrReadE {
            None            =   0,
            Event           =   1,
            Site            =   2,
            GameDate        =   4,
            Round           =   8,
            WhitePlayer     =   16,
            BlackPlayer     =   32,
            WhiteELO        =   64,
            BlackELO        =   128,
            GameResult      =   256,
            GameTime        =   512,
            WhiteType       =   1024,
            BlackType       =   2048,
            FEN             =   4096,
            TimeControl     =   8192,
            Termination     =  16384,
            WhiteSpan       =  32768,
            BlackSpan       =  65536
        }

        /// <summary>Game starting position in the PGN text file</summary>
        public  long                        StartingPos;
        /// <summary>Game length in the PGN text file</summary>
        public  int                         Length;
        /// <summary>Attributes</summary>
        public  Dictionary<string,string>   attrs;
        /// <summary>Undecoded SAN moves</summary>
        public  List<string>                sanMoves;
        /// <summary>Read attributes</summary>
        private AttrReadE                   m_eReadAttr;
        /// <summary>Event</summary>
        private string                      m_strEvent;
        /// <summary>Site of the event</summary>
        private string                      m_strSite;
        /// <summary>Date of the game</summary>
        private string                      m_strGameDate;
        /// <summary>Round</summary>
        private string                      m_strRound;
        /// <summary>White Player name</summary>
        private string                      m_strWhitePlayer;
        /// <summary>Black Player name</summary>
        private string                      m_strBlackPlayer;
        /// <summary>White ELO (-1 if none)</summary>
        private int                         m_iWhiteELO;
        /// <summary>Black ELO (-1 if none)</summary>
        private int                         m_iBlackELO;
        /// <summary>Game result 1-0, 0-1, 1/2-1/2 or *</summary>
        private string                      m_strGameResult;
        /// <summary>White Human/program</summary>
        private PlayerTypeE                 m_eWhitePlayerType;
        /// <summary>White Human/program</summary>
        private PlayerTypeE                 m_eBlackPlayerType;
        /// <summary>FEN defining the board</summary>
        private string                      m_strFEN;
        /// <summary>Time control</summary>
        private string                      m_strTimeControl;
        /// <summary>Game termination</summary>
        private string                      m_strTermination;
        /// <summary>Time span from White player</summary>
        private TimeSpan                    m_spanWhite;
        /// <summary>Time span from Black player</summary>
        private TimeSpan                    m_spanBlack;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="bAttrList">    true to create an attribute list</param>
        /// <param name="bMoveList">    true to create a move list</param>
        public PgnGame(bool bAttrList, bool bMoveList) {
            attrs           = bAttrList ? new Dictionary<string, string>(10) : null;
            sanMoves        = bMoveList ? new List<string>(256) : null;
            m_eReadAttr     = AttrReadE.None;
        }

        /// <summary>
        /// Event
        /// </summary>
        public string Event {
            get {
                if ((m_eReadAttr & AttrReadE.Event) == 0) {
                    m_eReadAttr |= AttrReadE.Event;
                    if (!attrs.TryGetValue("Event", out m_strEvent)) {
                        m_strEvent = null;
                    }
                }
                return(m_strEvent);
            }
        }

        /// <summary>
        /// Site
        /// </summary>
        public string Site {
            get {
                if ((m_eReadAttr & AttrReadE.Site) == 0) {
                    m_eReadAttr |= AttrReadE.Site;
                    if (!attrs.TryGetValue("Site", out m_strSite)) {
                        m_strSite = null;
                    }
                }
                return(m_strSite);
            }
        }

        /// <summary>
        /// Round
        /// </summary>
        public string Round {
            get {
                if ((m_eReadAttr & AttrReadE.Round) == 0) {
                    m_eReadAttr |= AttrReadE.Round;
                    if (!attrs.TryGetValue("Round", out m_strRound)) {
                        m_strRound = null;
                    }
                }
                return(m_strRound);
            }
        }

        /// <summary>
        /// Date of the game
        /// </summary>
        public string Date {
            get {
                if ((m_eReadAttr & AttrReadE.GameDate) == 0) {
                    m_eReadAttr |= AttrReadE.GameDate;
                    if (!attrs.TryGetValue("Date", out m_strGameDate)) {
                        m_strGameDate = null;
                    }
                }
                return(m_strGameDate);
            }
        }


        /// <summary>
        /// White Player
        /// </summary>
        public string WhitePlayer {
            get {
                if ((m_eReadAttr & AttrReadE.WhitePlayer) == 0) {
                    m_eReadAttr |= AttrReadE.WhitePlayer;
                    if (!attrs.TryGetValue("White", out m_strWhitePlayer)) {
                        m_strWhitePlayer = null;
                    }
                }
                return(m_strWhitePlayer);
            }
        }

        /// <summary>
        /// Black Player
        /// </summary>
        public string BlackPlayer {
            get {
                if ((m_eReadAttr & AttrReadE.BlackPlayer) == 0) {
                    m_eReadAttr |= AttrReadE.BlackPlayer;
                    if (!attrs.TryGetValue("Black", out m_strBlackPlayer)) {
                        m_strBlackPlayer = null;
                    }
                }
                return(m_strBlackPlayer);
            }
        }

        /// <summary>
        /// White ELO
        /// </summary>
        public int WhiteELO {
            get {
                string  strValue;

                if ((m_eReadAttr & AttrReadE.WhiteELO) == 0) {
                    m_eReadAttr |= AttrReadE.WhiteELO;
                    if (!attrs.TryGetValue("WhiteElo", out strValue) || !Int32.TryParse(strValue, out m_iWhiteELO)) {
                        m_iWhiteELO = -1;
                    }
                }
                return(m_iWhiteELO);
            }
        }

        /// <summary>
        /// Black ELO
        /// </summary>
        public int BlackELO {
            get {
                string  strValue;

                if ((m_eReadAttr & AttrReadE.BlackELO) == 0) {
                    m_eReadAttr |= AttrReadE.BlackELO;
                    if (!attrs.TryGetValue("BlackElo", out strValue) || !Int32.TryParse(strValue, out m_iBlackELO)) {
                        m_iBlackELO = -1;
                    }
                }
                return(m_iBlackELO);
            }
        }

        /// <summary>
        /// Game Result
        /// </summary>
        public string GameResult {
            get {
                if ((m_eReadAttr & AttrReadE.GameResult) == 0) {
                    m_eReadAttr |= AttrReadE.GameResult;
                    if (!attrs.TryGetValue("Result", out m_strGameResult)) {
                        m_strGameResult = null;
                    }
                }
                return(m_strGameResult);
            }
        }

        /// <summary>
        /// White player type
        /// </summary>
        public PlayerTypeE WhiteType {
            get {
                string  strValue;

                if ((m_eReadAttr & AttrReadE.WhiteType) == 0) {
                    m_eReadAttr |= AttrReadE.WhiteType;
                    if (attrs.TryGetValue("WhiteType", out strValue)) {
                        m_eWhitePlayerType = String.Compare(strValue, "Program", true /*IgnoreCase*/) == 0 ? PlayerTypeE.Program : PlayerTypeE.Human;
                    } else {
                        m_eWhitePlayerType = PlayerTypeE.Human;
                    }
                }
                return(m_eWhitePlayerType);
            }
        }

        /// <summary>
        /// Black player type
        /// </summary>
        public PlayerTypeE BlackType {
            get {
                string  strValue;

                if ((m_eReadAttr & AttrReadE.BlackType) == 0) {
                    m_eReadAttr |= AttrReadE.BlackType;
                    if (attrs.TryGetValue("BlackType", out strValue)) {
                        m_eBlackPlayerType = String.Compare(strValue, "Program", true /*IgnoreCase*/) == 0 ? PlayerTypeE.Program : PlayerTypeE.Human;
                    } else {
                        m_eBlackPlayerType = PlayerTypeE.Human;
                    }
                }
                return(m_eBlackPlayerType);
            }
        }

        /// <summary>
        /// FEN defining the board
        /// </summary>
        public string FEN {
            get {
                if ((m_eReadAttr & AttrReadE.FEN) == 0) {
                    m_eReadAttr |= AttrReadE.FEN;
                    if (attrs == null || !attrs.TryGetValue("FEN", out m_strFEN)) {
                        m_strFEN = null;
                    }
                }
                return(m_strFEN);
            }
        }

        /// <summary>
        /// Time control
        /// </summary>
        public string TimeControl {
            get {
                if ((m_eReadAttr & AttrReadE.TimeControl) == 0) {
                    m_eReadAttr |= AttrReadE.TimeControl;
                    if (!attrs.TryGetValue("TimeControl", out m_strTimeControl)) {
                        m_strTimeControl = null;
                    }
                }
                return(m_strTimeControl);
            }
        }

        /// <summary>
        /// Game termination
        /// </summary>
        public string Termination {
            get {
                if ((m_eReadAttr & AttrReadE.Termination) == 0) {
                    m_eReadAttr |= AttrReadE.Termination;
                    if (!attrs.TryGetValue("Termination", out m_strTermination)) {
                        m_strTermination = null;
                    }
                }
                return(m_strTermination);
            }
        }

        /// <summary>
        /// Initialize the proprietary time control
        /// </summary>
        private void InitPlayerSpan() {
            string      strTimeControl;
            string[]    arrTimeControl;
            int         iTick1;
            int         iTick2;

            m_spanWhite     = TimeSpan.Zero;
            m_spanBlack     = TimeSpan.Zero;
            strTimeControl  = TimeControl;
            if (strTimeControl != null) {
                arrTimeControl = strTimeControl.Split(':');
                if (arrTimeControl.Length == 3                      &&
                    arrTimeControl[0] == "?"                        &&
                    Int32.TryParse(arrTimeControl[1], out iTick1)   &&
                    Int32.TryParse(arrTimeControl[2], out iTick2)) {
                    m_spanWhite = new TimeSpan(iTick1);
                    m_spanBlack = new TimeSpan(iTick2);
                }
            }
            m_eReadAttr |= AttrReadE.WhiteSpan | AttrReadE.BlackSpan;
        }

        /// <summary>
        /// Time used by the White player
        /// </summary>
        public TimeSpan WhiteSpan {
            get {
                if ((m_eReadAttr & AttrReadE.WhiteSpan) == 0) {
                    m_eReadAttr |= AttrReadE.WhiteSpan;
                    InitPlayerSpan();
                }
                return(m_spanWhite);
            }
        }

        /// <summary>
        /// Time used by the Black player
        /// </summary>
        public TimeSpan BlackSpan {
            get {
                if ((m_eReadAttr & AttrReadE.BlackSpan) == 0) {
                    m_eReadAttr |= AttrReadE.BlackSpan;
                    InitPlayerSpan();
                }
                return(m_spanBlack);
            }
        }

        /// <summary>
        /// List of moves defines as an integer per move defines as StartingPos + EndingPos * 256
        /// </summary>
        public short[] MoveList {
            get;
            set;
        }

        /// <summary>
        /// List of moves defines as MoveExt object
        /// </summary>
        public List<MoveExt> MoveExtList {
            get;
            set;
        }

        /// <summary>
        /// Starting chessboard when defined with a FEN
        /// </summary>
        public ChessBoard StartingChessBoard {
            get;
            set;
        }

        /// <summary>
        /// Starting player
        /// </summary>
        public ChessBoard.PlayerE StartingColor {
            get;
            set;
        }

        /// <summary>
        /// Set default value for some properties
        /// </summary>
        public void SetDefaultValue() {
            if (WhitePlayer == null) {
                m_strWhitePlayer = "Player 1";
            }
            if (BlackPlayer == null) {
                m_strBlackPlayer = "Player 2";
            }
        }

    } // Class PGNGame
} // Namespace
