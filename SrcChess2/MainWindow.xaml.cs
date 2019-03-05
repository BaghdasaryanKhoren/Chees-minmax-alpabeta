using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// TODO:
    ///     Ply must be 2 moves
    ///     Implement blitz
    ///     Implement background thinking while human is playing
    ///     Try to find a better color picker
    ///     Indicates the rating of the move found
    ///     
    public partial class MainWindow : Window {

        #region Types
        /// <summary>Getting computer against computer playing statistic</summary>
        private class BoardEvaluationStat {
            public BoardEvaluationStat(int iGameCount) {
                m_timeSpanMethod1   = TimeSpan.Zero;
                m_timeSpanMethod2   = TimeSpan.Zero;
                m_eResult           = ChessBoard.GameResultE.OnGoing;
                m_iMethod1MoveCount = 0;
                m_iMethod2MoveCount = 0;
                m_iMethod1WinCount  = 0;
                m_iMethod2WinCount  = 0;
                m_bUserCancel       = false;
                m_iGameIndex        = 0;
                m_iGameCount        = iGameCount;
            }
            public  TimeSpan                m_timeSpanMethod1;
            public  TimeSpan                m_timeSpanMethod2;
            public  ChessBoard.GameResultE  m_eResult;
            public  int                     m_iMethod1MoveCount;
            public  int                     m_iMethod2MoveCount;
            public  int                     m_iMethod1WinCount;
            public  int                     m_iMethod2WinCount;
            public  bool                    m_bUserCancel;
            public  int                     m_iGameIndex;
            public  int                     m_iGameCount;
            public  SearchMode              m_searchModeOri;
            public  MessageModeE            m_eMessageModeOri;
        };
        
        /// <summary>Use for computer move</summary>
        public enum MessageModeE {
            /// <summary>No message</summary>
            Silent      = 0,
            /// <summary>Only messages for move which are terminating the game</summary>
            CallEndGame = 1,
            /// <summary>All messages</summary>
            Verbose     = 2
        };
        
        /// <summary>Current playing mode</summary>
        public enum PlayingModeE {
            /// <summary>Player plays against another player</summary>
            PlayerAgainstPlayer,
            /// <summary>Computer play the white against a human black</summary>
            ComputerPlayWhite,
            /// <summary>Computer play the black against a human white</summary>
            ComputerPlayBlack,
            /// <summary>Computer play against computer</summary>
            ComputerPlayBoth,
            /// <summary>Design mode.</summary>
            DesignMode,
            /// <summary>Test evaluation methods. Computer play against itself in loop using two different evaluation methods</summary>
            TestEvaluationMethod
        };
        #endregion

        #region Command
        /// <summary>Command: New Game</summary>
        public static readonly RoutedUICommand              NewGameCommand              = new RoutedUICommand("_New Game...",                   "NewGame",              typeof(MainWindow));
        /// <summary>Command: Load Game</summary>
        public static readonly RoutedUICommand              LoadGameCommand             = new RoutedUICommand("_Load Game...",                  "LoadGame",             typeof(MainWindow));
        /// <summary>Command: Load Game</summary>
        public static readonly RoutedUICommand              LoadPuzzleCommand           = new RoutedUICommand("Load a Chess _Puzzle...",        "LoadPuzzle",           typeof(MainWindow));
        /// <summary>Command: Create Game</summary>
        public static readonly RoutedUICommand              CreateGameCommand           = new RoutedUICommand("_Create Game from PGN...",       "CreateGame",           typeof(MainWindow));
        /// <summary>Command: Save Game</summary>
        public static readonly RoutedUICommand              SaveGameCommand             = new RoutedUICommand("_Save Game...",                  "SaveGame",             typeof(MainWindow));
        /// <summary>Command: Save Game in PGN</summary>
        public static readonly RoutedUICommand              SaveGameInPGNCommand        = new RoutedUICommand("Save Game _To PGN...",           "SaveGameToPGN",        typeof(MainWindow));
        /// <summary>Command: Save Game in PGN</summary>
        public static readonly RoutedUICommand              CreateSnapshotCommand       = new RoutedUICommand("Create a _Debugging Snapshot...","CreateSnapshot",       typeof(MainWindow));
        /// <summary>Command: Connect to FICS Server</summary>
        public static readonly RoutedUICommand              ConnectToFICSCommand        = new RoutedUICommand("Connect to _FICS Server...",     "ConnectToFICS",        typeof(MainWindow));
        /// <summary>Command: Connect to FICS Server</summary>
        public static readonly RoutedUICommand              DisconnectFromFICSCommand   = new RoutedUICommand("_Disconnect from FICS Server",  "DisconnectFromFICS",   typeof(MainWindow));
        /// <summary>Command: Connect to FICS Server</summary>
        public static readonly RoutedUICommand              ObserveFICSGameCommand      = new RoutedUICommand("_Observe a FICS Game...",        "ObserveFICSGame",      typeof(MainWindow));
        /// <summary>Command: Quit</summary>
        public static readonly RoutedUICommand              QuitCommand                 = new RoutedUICommand("_Quit",                          "Quit",                 typeof(MainWindow));

        /// <summary>Command: Hint</summary>
        public static readonly RoutedUICommand              HintCommand                 = new RoutedUICommand("_Hint",                          "Hint",                 typeof(MainWindow));
        /// <summary>Command: Undo</summary>
        public static readonly RoutedUICommand              UndoCommand                 = new RoutedUICommand("_Undo",                          "Undo",                 typeof(MainWindow));
        /// <summary>Command: Redo</summary>
        public static readonly RoutedUICommand              RedoCommand                 = new RoutedUICommand("_Redo",                          "Redo",                 typeof(MainWindow));
        /// <summary>Command: Refresh</summary>
        public static readonly RoutedUICommand              RefreshCommand              = new RoutedUICommand("Re_fresh",                       "Refresh",              typeof(MainWindow));
        /// <summary>Command: Select Players</summary>
        public static readonly RoutedUICommand              SelectPlayersCommand        = new RoutedUICommand("_Select Players...",             "SelectPlayers",        typeof(MainWindow));
        /// <summary>Command: Automatic Play</summary>
        public static readonly RoutedUICommand              AutomaticPlayCommand        = new RoutedUICommand("_Automatic Play",                "AutomaticPlay",        typeof(MainWindow));
        /// <summary>Command: Fast Automatic Play</summary>
        public static readonly RoutedUICommand              FastAutomaticPlayCommand    = new RoutedUICommand("_Fast Automatic Play",           "FastAutomaticPlay",    typeof(MainWindow));
        /// <summary>Command: Cancel Play</summary>
        public static readonly RoutedUICommand              CancelPlayCommand           = new RoutedUICommand("_Cancel Play",                   "CancelPlay",           typeof(MainWindow));
        /// <summary>Command: Design Mode</summary>
        public static readonly RoutedUICommand              DesignModeCommand           = new RoutedUICommand("_Design Mode",                   "DesignMode",           typeof(MainWindow));

        /// <summary>Command: Search Mode</summary>
        public static readonly RoutedUICommand              SearchModeCommand           = new RoutedUICommand("_Search Mode...",                "SearchMode",           typeof(MainWindow));
        /// <summary>Command: Flash Piece</summary>
        public static readonly RoutedUICommand              FlashPieceCommand           = new RoutedUICommand("_Flash Piece",                   "FlashPiece",           typeof(MainWindow));
        /// <summary>Command: PGN Notation</summary>
        public static readonly RoutedUICommand              PGNNotationCommand          = new RoutedUICommand("_PGN Notation",                  "PGNNotation",          typeof(MainWindow));
        /// <summary>Command: Board Settings</summary>
        public static readonly RoutedUICommand              BoardSettingCommand         = new RoutedUICommand("_Board Settings...",             "BoardSettings",         typeof(MainWindow));
        
        /// <summary>Command: Create a Book</summary>
        public static readonly RoutedUICommand              CreateBookCommand           = new RoutedUICommand("_Create a Book...",              "CreateBook",           typeof(MainWindow));
        /// <summary>Command: Filter a PGN File</summary>
        public static readonly RoutedUICommand              FilterPGNFileCommand        = new RoutedUICommand("_Filter a PGN File...",          "FilterPGNFile",        typeof(MainWindow));
        /// <summary>Command: Test Board Evaluation</summary>
        public static readonly RoutedUICommand              TestBoardEvaluationCommand  = new RoutedUICommand("_Test Board Evaluation...",      "TestBoardEvaluation",  typeof(MainWindow));

        /// <summary>Command: Test Board Evaluation</summary>
        public static readonly RoutedUICommand              AboutCommand                = new RoutedUICommand("_About...",                      "About",                typeof(MainWindow));

        /// <summary>List of all supported commands</summary>
        private static readonly RoutedUICommand[]           m_arrCommands = new RoutedUICommand[] { NewGameCommand,
                                                                                                    LoadGameCommand,
                                                                                                    LoadPuzzleCommand,
                                                                                                    CreateGameCommand,
                                                                                                    SaveGameCommand,
                                                                                                    SaveGameInPGNCommand,
                                                                                                    CreateSnapshotCommand,
                                                                                                    ConnectToFICSCommand,
                                                                                                    DisconnectFromFICSCommand,
                                                                                                    ObserveFICSGameCommand,
                                                                                                    QuitCommand,
                                                                                                    HintCommand,
                                                                                                    UndoCommand,
                                                                                                    RedoCommand,
                                                                                                    RefreshCommand,
                                                                                                    SelectPlayersCommand,
                                                                                                    AutomaticPlayCommand,
                                                                                                    FastAutomaticPlayCommand,
                                                                                                    CancelPlayCommand,
                                                                                                    DesignModeCommand,
                                                                                                    SearchModeCommand,
                                                                                                    FlashPieceCommand,
                                                                                                    PGNNotationCommand,
                                                                                                    BoardSettingCommand,
                                                                                                    CreateBookCommand,
                                                                                                    FilterPGNFileCommand,
                                                                                                    TestBoardEvaluationCommand,
                                                                                                    AboutCommand };
        #endregion

        #region Members        
        /// <summary>Playing mode (player vs player, player vs computer, computer vs computer</summary>
        private PlayingModeE                                m_ePlayingMode;
        /// <summary>Color played by the computer</summary>
        public ChessBoard.PlayerE                           m_eComputerPlayingColor;
        /// <summary>Utility class to handle board evaluation objects</summary>
        private BoardEvaluationUtil                         m_boardEvalUtil;
        /// <summary>List of piece sets</summary>
        private SortedList<string,PieceSet>                 m_listPieceSet;
        /// <summary>Currently selected piece set</summary>
        private PieceSet                                    m_pieceSet;
        /// <summary>Color use to create the background brush</summary>
        internal Color                                      m_colorBackground;
        /// <summary>Dispatcher timer</summary>
        private DispatcherTimer                             m_dispatcherTimer;
        /// <summary>Current message mode</summary>
        private MessageModeE                                m_eMessageMode;
        /// <summary>Search mode</summary>
        private SettingSearchMode                           m_settingSearchMode;
        /// <summary>Connection to FICS Chess Server</summary>
        private FICSInterface.FICSConnection                m_ficsConnection;
        /// <summary>Setting to connect to the FICS server</summary>
        private FICSInterface.FICSConnectionSetting         m_ficsConnectionSetting;
        /// <summary>Convert properties settings to/from object setting</summary>
        private SettingAdaptor                              m_settingAdaptor;
        /// <summary>Search criteria to use to find FICS game</summary>
        private FICSInterface.SearchCriteria                m_searchCriteria;
        /// <summary>Index of the puzzle game being played (if not -1)</summary>
        private int                                         m_iPuzzleGameIndex;
        /// <summary>Mask of puzzle which has been solved</summary>
        internal long[]                                     m_arrPuzzleMask;
        #endregion

        #region Ctor
        /// <summary>
        /// Static Ctor
        /// </summary>
        static MainWindow() {
            NewGameCommand.InputGestures.Add(               new KeyGesture(Key.N,           ModifierKeys.Control));
            LoadGameCommand.InputGestures.Add(              new KeyGesture(Key.O,           ModifierKeys.Control));
            SaveGameCommand.InputGestures.Add(              new KeyGesture(Key.S,           ModifierKeys.Control));
            ConnectToFICSCommand.InputGestures.Add(         new KeyGesture(Key.C,           ModifierKeys.Shift | ModifierKeys.Control));
            ObserveFICSGameCommand.InputGestures.Add(       new KeyGesture(Key.O,           ModifierKeys.Shift | ModifierKeys.Control));
            DisconnectFromFICSCommand.InputGestures.Add(    new KeyGesture(Key.D,           ModifierKeys.Shift | ModifierKeys.Control));
            QuitCommand.InputGestures.Add(                  new KeyGesture(Key.F4,          ModifierKeys.Alt));
            HintCommand.InputGestures.Add(                  new KeyGesture(Key.H,           ModifierKeys.Control));
            UndoCommand.InputGestures.Add(                  new KeyGesture(Key.Z,           ModifierKeys.Control));
            RedoCommand.InputGestures.Add(                  new KeyGesture(Key.Y,           ModifierKeys.Control));
            RefreshCommand.InputGestures.Add(               new KeyGesture(Key.F5));
            SelectPlayersCommand.InputGestures.Add(         new KeyGesture(Key.P,           ModifierKeys.Control));
            AutomaticPlayCommand.InputGestures.Add(         new KeyGesture(Key.F2,          ModifierKeys.Control));
            FastAutomaticPlayCommand.InputGestures.Add(     new KeyGesture(Key.F3,          ModifierKeys.Control));
            CancelPlayCommand.InputGestures.Add(            new KeyGesture(Key.C,           ModifierKeys.Control));
            DesignModeCommand.InputGestures.Add(            new KeyGesture(Key.D,           ModifierKeys.Control));
            SearchModeCommand.InputGestures.Add(            new KeyGesture(Key.M,           ModifierKeys.Control));
            AboutCommand.InputGestures.Add(                 new KeyGesture(Key.F1));
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        public MainWindow() {
            ExecutedRoutedEventHandler      onExecutedCmd;
            CanExecuteRoutedEventHandler    onCanExecuteCmd;

            InitializeComponent();
            m_settingAdaptor                    = new SettingAdaptor(Properties.Settings.Default);
            m_listPieceSet                      = PieceSetStandard.LoadPieceSetFromResource();
            m_chessCtl.Father                   = this;
            m_moveViewer.ChessControl           = m_chessCtl;
            m_eMessageMode                      = MessageModeE.CallEndGame;
            m_lostPieceBlack.ChessBoardControl  = m_chessCtl;
            m_lostPieceBlack.Color              = true;
            m_lostPieceWhite.ChessBoardControl  = m_chessCtl;
            m_lostPieceWhite.Color              = false;
            m_ficsConnectionSetting             = new FICSInterface.FICSConnectionSetting();
            m_boardEvalUtil                     = new BoardEvaluationUtil();
            m_settingSearchMode                 = new SettingSearchMode();
            m_searchCriteria                    = new FICSInterface.SearchCriteria();
            m_arrPuzzleMask                     = new long[2];
            m_iPuzzleGameIndex                  = -1;
            m_settingAdaptor.LoadChessBoardCtl(m_chessCtl);
            m_settingAdaptor.LoadMainWindow(this, m_listPieceSet);
            m_settingAdaptor.LoadFICSConnectionSetting(m_ficsConnectionSetting);
            m_settingAdaptor.LoadSearchMode(m_boardEvalUtil, m_settingSearchMode);
            m_settingAdaptor.LoadMoveViewer(m_moveViewer);
            m_settingAdaptor.LoadFICSSearchCriteria(m_searchCriteria);
            m_chessCtl.SearchMode               = m_settingSearchMode.GetSearchMode();
            m_chessCtl.UpdateCmdState          += m_chessCtl_UpdateCmdState;
            PlayingMode                         = PlayingModeE.ComputerPlayBlack;
            m_moveViewer.NewMoveSelected       += m_moveViewer_NewMoveSelected;
            m_chessCtl.MoveSelected            += m_chessCtl_MoveSelected;
            m_chessCtl.NewMove                 += m_chessCtl_NewMove;
            m_chessCtl.QueryPiece              += m_chessCtl_QueryPiece;
            m_chessCtl.QueryPawnPromotionType  += m_chessCtl_QueryPawnPromotionType;
            m_chessCtl.FindMoveBegin           += m_chessCtl_FindMoveBegin;
            m_chessCtl.FindMoveEnd             += m_chessCtl_FindMoveEnd;
            m_dispatcherTimer                   = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, new EventHandler(dispatcherTimer_Tick), Dispatcher);
            m_dispatcherTimer.Start();
            SetCmdState();
            ShowSearchMode();
            mnuOptionFlashPiece.IsChecked       = m_chessCtl.MoveFlashing;
            mnuOptionPGNNotation.IsChecked      = (m_moveViewer.DisplayMode == MoveViewer.DisplayModeE.PGN);
            m_ficsConnection                    = null;
            onExecutedCmd                       = new ExecutedRoutedEventHandler(OnExecutedCmd);
            onCanExecuteCmd                     = new CanExecuteRoutedEventHandler(OnCanExecuteCmd);
            Closing                            += MainWindow_Closing;
            Closed                             += MainWindow_Closed;
            foreach (RoutedUICommand cmd in m_arrCommands) {
                CommandBindings.Add(new CommandBinding(cmd, onExecutedCmd, onCanExecuteCmd));
            }
        }

        /// <summary>
        /// Called when the main window is closing
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (CheckIfDirty()) {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Called when the main window has been closed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void MainWindow_Closed(object sender, EventArgs e) {
            m_settingAdaptor.SaveChessBoardCtl(m_chessCtl);
            m_settingAdaptor.SaveMainWindow(this);
            m_settingAdaptor.SaveFICSConnectionSetting(m_ficsConnectionSetting);
            m_settingAdaptor.SaveSearchMode(m_settingSearchMode);
            m_settingAdaptor.SaveMoveViewer(m_moveViewer);
            m_settingAdaptor.SaveFICSSearchCriteria(m_searchCriteria);
            m_settingAdaptor.Settings.Save();
            if (m_ficsConnection != null) {
                m_ficsConnection.Dispose();
                m_ficsConnection = null;
            }
        }

        #endregion

        #region Command Handling
        /// <summary>
        /// Executes the specified command
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Routed event argument</param>
        public virtual void OnExecutedCmd(object sender, ExecutedRoutedEventArgs e) {
            ChessBoard.GameResultE  eResult;
            ChessBoard.PlayerE      eComputerColor;
            bool                    bPlayerAgainstPlayer;

            if (e.Command == NewGameCommand) {
                NewGame();
            } else if (e.Command == LoadGameCommand) {
                LoadGame();
            } else if (e.Command == LoadPuzzleCommand) {
                LoadPuzzle();
            } else if (e.Command == CreateGameCommand) {
                CreateGame();
            } else if (e.Command == SaveGameCommand) {
                m_chessCtl.SaveToFile();
            } else if (e.Command == SaveGameInPGNCommand) {
                m_chessCtl.SavePGNToFile();
            } else if (e.Command == CreateSnapshotCommand) {
                m_chessCtl.SaveSnapshot();
            } else if (e.Command == ConnectToFICSCommand) {
                ConnectToFICS();
            } else if (e.Command == ObserveFICSGameCommand) {
                ObserveFICSGame();
            } else if (e.Command == DisconnectFromFICSCommand) {
                DisconnectFromFICS();
            } else if (e.Command == QuitCommand) {
                Close();
            } else if (e.Command == HintCommand) {
                ShowHint();
            } else if (e.Command == UndoCommand) {
                bPlayerAgainstPlayer    = PlayingMode == PlayingModeE.PlayerAgainstPlayer;
                eComputerColor          = PlayingMode == PlayingModeE.ComputerPlayWhite ? ChessBoard.PlayerE.White : ChessBoard.PlayerE.Black;
                m_chessCtl.UndoMove(bPlayerAgainstPlayer, eComputerColor);
            } else if (e.Command == RedoCommand) {
                eResult = m_chessCtl.RedoMove(PlayingMode == PlayingModeE.PlayerAgainstPlayer);
            } else if (e.Command == RefreshCommand) {
                m_chessCtl.Refresh();
            } else if (e.Command == SelectPlayersCommand) {
                SelectPlayers();
            } else if (e.Command == AutomaticPlayCommand) {
                PlayComputerAgainstComputer(true);
            } else if (e.Command == FastAutomaticPlayCommand) {
                PlayComputerAgainstComputer(false);
            } else if (e.Command == CancelPlayCommand) {
                CancelAutoPlay();
            } else if (e.Command == DesignModeCommand) {
                ToggleDesignMode();
            } else if (e.Command == SearchModeCommand) {
                SetSearchMode();
            } else if (e.Command == FlashPieceCommand) {
                ToggleFlashPiece();
            } else if (e.Command == PGNNotationCommand) {
                TogglePGNNotation();
            } else if (e.Command == BoardSettingCommand) {
                ChooseBoardSetting();
            } else if (e.Command == CreateBookCommand) {
                m_chessCtl.CreateBookFromFiles();
            } else if (e.Command == FilterPGNFileCommand) {
                FilterPGNFile();
            } else if (e.Command == TestBoardEvaluationCommand) {
                TestBoardEvaluation();
            } else if (e.Command == AboutCommand) {
                ShowAbout();
            } else {
                e.Handled   = false;
            }
        }

        /// <summary>
        /// Determine if a command can be executed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Routed event argument</param>
        public virtual void OnCanExecuteCmd(object sender, CanExecuteRoutedEventArgs e) {
            bool    bDesignMode;
            bool    bIsBusy;
            bool    bIsSearchEngineBusy;
            bool    bIsObservingGame;

            bDesignMode         = (PlayingMode == PlayingModeE.DesignMode);
            bIsBusy             = m_chessCtl.IsBusy;
            bIsSearchEngineBusy = m_chessCtl.IsSearchEngineBusy;
            bIsObservingGame    = m_chessCtl.IsObservingAGame;
            if (e.Command == NewGameCommand                     ||
                e.Command == CreateGameCommand                  ||
                e.Command == LoadGameCommand                    ||
                e.Command == LoadPuzzleCommand                  ||
                e.Command == SaveGameCommand                    ||
                e.Command == SaveGameInPGNCommand               ||
                e.Command == CreateSnapshotCommand              ||
                e.Command == HintCommand                        ||
                e.Command == RefreshCommand                     ||
                e.Command == SelectPlayersCommand               ||
                e.Command == AutomaticPlayCommand               ||
                e.Command == FastAutomaticPlayCommand           ||
                e.Command == CreateBookCommand                  ||
                e.Command == FilterPGNFileCommand) {
                e.CanExecute    = !(bIsSearchEngineBusy || bDesignMode || bIsBusy || bIsObservingGame);
            } else if (e.Command == QuitCommand                 ||
                       e.Command == SearchModeCommand           ||
                       e.Command == FlashPieceCommand           ||
                       e.Command == DesignModeCommand           ||
                       e.Command == PGNNotationCommand          ||
                       e.Command == BoardSettingCommand         ||
                       e.Command == TestBoardEvaluationCommand  ||
                       e.Command == AboutCommand) {
                e.CanExecute    = !(bIsSearchEngineBusy || bIsBusy || bIsObservingGame);
            } else if (e.Command == CancelPlayCommand) {
                e.CanExecute    = bIsSearchEngineBusy | bIsBusy | bIsObservingGame;
            } else if (e.Command == UndoCommand) {
                e.CanExecute    = (!bIsSearchEngineBusy && !bIsBusy && !bIsObservingGame && !bDesignMode && m_chessCtl.UndoCount >= ((m_ePlayingMode == PlayingModeE.PlayerAgainstPlayer) ? 1 : 2));
            } else if (e.Command == RedoCommand) {
                e.CanExecute    = (!bIsSearchEngineBusy && !bIsBusy && !bIsObservingGame && !bDesignMode && m_chessCtl.RedoCount != 0);
            } else if (e.Command == ConnectToFICSCommand) {
                e.CanExecute    = (m_ficsConnection == null);
            } else if (e.Command == DisconnectFromFICSCommand) {
                e.CanExecute    = (m_ficsConnection != null && !bIsObservingGame);
            } else if (e.Command == ObserveFICSGameCommand) {
                e.CanExecute    = (m_ficsConnection != null && !bIsObservingGame);
            } else {
                e.Handled   = false;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Used piece set
        /// </summary>
        public PieceSet PieceSet {
            get {
                return(m_pieceSet);
            }
            set {
                if (m_pieceSet != value) {
                    m_pieceSet                  = value;
                    m_chessCtl.PieceSet         = value;
                    m_lostPieceBlack.PieceSet   = value;
                    m_lostPieceWhite.PieceSet   = value;
                }
            }
        }

        /// <summary>
        /// Current playing mode (player vs player, player vs computer or computer vs computer)
        /// </summary>
        public PlayingModeE PlayingMode {
            get {
                return(m_ePlayingMode);
            }
            set {
                m_ePlayingMode = value;
                switch(m_ePlayingMode) {
                case PlayingModeE.PlayerAgainstPlayer:
                    m_chessCtl.WhitePlayerType = PlayerTypeE.Human;
                    m_chessCtl.BlackPlayerType = PlayerTypeE.Human;
                    break;
                case PlayingModeE.ComputerPlayWhite:
                    m_chessCtl.WhitePlayerType = PlayerTypeE.Program;
                    m_chessCtl.BlackPlayerType = PlayerTypeE.Human;
                    break;
                case PlayingModeE.ComputerPlayBlack:
                    m_chessCtl.WhitePlayerType = PlayerTypeE.Human;
                    m_chessCtl.BlackPlayerType = PlayerTypeE.Program;
                    break;
                default:
                    m_chessCtl.WhitePlayerType = PlayerTypeE.Program;
                    m_chessCtl.BlackPlayerType = PlayerTypeE.Program;
                    break;
                }
            }
        }

        /// <summary>
        /// Checks if computer must play the current move
        /// </summary>
        public bool IsComputerMustPlay {
            get {
                bool                    bRetVal;
                ChessBoard.GameResultE  eMoveResult;
                ChessBoard              board;

                board   = m_chessCtl.Board;
                switch (m_ePlayingMode) {
                case PlayingModeE.PlayerAgainstPlayer:
                    bRetVal  = false;
                    break;
                case PlayingModeE.ComputerPlayWhite:
                    bRetVal = (board.CurrentPlayer == ChessBoard.PlayerE.White);
                    break;
                case PlayingModeE.ComputerPlayBlack:
                    bRetVal = (board.CurrentPlayer == ChessBoard.PlayerE.Black);
                    break;
                case PlayingModeE.ComputerPlayBoth:
                    bRetVal = false;
                    break;
                case PlayingModeE.DesignMode:
                default:
                    bRetVal = false;
                    break;
                }
                if (bRetVal) {
                    eMoveResult = board.GetCurrentResult();
                    bRetVal     = (eMoveResult == ChessBoard.GameResultE.OnGoing || eMoveResult == ChessBoard.GameResultE.Check);
                }
                return(bRetVal);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Checks if board is dirty and need to be saved
        /// </summary>
        /// <returns>
        /// true if still dirty (command must be canceled), false not
        /// </returns>
        private bool CheckIfDirty() {
            bool    bRetVal = false;

            if (m_chessCtl.IsDirty) {
                switch(MessageBox.Show("Board has been changed. Do you want to save it?", "SrcChess2", MessageBoxButton.YesNoCancel)) {
                case MessageBoxResult.Yes:
                    if (!m_chessCtl.SaveToFile()) {
                        bRetVal = true;
                    }
                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    bRetVal = true;
                    break;
                }
            }
            return(bRetVal);
        }

        /// <summary>
        /// Set the current playing mode. Defined as a method so it can be called by a delegate
        /// </summary>
        /// <param name="ePlayingMode"> Playing mode</param>
        private void SetPlayingMode(PlayingModeE ePlayingMode) {
            PlayingMode = ePlayingMode;
        }

        /// <summary>
        /// Start asynchronous computing
        /// </summary>
        private void StartAsyncComputing() {
            bool        bDifferentThreadForUI;
            SearchMode  searchMode;

            searchMode = m_chessCtl.SearchMode;
            if (searchMode.m_eThreadingMode == SearchMode.ThreadingModeE.OnePerProcessorForSearch) {
                bDifferentThreadForUI   = true;
            } else if (searchMode.m_eThreadingMode == SearchMode.ThreadingModeE.DifferentThreadForSearch) {
                bDifferentThreadForUI   = true;
            } else {
                bDifferentThreadForUI   = false;
            }
            if (bDifferentThreadForUI) {
                SetCmdState();
            }
            m_statusLabelMove.Content           = "Finding Best Move...";
            m_statusLabelPermutation.Content    = "";
            Cursor = Cursors.Wait;
        }

        /// <summary>
        /// Show a move in status bar
        /// </summary>
        /// <param name="ePlayerColor"> Color of the move</param>
        /// <param name="move">         Move</param>
        private void ShowMoveInStatusBar(ChessBoard.PlayerE ePlayerColor, MoveExt move) {
            string                              strPermCount;
            System.Globalization.CultureInfo    ci;

            if (m_chessCtl.IsObservingAGame) {
                strPermCount    = "Waiting next move...";
            } else {
                ci = new System.Globalization.CultureInfo("en-US");
                switch(move.PermutationCount) {
                case -1:
                    strPermCount = "Found in Book.";
                    break;
                case 0:
                    strPermCount = "---";
                    break;
                default:
                    strPermCount = move.PermutationCount.ToString("C0", ci).Replace("$", "") + " permutations evaluated. " + move.CacheHit.ToString("C0", ci).Replace("$","") + " found in cache.";
                    break;
                }
                if (move.SearchDepth != -1) {
                    strPermCount += " " + move.SearchDepth.ToString() + " ply.";
                }
                strPermCount += " " + move.TimeToCompute.TotalSeconds.ToString() + " sec(s).";
            }
            m_statusLabelMove.Content           = ((ePlayerColor == ChessBoard.PlayerE.Black) ? "Black " : "White ") + ChessBoard.GetHumanPos(move);
            m_statusLabelPermutation.Content    = strPermCount;
        }

        /// <summary>
        /// Show the current searching parameters in the status bar
        /// </summary>
        private void ShowSearchMode() {
            string              strTooltip;
            string              strSearchMode;
            SettingSearchMode   settingSearchMode;
            SearchMode          searchMode;

            settingSearchMode   = m_settingSearchMode;
            searchMode          = m_chessCtl.SearchMode;
            switch (settingSearchMode.DifficultyLevel) {
            case SettingSearchMode.DifficultyLevelE.Manual:
                strSearchMode   = "Manual";
                break;
            case SettingSearchMode.DifficultyLevelE.VeryEasy:
                strSearchMode   = "Beginner";
                break;
            case SettingSearchMode.DifficultyLevelE.Easy:
                strSearchMode   = "Easy";
                break;
            case SettingSearchMode.DifficultyLevelE.Intermediate:
                strSearchMode   = "Intermediate";
                break;
            case SettingSearchMode.DifficultyLevelE.Hard:
                strSearchMode   = "Advanced";
                break;
            case SettingSearchMode.DifficultyLevelE.VeryHard:
                strSearchMode   = "More advanced";
                break;
            default:
                strSearchMode   = "???";
                break;
            }
            strTooltip = settingSearchMode.HumanSearchMode();
            m_statusLabelSearchMode.Content = strSearchMode;
            m_statusLabelSearchMode.ToolTip = strTooltip;
        }

        /// <summary>
        /// Display a message related to the MoveStateE
        /// </summary>
        /// <param name="eMoveResult">  Move result</param>
        /// <param name="eMessageMode"> Message mode</param>
        /// <returns>
        /// true if it's the end of the game. false if not
        /// </returns>
        private bool DisplayMessage(ChessBoard.GameResultE eMoveResult, MessageModeE eMessageMode) {
            bool                bRetVal;
            string              strOpponent;
            ChessBoard.PlayerE  eCurrentPlayer;
            
            eCurrentPlayer = m_chessCtl.ChessBoard.CurrentPlayer;
            switch (m_ePlayingMode)	{
            case PlayingModeE.ComputerPlayWhite:
                strOpponent = (eCurrentPlayer == ChessBoard.PlayerE.White) ? "Computer is " : "You are ";
                break;
            case PlayingModeE.ComputerPlayBlack:
                strOpponent = (eCurrentPlayer == ChessBoard.PlayerE.Black) ? "Computer is " : "You are ";
                break;
            default:
                strOpponent = (eCurrentPlayer == ChessBoard.PlayerE.White) ? "White player is " : "Black player is ";
                break;
	        }
            switch(eMoveResult) {
            case ChessBoard.GameResultE.OnGoing:
                bRetVal = false;
                break;
            case ChessBoard.GameResultE.TieNoMove:
                if (eMessageMode != MessageModeE.Silent) {
                    MessageBox.Show("Draw. " + strOpponent + "unable to move.");
                }
                bRetVal = true;
                break;
            case ChessBoard.GameResultE.TieNoMatePossible:
                if (eMessageMode != MessageModeE.Silent) {
                    MessageBox.Show("Draw. Not enough pieces to make a checkmate.");
                }
                bRetVal = true;
                break;
            case ChessBoard.GameResultE.ThreeFoldRepeat:
                if (eMessageMode != MessageModeE.Silent) {
                    MessageBox.Show("Draw. 3 times the same board.");
                }
                bRetVal = true;
                break;
            case ChessBoard.GameResultE.FiftyRuleRepeat:
                if (eMessageMode != MessageModeE.Silent) {
                    MessageBox.Show("Draw. 50 moves without moving a pawn or eating a piece.");
                }
                bRetVal = true;
                break;
            case ChessBoard.GameResultE.Check:
                if (eMessageMode == MessageModeE.Verbose) {
                    MessageBox.Show(strOpponent + "in check.");
                }
                if (m_iPuzzleGameIndex != -1) {
                    m_arrPuzzleMask[m_iPuzzleGameIndex / 64] |= 1L << (m_iPuzzleGameIndex & 63);
                }
                bRetVal = false;
                break;
            case ChessBoard.GameResultE.Mate:
                if (eMessageMode != MessageModeE.Silent) {
                    MessageBox.Show(strOpponent + "checkmate.");
                }
                bRetVal = true;
                break;
            default:
                bRetVal = false;
                break;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Reset the board.
        /// </summary>
        private void ResetBoard() {
            m_chessCtl.ResetBoard();
            SetCmdState();
        }

        /// <summary>
        /// Determine which menu item is enabled
        /// </summary>
        public void SetCmdState() {
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Unlock the chess board when asynchronous computing is finished
        /// </summary>
        private void UnlockBoard() {
            Cursor = Cursors.Arrow;
            SetCmdState();
        }

        /// <summary>
        /// Play the computer move found by the search.
        /// </summary>
        /// <param name="bFlashing">    true to flash moving position</param>
        /// <param name="move">         Best move</param>
        /// <returns>
        /// true if end of game, false if not
        /// </returns>
        private void PlayComputerEnd(bool bFlashing, MoveExt move) {
            ChessBoard.GameResultE  eResult;

            if (move != null) { 
                eResult = m_chessCtl.DoMove(move, bFlashing);
                switch(m_ePlayingMode) {
                case PlayingModeE.ComputerPlayBoth:
                    switch (eResult) {
                    case ChessBoard.GameResultE.OnGoing:
                    case ChessBoard.GameResultE.Check:
                        PlayComputer(bFlashing);
                        break;
                    case ChessBoard.GameResultE.ThreeFoldRepeat:
                    case ChessBoard.GameResultE.FiftyRuleRepeat:
                    case ChessBoard.GameResultE.TieNoMove:
                    case ChessBoard.GameResultE.TieNoMatePossible:
                    case ChessBoard.GameResultE.Mate:
                        break;
                    default:
                        break;
                    }
                    break;
                }
            }
            UnlockBoard();
        }

        /// <summary>
        /// Make the computer play the next move
        /// </summary>
        /// <param name="bFlash">           true to flash moving position</param>
        private void PlayComputer(bool bFlash) {
            StartAsyncComputing();
            if (!m_chessCtl.FindBestMove(null, (x,y) => PlayComputerEnd(x, y), bFlash, PlayerTypeE.Program)) {
                UnlockBoard();
            }
        }

        /// <summary>
        /// Make the computer play the next move
        /// </summary>
        /// <param name="bFlash">           true to flash moving position</param>
        private void PlayComputerAgainstComputer(bool bFlash) {
            m_ePlayingMode = PlayingModeE.ComputerPlayBoth;
            PlayComputer(bFlash);
        }

        /// <summary>
        /// Show the test result of a computer playing against a computer
        /// </summary>
        /// <param name="stat">             Statistic.</param>
        private void TestShowResult(BoardEvaluationStat stat) {
            string      strMsg;
            string      strMethod1;
            string      strMethod2;
            int         iTimeMethod1;
            int         iTimeMethod2;
            SearchMode  searchMode;

            searchMode      = m_chessCtl.SearchMode;
            strMethod1      = searchMode.m_boardEvaluationWhite.Name;
            strMethod2      = searchMode.m_boardEvaluationBlack.Name;
            iTimeMethod1    = (stat.m_iMethod1MoveCount == 0) ? 0 : stat.m_timeSpanMethod1.Milliseconds / stat.m_iMethod1MoveCount;
            iTimeMethod2    = (stat.m_iMethod2MoveCount == 0) ? 0 : stat.m_timeSpanMethod2.Milliseconds / stat.m_iMethod2MoveCount;
            strMsg          = stat.m_iGameCount.ToString() + " game(s) played.\r\n" +
                              stat.m_iMethod1WinCount.ToString() + " win(s) for method #1 (" + strMethod1 + "). Average time = " + stat.m_iMethod1WinCount.ToString() + " ms per move.\r\n" + 
                              stat.m_iMethod2WinCount.ToString() + " win(s) for method #2 (" + strMethod2 + "). Average time = " + stat.m_iMethod2WinCount.ToString() + " ms per move.\r\n" + 
                              (stat.m_iGameCount - stat.m_iMethod1WinCount - stat.m_iMethod2WinCount).ToString() + " draw(s).";
            MessageBox.Show(strMsg);
        }

        private void TestBoardEvaluation_StartNewGame(BoardEvaluationStat stat) {
            SearchMode          searchMode;
            IBoardEvaluation    boardEvaluation;

            m_chessCtl.ResetBoard();
            searchMode                          = m_chessCtl.SearchMode;
            boardEvaluation                     = searchMode.m_boardEvaluationWhite;
            searchMode.m_boardEvaluationWhite   = searchMode.m_boardEvaluationBlack;
            searchMode.m_boardEvaluationBlack   = boardEvaluation;
            if (!m_chessCtl.FindBestMove(null, (x,y) => TestBoardEvaluation_PlayNextMove(x, y), stat, PlayerTypeE.Program)) {
                throw new ApplicationException("How did we get here?");
            }
        }

        /// <summary>
        /// Play the next move when doing a board evaluation
        /// </summary>
        /// <param name="stat"> Board evaluation statistic</param>
        /// <param name="move"> Move to be done</param>
        private void TestBoardEvaluation_PlayNextMove(BoardEvaluationStat stat, MoveExt move) {
            ChessBoard.GameResultE  eResult;
            bool                    bIsSearchCancel;
            bool                    bEven;

            bEven           = ((stat.m_iGameIndex & 1) == 0);
            bIsSearchCancel = m_chessCtl.IsSearchCancel;
            if (move == null || bIsSearchCancel) {
                eResult = ChessBoard.GameResultE.TieNoMove;
            } else if (m_chessCtl.Board.MovePosStack.Count > 250) {
                eResult = ChessBoard.GameResultE.TieNoMatePossible;
            } else {
                if ((m_chessCtl.Board.CurrentPlayer == ChessBoard.PlayerE.White && bEven) ||
                    (m_chessCtl.Board.CurrentPlayer == ChessBoard.PlayerE.Black && !bEven)) {
                    stat.m_timeSpanMethod1 += move.TimeToCompute;
                    stat.m_iMethod1MoveCount++;
                } else {
                    stat.m_timeSpanMethod2 += move.TimeToCompute;
                    stat.m_iMethod2MoveCount++;
                }
                eResult = m_chessCtl.DoMove(move, false /*bFlashing*/);
            }
            if (eResult == ChessBoard.GameResultE.OnGoing || eResult == ChessBoard.GameResultE.Check) {
                if (!m_chessCtl.FindBestMove(null, (x,y) => TestBoardEvaluation_PlayNextMove(x, y), stat, PlayerTypeE.Program)) {
                    throw new ApplicationException("How did we get here?");
                }
            } else {
                if (eResult == ChessBoard.GameResultE.Mate) {
                    if ((m_chessCtl.NextMoveColor == ChessBoard.PlayerE.Black && bEven) ||
                        (m_chessCtl.NextMoveColor == ChessBoard.PlayerE.White && !bEven)) {
                        stat.m_iMethod1WinCount++;
                    } else {
                        stat.m_iMethod2WinCount++;
                    }
                }
                stat.m_iGameIndex++;
                if (stat.m_iGameIndex < stat.m_iGameCount && !bIsSearchCancel) {
                    TestBoardEvaluation_StartNewGame(stat);
                } else {
                    TestShowResult(stat);
                    PlayingMode             = PlayingModeE.PlayerAgainstPlayer;
                    m_chessCtl.SearchMode   = stat.m_searchModeOri;
                    m_eMessageMode          = stat.m_eMessageModeOri;
                    UnlockBoard();
                }
            }
        }

        /// <summary>
        /// Tests the computer playing against itself. Can be called asynchronously by a secondary thread.
        /// </summary>
        /// <param name="iGameCount">       Number of games to play.</param>
        /// <param name="searchMode">       Search mode</param>
        private void TestBoardEvaluation(int iGameCount, SearchMode searchMode) {
            BoardEvaluationStat     stat;

            stat                    = new BoardEvaluationStat(iGameCount);
            stat.m_searchModeOri    = m_chessCtl.SearchMode;
            stat.m_eMessageModeOri  = m_eMessageMode;
            m_eMessageMode          = MessageModeE.Silent;
            m_chessCtl.SearchMode   = searchMode;
            PlayingMode             = PlayingModeE.TestEvaluationMethod;
            TestBoardEvaluation_StartNewGame(stat);
        }

        /// <summary>
        /// Show the hint move in the status bar
        /// </summary>
        /// <param name="bBeforeMove">  true if before showing the move, false if after</param>
        /// <param name="move">         Move to show</param>
        private void ShowHintEnd(bool bBeforeMove, MoveExt move) {
            if (bBeforeMove) {
                ShowMoveInStatusBar(m_chessCtl.NextMoveColor, move);
            } else {
                UnlockBoard();
            }
        }       

        /// <summary>
        /// Show a hint
        /// </summary>
        private void ShowHint() {
            m_iPuzzleGameIndex = -1;    // Hint means you didn't solve it by yourself
            StartAsyncComputing();
            if (!m_chessCtl.ShowHint((x,y) => ShowHintEnd(x,y))) {
                UnlockBoard();
            }
        }

        /// <summary>
        /// Toggle the design mode. In design mode, the user can create its own board
        /// </summary>
        private void ToggleDesignMode() {
            if (PlayingMode == PlayingModeE.DesignMode) {
                PlayingMode                     = PlayingModeE.PlayerAgainstPlayer;
                mnuEditDesignMode.IsCheckable   = false;
                if (frmGameParameter.AskGameParameter(this, m_settingSearchMode)) {
                    ShowSearchMode();
                    m_chessCtl.BoardDesignMode = false;
                    if (m_chessCtl.BoardDesignMode) {
                        PlayingMode = PlayingModeE.DesignMode;
                        MessageBox.Show("Invalid board configuration. Correct or reset.");
                    } else {
                        m_lostPieceBlack.BoardDesignMode    = false;
                        m_lostPieceWhite.Visibility         = System.Windows.Visibility.Visible;
                        StartAutomaticMove();
                    }
                } else {
                    PlayingMode = PlayingModeE.DesignMode;
                }
            } else {
                PlayingMode                         = PlayingModeE.DesignMode;
                mnuEditDesignMode.IsCheckable       = true;
                m_lostPieceBlack.BoardDesignMode    = true;
                m_lostPieceWhite.Visibility         = System.Windows.Visibility.Hidden;
                m_chessCtl.BoardDesignMode          = true;
            }
            mnuEditDesignMode.IsChecked = (PlayingMode == PlayingModeE.DesignMode);
            SetCmdState();
        }

        /// <summary>
        /// Called when the game need to be reinitialized
        /// </summary>
        private void NewGame() {
            if (!CheckIfDirty()) {
                if (frmGameParameter.AskGameParameter(this, m_settingSearchMode)) {
                    ShowSearchMode();
                    ResetBoard();
                    StartAutomaticMove();
                }
            }
        }

        /// <summary>
        /// Load a board
        /// </summary>
        private void LoadGame() {
            if (!CheckIfDirty()) {
                m_iPuzzleGameIndex = -1;
                if (m_chessCtl.LoadFromFile()) {
                    DoAutomaticMove();
                }
            }
        }

        /// <summary>
        /// Load a puzzle
        /// </summary>
        private void LoadPuzzle() {
            frmLoadPuzzle   frm;
            PgnGame         game;

            if (!CheckIfDirty()) {
                frm = new frmLoadPuzzle(m_arrPuzzleMask);
                frm.Owner = this;
                if (frm.ShowDialog() == true) {
                    game = frm.Game;
                    m_chessCtl.CreateGameFromMove(game.StartingChessBoard,
                                                  new List<MoveExt>(),
                                                  game.StartingColor,
                                                  "White",
                                                  "Black",
                                                  PlayerTypeE.Human,
                                                  PlayerTypeE.Program,
                                                  TimeSpan.Zero,
                                                  TimeSpan.Zero);
                    PlayingMode                      = PlayingModeE.ComputerPlayBlack;
                    m_statusLabelPermutation.Content = game.Event;
                    m_iPuzzleGameIndex               = frm.GameIndex;
                    DoAutomaticMove();
                }
            }
        }

        /// <summary>
        /// Creates a game from a PGN text
        /// </summary>
        private void CreateGame() {
            if (!CheckIfDirty()) {
                m_iPuzzleGameIndex = -1;
                if (m_chessCtl.CreateFromPGNText()) {
                    PlayingMode = PlayingModeE.PlayerAgainstPlayer;
                }
            }
        }

        /// <summary>
        /// Try to connect to the FICS Chess Server
        /// </summary>
        private void ConnectToFICS() {
            FICSInterface.frmConnectToFICS  frm;

            frm             = new FICSInterface.frmConnectToFICS(m_chessCtl, m_ficsConnectionSetting);
            frm.Owner       = this;
            if (frm.ShowDialog() == true) {
                m_ficsConnection = frm.Connection;
            }
        }

        /// <summary>
        /// Observe a FICS Game
        /// </summary>
        private void ObserveFICSGame() {
            FICSInterface.frmFindBlitzGame  frmFindGame;
            FICSInterface.FICSGame          game;
            string                          strError;

            if (!CheckIfDirty()) {
                frmFindGame                         = new FICSInterface.frmFindBlitzGame(m_ficsConnection, m_searchCriteria);
                frmFindGame.Owner                   = this;
                if (frmFindGame.ShowDialog() == true) {
                    m_iPuzzleGameIndex                          = -1;
                    m_searchCriteria                            = frmFindGame.SearchCriteria;
                    game                                        = frmFindGame.Game;
                    m_toolbar.labelWhitePlayerName.Content      = "(" + game.WhitePlayer + ") :";
                    m_toolbar.labelWhitePlayerName.ToolTip      = "Rating = " + FICSInterface.FICSGame.GetHumanRating(game.WhiteRating);
                    m_toolbar.labelBlackPlayerName.Content      = "(" + game.BlackPlayer + ") :";
                    m_toolbar.labelBlackPlayerName.ToolTip      = "Rating = " + FICSInterface.FICSGame.GetHumanRating(game.BlackRating);
                    m_chessCtl.IsObservingAGame                 = true;
                    SetCmdState();
                    m_statusLabelMove.Content                   = "Waiting move from chess server...";
                    m_statusLabelPermutation.Content            = "";
                    Cursor                                      = Cursors.Wait;
                    m_toolbar.StartProgressBar();
                    if (!m_ficsConnection.ObserveGame(game, m_chessCtl, 10, m_searchCriteria.MoveTimeOut, ObserveFinished, out strError)) {
                        ObserveFinished(null, FICSInterface.TerminationE.TerminatedWithErr, "Cannot observe the game");
                    } 
                }
            }
        }

        /// <summary>
        /// Called when an observed game is finished
        /// </summary>
        /// <param name="gameIntf">         Game interface</param>
        /// <param name="eTerminationCode"> Termination code</param>
        /// <param name="strMsg">           Message</param>
        private void ObserveFinished(FICSInterface.GameIntf gameIntf, FICSInterface.TerminationE eTerminationCode, string strMsg) {
            if (Dispatcher.Thread == System.Threading.Thread.CurrentThread) {
                m_chessCtl.GameTimer.Enabled = false;
                m_toolbar.EndProgressBar();
                m_statusLabelPermutation.Content = strMsg;
                if (eTerminationCode == FICSInterface.TerminationE.TerminatedWithErr) {
                    MessageBox.Show("Error observing a game - " + strMsg, "...", MessageBoxButton.OK, MessageBoxImage.Error);
                    m_statusLabelMove.Content = "";
                } else {
                    MessageBox.Show(strMsg);
                }
                m_chessCtl.IsObservingAGame = false;
                Cursor                      = Cursors.Arrow;
                SetCmdState();
            } else {
                Dispatcher.Invoke((Action)(() => { ObserveFinished(gameIntf, eTerminationCode, strMsg); }));
            }
        }

        /// <summary>
        /// Disconnect from the FICS Chess Server
        /// </summary>
        private void DisconnectFromFICS() {
            if (m_ficsConnection != null) {
                m_ficsConnection.Dispose();
                m_ficsConnection = null;
            }
        }

        /// <summary>
        /// Cancel the auto-play
        /// </summary>
        private void CancelAutoPlay() {
            if (m_chessCtl.IsObservingAGame) {
                m_ficsConnection.TerminateObservation(m_chessCtl);
            } else if (PlayingMode == PlayingModeE.ComputerPlayBoth) {
                PlayingMode = PlayingModeE.PlayerAgainstPlayer;
            } else {
                m_chessCtl.CancelSearch();
            }
        }

        /// <summary>
        /// Toggle the player vs player mode.
        /// </summary>
        private void SelectPlayers() {
            if (frmGameParameter.AskGameParameter(this, m_settingSearchMode)) {
                ShowSearchMode();
                StartAutomaticMove();
            }
        }

        /// <summary>
        /// Filter the content of a PGN file
        /// </summary>
        private void FilterPGNFile() {
             PgnUtil    pgnUtil;
             
             pgnUtil = new PgnUtil();
             pgnUtil.CreatePGNSubsets(this);
        }

        /// <summary>
        /// Show the About Dialog Box
        /// </summary>
        public void ShowAbout() {
            frmAbout frm;

            frm = new frmAbout();
            frm.Owner = this;
            frm.ShowDialog();
        }

        /// <summary>
        /// Specifies the search mode
        /// </summary>
        private void SetSearchMode() {
            frmSearchMode   frm;

            frm         = new frmSearchMode(m_settingSearchMode, m_boardEvalUtil);
            frm.Owner   = this;
            if (frm.ShowDialog() == true) {
                frm.UpdateSearchMode();
                m_chessCtl.SearchMode = m_settingSearchMode.GetSearchMode();
                ShowSearchMode();
            }
        }

        /// <summary>
        /// Test board evaluation routine
        /// </summary>
        private void TestBoardEvaluation() {
            frmTestBoardEval    frm;
            SearchMode          searchMode;
            int                 iGameCount;

            if (!CheckIfDirty()) {
                frm         = new frmTestBoardEval(m_boardEvalUtil, m_chessCtl.SearchMode);
                frm.Owner   = this;
                if (frm.ShowDialog() == true) {
                    searchMode  = frm.SearchMode;
                    iGameCount  = frm.GameCount;
                    TestBoardEvaluation(iGameCount, searchMode);
                }
            }
        }

        /// <summary>
        /// Do the move which are done by the computer
        /// </summary>
        /// <param name="bFlashing">    true to flash moving pieces</param>
        private void DoAutomaticMove(bool bFlashing) {
            if (IsComputerMustPlay) {
                PlayComputer(bFlashing);
            }
        }

        /// <summary>
        /// Do the move which are done by the computer
        /// </summary>
        private void DoAutomaticMove() {
            DoAutomaticMove(m_chessCtl.MoveFlashing);
        }

        /// <summary>
        /// Start automatic move mode when a new game is started
        /// </summary>
        private void StartAutomaticMove() {
            if (m_ePlayingMode == PlayingModeE.ComputerPlayBoth) {
                PlayComputerAgainstComputer(m_chessCtl.MoveFlashing);
            } else {
                DoAutomaticMove();
            }
        }

        /// <summary>
        /// Toggle PGN/Move notation
        /// </summary>
        private void TogglePGNNotation() {
            bool    bPGNNotationChecked;
            
            bPGNNotationChecked      = mnuOptionPGNNotation.IsChecked;
            m_moveViewer.DisplayMode = (bPGNNotationChecked) ? MoveViewer.DisplayModeE.PGN : MoveViewer.DisplayModeE.MovePos;
        }

        /// <summary>
        /// Toggle Flash piece
        /// </summary>
        private void ToggleFlashPiece() {
            bool    bFlashPiece;

            bFlashPiece             = mnuOptionFlashPiece.IsChecked;
            m_chessCtl.MoveFlashing = bFlashPiece;
        }

        /// <summary>
        /// Choose board setting
        /// </summary>
        private void ChooseBoardSetting() {
            frmBoardSetting         frm;
            
            frm         = new frmBoardSetting(m_chessCtl.LiteCellColor, 
                                              m_chessCtl.DarkCellColor,
                                              m_chessCtl.WhitePieceColor,
                                              m_chessCtl.BlackPieceColor,
                                              m_colorBackground,
                                              m_listPieceSet,
                                              PieceSet);
            frm.Owner   = this;
            if (frm.ShowDialog() == true) {
                m_colorBackground           = frm.BackgroundColor;
                Background                  = new SolidColorBrush(m_colorBackground);
                m_chessCtl.LiteCellColor    = frm.LiteCellColor;
                m_chessCtl.DarkCellColor    = frm.DarkCellColor;
                m_chessCtl.WhitePieceColor  = frm.WhitePieceColor;
                m_chessCtl.BlackPieceColor  = frm.BlackPieceColor;
                PieceSet                    = frm.PieceSet;
            }
        }
        #endregion

        #region Sink
        /// <summary>
        /// Called each second for timer click
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            GameTimer   gameTimer;
            
            gameTimer                               = m_chessCtl.GameTimer;
            m_toolbar.labelWhitePlayTime.Content    = GameTimer.GetHumanElapse(gameTimer.WhitePlayTime);
            m_toolbar.labelBlackPlayTime.Content    = GameTimer.GetHumanElapse(gameTimer.BlackPlayTime);
            if (gameTimer.MaxWhitePlayTime.HasValue) {
                m_toolbar.labelWhiteLimitPlayTime.Content = "(" + GameTimer.GetHumanElapse(gameTimer.MaxWhitePlayTime.Value) + "/" + gameTimer.MoveIncInSec.ToString() + ")";
            }
            if (gameTimer.MaxBlackPlayTime.HasValue) {
                m_toolbar.labelBlackLimitPlayTime.Content = "(" + GameTimer.GetHumanElapse(gameTimer.MaxBlackPlayTime.Value) + "/" + gameTimer.MoveIncInSec.ToString() + ")";
            }
        }

        /// <summary>
        /// Called to gets the selected piece for design mode
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_chessCtl_QueryPiece(object sender, ChessBoardControl.QueryPieceEventArgs e) {
            e.Piece = m_lostPieceBlack.SelectedPiece;
        }

        /// <summary>
        /// Called to gets the type of pawn promotion for the current move
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_chessCtl_QueryPawnPromotionType(object sender, ChessBoardControl.QueryPawnPromotionTypeEventArgs e) {
            frmQueryPawnPromotionType   frm;
            
            frm         = new frmQueryPawnPromotionType(e.ValidPawnPromotion);
            frm.Owner   = this;
            frm.ShowDialog();
            e.PawnPromotionType = frm.PromotionType;
        }

        /// <summary>
        /// Called when FindBestMove finished its job
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void m_chessCtl_FindMoveBegin(object sender, EventArgs e) {
            m_toolbar.StartProgressBar();
        }

        /// <summary>
        /// Called when FindBestMove begin its job
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void m_chessCtl_FindMoveEnd(object sender, EventArgs e) {
            m_toolbar.EndProgressBar();
        }

        /// <summary>
        /// Called when a new move has been done in the chessboard control
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void m_chessCtl_NewMove(object sender, ChessBoardControl.NewMoveEventArgs e) {
            MoveExt             move;
            ChessBoard.PlayerE  eMoveColor;

            move        = e.Move;
            eMoveColor  = m_chessCtl.ChessBoard.LastMovePlayer;
            ShowMoveInStatusBar(eMoveColor, move);
            DisplayMessage(e.MoveResult, m_eMessageMode);
            DoAutomaticMove();
        }

        /// <summary>
        /// Called when a move is selected in the MoveViewer
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_moveViewer_NewMoveSelected(object sender, MoveViewer.NewMoveSelectedEventArg e) {
            ChessBoard.GameResultE  eResult;
            bool                    bSucceed;
            
            if (PlayingMode == PlayingModeE.PlayerAgainstPlayer) {
                eResult = m_chessCtl.SelectMove(e.NewIndex, out bSucceed);
                DisplayMessage(eResult, MessageModeE.Verbose);
                e.Cancel = !bSucceed;
            } else {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Called when the user has selected a valid move
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        void m_chessCtl_MoveSelected(object sender, ChessBoardControl.MoveSelectedEventArgs e) {
            m_chessCtl.DoUserMove(e.Move);
        }

        /// <summary>
        /// Called when the state of the commands need to be refreshed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event handler</param>
        private void m_chessCtl_UpdateCmdState(object sender, EventArgs e) {
            m_lostPieceBlack.Refresh();
            m_lostPieceWhite.Refresh();
            SetCmdState();
        }
        #endregion

    } // Class MainWindow
} // Namespace
