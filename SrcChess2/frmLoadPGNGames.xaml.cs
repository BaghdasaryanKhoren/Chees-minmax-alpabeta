using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static SrcChess2.PgnParser;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmLoadPGNGames.xaml
    /// </summary>
    public partial class frmLoadPGNGames : Window {
        /// <summary>Processed file name</summary>
        private string          m_strFileName;
        /// <summary>Task used to process the file</summary>
        private Task<bool>      m_task;
        /// <summary>Total skipped games</summary>
        private int             m_iTotalSkipped;
        /// <summary>Total truncated games</summary>
        private int             m_iTotalTruncated;
        /// <summary>Error if any</summary>
        private string          m_strError;
        /// <summary>Actual phase</summary>
        private ParsingPhaseE   m_ePhase;
        /// <summary>PGN parsing result</summary>
        private bool            m_bResult;
        /// <summary>PGN games</summary>
        private List<PgnGame>   m_pgnGames;
        /// <summary>PGN parser</summary>
        private PgnParser       m_pgnParser;
        /// <summary>Private delegate</summary>
        private delegate void   delProgressCallBack(ParsingPhaseE ePhase, int iFileIndex, int iFileCount, string strFileName, int iGameDone, int iGameCount);

        /// <summary>
        /// Ctor
        /// </summary>
        public frmLoadPGNGames() {
            InitializeComponent();
            Loaded      += PGNParsing_Loaded;
            Unloaded    += PGNParsing_Unloaded;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public frmLoadPGNGames(string strFileName) : this() {
            m_strFileName = strFileName;
        }

        /// <summary>
        /// Called when the windows is loaded
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void PGNParsing_Loaded(object sender, RoutedEventArgs e) {
            ProgressBar.Start();
            StartProcessing();
        }

        /// <summary>
        /// Called when the windows is closing
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void PGNParsing_Unloaded(object sender, RoutedEventArgs e) {
            ProgressBar.Stop();
        }

        /// <summary>
        /// List of PGN games read from the file
        /// </summary>
        public List<PgnGame> PGNGames {
            get {
                return(m_pgnGames);
            }
        }

        /// <summary>
        /// PGN Parser
        /// </summary>
        public PgnParser PGNParser {
            get {
                return(m_pgnParser);
            }
        }

        /// <summary>
        /// Total number of games skipped
        /// </summary>
        public int TotalSkipped {
            get {
                return(m_iTotalSkipped);
            }
        }

        /// <summary>
        /// Total number of games truncated
        /// </summary>
        public int TotalTruncated {
            get {
                return(m_iTotalTruncated);
            }
        }

        /// <summary>
        /// Error if any
        /// </summary>
        public string Error {
            get {
                return(m_strError);
            }
        }

        /// <summary>
        /// Cancel the parsing job
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butCancel_Click(object sender, RoutedEventArgs e) {
            butCancel.IsEnabled = false;
            PgnParser.CancelParsingJob();
        }

        /// <summary>
        /// Progress bar
        /// </summary>
        /// <param name="ePhase">       Phase</param>
        /// <param name="iFileIndex">   File index</param>
        /// <param name="iFileCount">   File count</param>
        /// <param name="strFileName">  File name</param>
        /// <param name="iGameDone">    Games processed since the last call</param>
        /// <param name="iGameCount">   Game count</param>
        private void WndCallBack(ParsingPhaseE ePhase, int iFileIndex, int iFileCount, string strFileName, int iGameDone, int iGameCount) {
            if (m_ePhase != ePhase) {
                switch (ePhase) {
                case ParsingPhaseE.OpeningFile:
                    ctlPhase.Content                = "Openning the file";
                    ctlFileBeingProcessed.Content   = System.IO.Path.GetFileName(strFileName);
                    ctlStep.Content                 = "";
                    break;
                case ParsingPhaseE.ReadingFile:
                    ctlPhase.Content                = "Reading the file content into memory";
                    ctlStep.Content                 = "";
                    break;
                case ParsingPhaseE.RawParsing:
                    ctlPhase.Content                = "Parsing the PGN";
                    ctlStep.Content                 = "0 / " + iGameCount.ToString() + "mb";
                    break;
                case ParsingPhaseE.Finished:
                    ctlPhase.Content                = "Done";
                    break;
                default:
                    break;
                }
                m_ePhase = ePhase;
            }
            switch (ePhase) {
            case ParsingPhaseE.OpeningFile:
                break;
            case ParsingPhaseE.ReadingFile:
                ctlPhase.Content    = "Reading the file content into memory";
                break;
            case ParsingPhaseE.RawParsing:
                ctlStep.Content = iGameDone.ToString() + " / " + iGameCount.ToString() + " mb";
                break;
            case ParsingPhaseE.Finished:
                if (PgnParser.IsJobCancelled) {
                    DialogResult = false;
                } else {
                    DialogResult = m_bResult;
                }
                break;
            default:
                break;
            }
        }

        /// <summary>
        /// Progress bar
        /// </summary>
        /// <param name="cookie">           Cookie</param>
        /// <param name="ePhase">           Phase</param>
        /// <param name="iFileIndex">       File index</param>
        /// <param name="iFileCount">       File count</param>
        /// <param name="strFileName">      File name</param>
        /// <param name="iGameProcessed">   Games processed since the last call</param>
        /// <param name="iGameCount">       Game count</param>
        static void ProgressCallBack(object cookie, ParsingPhaseE ePhase, int iFileIndex, int iFileCount, string strFileName, int iGameProcessed, int iGameCount) {
            frmLoadPGNGames     frm;
            delProgressCallBack del;

            frm = (frmLoadPGNGames)cookie;
            del = frm.WndCallBack;
            frm.Dispatcher.Invoke(del, System.Windows.Threading.DispatcherPriority.Normal, new object[] { ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount });
        }

        /// <summary>
        /// Load the PGN games from the specified file
        /// </summary>
        /// <returns></returns>
        private bool LoadPGN() {
            bool            bRetVal;

            try {
                m_iTotalSkipped     = 0;
                m_iTotalTruncated   = 0;
                m_strError          = null;
                m_ePhase            = ParsingPhaseE.None;
                m_pgnParser         = new PgnParser(false /*bDiagnose*/);
                bRetVal             = m_pgnParser.InitFromFile(m_strFileName);
                if (bRetVal) {
                    m_pgnGames = m_pgnParser.GetAllRawPGN(true /*bAttrList*/,
                                                          false /*bMoveList*/,
                                                          out m_iTotalSkipped,
                                                          (cookie, ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount) => { ProgressCallBack(cookie, ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount); },
                                                          this);
                    bRetVal    = m_pgnGames != null;
                }
            } catch(System.Exception ex) {
                MessageBox.Show(ex.Message);
                bRetVal = false;
            }
            m_bResult  = bRetVal;
            ProgressCallBack(this, ParsingPhaseE.Finished, 0, 0, null, 0, 0);
            return(bRetVal);
        }

        private void StartProcessing() {
            m_task = Task<bool>.Factory.StartNew(() => { return(LoadPGN()); });
        }
    }
}
