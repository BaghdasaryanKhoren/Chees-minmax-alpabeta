using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SrcChess2.PgnParser;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for wndPGNParsing.xaml
    /// </summary>
    public partial class frmCreatingBookFromPGN : Window {
        /// <summary>Task used to process the file</summary>
        private Task<bool>      m_task;
        /// <summary>Array of file names</summary>
        private string[]        m_arrFileNames;
        /// <summary>Total skipped games</summary>
        private int             m_iTotalSkipped;
        /// <summary>Total truncated games</summary>
        private int             m_iTotalTruncated;
        /// <summary>Error if any</summary>
        private string          m_strError;
        /// <summary>Actual phase</summary>
        private ParsingPhaseE   m_ePhase;
        /// <summary>Book creation result</summary>
        private bool            m_bResult;
        /// <summary>List of moves for all games</summary>
        private List<short[]>   m_listMoveList;
        /// <summary>Private delegate</summary>
        private delegate void   delProgressCallBack(ParsingPhaseE ePhase, int iFileIndex, int iFileCount, string strFileName, int iGameDone, int iGameCount);

        /// <summary>
        /// Ctor
        /// </summary>
        public frmCreatingBookFromPGN() {
            InitializeComponent();
            Loaded      += WndPGNParsing_Loaded;
            Unloaded    += WndPGNParsing_Unloaded;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public frmCreatingBookFromPGN(string[] arrFileNames) : this() {
            m_arrFileNames  = arrFileNames;
        }

        /// <summary>
        /// Called when the windows is loaded
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void WndPGNParsing_Loaded(object sender, RoutedEventArgs e) {
            ProgressBar.Start();
            StartProcessing();
        }

        /// <summary>
        /// Called when the windows is closing
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void WndPGNParsing_Unloaded(object sender, RoutedEventArgs e) {
            ProgressBar.Stop();
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
        /// Created openning book
        /// </summary>
        public Book Book {
            get;
            private set;
        }

        /// <summary>
        /// Number of entries in the book
        /// </summary>
        public int BookEntryCount {
            get;
            private set;
        }

        /// <summary>
        /// List of moves of all games
        /// </summary>
        public List<short[]> ListMoveList {
            get {
                return(m_listMoveList);
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
                case ParsingPhaseE.CreatingBook:
                    ctlPhase.Content                = "Creating the book entries";
                    ctlFileBeingProcessed.Content   = "***";
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
            case ParsingPhaseE.CreatingBook:
                ctlStep.Content = iGameDone.ToString() + " / " + iGameCount.ToString();
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
            frmCreatingBookFromPGN  wnd;
            delProgressCallBack     del;

            wnd = (frmCreatingBookFromPGN)cookie;
            del = wnd.WndCallBack;
            wnd.Dispatcher.Invoke(del, System.Windows.Threading.DispatcherPriority.Normal, new object[] { ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount });
        }

        /// <summary>
        /// Create a book from a list of PGN games
        /// </summary>
        /// <returns></returns>
        private bool CreateBook() {
            bool        bRetVal;

            try {
                m_iTotalSkipped     = 0;
                m_iTotalTruncated   = 0;
                m_strError          = null;
                m_ePhase            = ParsingPhaseE.None;
                bRetVal             = PgnParser.ExtractMoveListFromMultipleFiles(m_arrFileNames,
                                                                                 true /*bNoAttr*/,
                                                                                 (cookie, ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount) => { ProgressCallBack(cookie, ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount); },
                                                                                 this,
                                                                                 out m_listMoveList,
                                                                                 out m_iTotalSkipped,
                                                                                 out m_iTotalTruncated,
                                                                                 out m_strError);
                if (bRetVal) {
                    Book            = new Book();
                    BookEntryCount  = Book.CreateBookList(m_listMoveList,
                                                          30 /*iMinMoveCount*/,
                                                          10 /*iMaxDepth*/,
                                                          (cookie, ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount) => { ProgressCallBack(cookie, ePhase, iFileIndex, iFileCount, strFileName, iGameProcessed, iGameCount); },
                                                          this);
                }
            } catch(System.Exception ex) {
                MessageBox.Show(ex.Message);
                bRetVal = false;
            }
            m_bResult   = bRetVal;
            ProgressCallBack(this, ParsingPhaseE.Finished, 0, 0, null, 0, 0);
            return(bRetVal);
        }

        private void StartProcessing() {
            m_task = Task<bool>.Factory.StartNew(() => { return(CreateBook()); });
        }
    }
}
