using System.Windows;

namespace SrcChess2 {
    /// <summary>
    /// Pickup Game Parameter from the player
    /// </summary>
    public partial class frmGameParameter : Window {
        /// <summary>Parent Window</summary>
        private MainWindow          ParentWindow { get; set; }
        /// <summary>Search mode</summary>
        private SettingSearchMode   SettingSearchMode;


        /// <summary>
        /// Class Ctor
        /// </summary>
        public frmGameParameter() {
            InitializeComponent();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="parent">               Parent Window</param>
        /// <param name="settingSearchMode">    Search mode</param>
        private frmGameParameter(MainWindow parent, SettingSearchMode settingSearchMode) : this() {
            ParentWindow        = parent;
            SettingSearchMode   = settingSearchMode;
            switch(ParentWindow.PlayingMode) {
            case MainWindow.PlayingModeE.DesignMode:
                throw new System.ApplicationException("Must not be called in design mode.");
            case MainWindow.PlayingModeE.ComputerPlayWhite:
            case MainWindow.PlayingModeE.ComputerPlayBlack:
                radioButtonPlayerAgainstComputer.IsChecked = true;
                radioButtonPlayerAgainstComputer.Focus();
                break;
            case MainWindow.PlayingModeE.PlayerAgainstPlayer:
                radioButtonPlayerAgainstPlayer.IsChecked = true;
                radioButtonPlayerAgainstPlayer.Focus();
                break;
            case MainWindow.PlayingModeE.ComputerPlayBoth:
                radioButtonComputerAgainstComputer.IsChecked = true;
                radioButtonComputerAgainstComputer.Focus();
                break;
            }
            if (ParentWindow.PlayingMode == MainWindow.PlayingModeE.ComputerPlayBlack) { 
                radioButtonComputerPlayBlack.IsChecked = true;
            } else {
                radioButtonComputerPlayWhite.IsChecked = true;
            }
            switch (SettingSearchMode.DifficultyLevel) {
            case SettingSearchMode.DifficultyLevelE.Manual:
                radioButtonLevelManual.IsChecked = true;
                break;
            case SettingSearchMode.DifficultyLevelE.VeryEasy:
                radioButtonLevel1.IsChecked = true;
                break;
            case SettingSearchMode.DifficultyLevelE.Easy:
                radioButtonLevel2.IsChecked = true;
                break;
            case SettingSearchMode.DifficultyLevelE.Intermediate:
                radioButtonLevel3.IsChecked = true;
                break;
            case SettingSearchMode.DifficultyLevelE.Hard:
                radioButtonLevel4.IsChecked = true;
                break;
            case SettingSearchMode.DifficultyLevelE.VeryHard:
                radioButtonLevel5.IsChecked = true;
                break;
            default:
                radioButtonLevel1.IsChecked = true;
                break;
            }
            CheckState();
            radioButtonLevel1.ToolTip       = SettingSearchMode.ModeTooltip(SettingSearchMode.DifficultyLevelE.VeryEasy);
            radioButtonLevel2.ToolTip       = SettingSearchMode.ModeTooltip(SettingSearchMode.DifficultyLevelE.Easy);
            radioButtonLevel3.ToolTip       = SettingSearchMode.ModeTooltip(SettingSearchMode.DifficultyLevelE.Intermediate);
            radioButtonLevel4.ToolTip       = SettingSearchMode.ModeTooltip(SettingSearchMode.DifficultyLevelE.Hard);
            radioButtonLevel5.ToolTip       = SettingSearchMode.ModeTooltip(SettingSearchMode.DifficultyLevelE.VeryHard);
            radioButtonLevelManual.ToolTip  = SettingSearchMode.ModeTooltip(SettingSearchMode.DifficultyLevelE.Manual);
        }

        /// <summary>
        /// Check the state of the group box
        /// </summary>
        private void CheckState() {
            groupBoxComputerPlay.IsEnabled = radioButtonPlayerAgainstComputer.IsChecked.Value;
        }

        /// <summary>
        /// Called to accept the form
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            if (radioButtonPlayerAgainstComputer.IsChecked == true) {
                ParentWindow.PlayingMode = (radioButtonComputerPlayBlack.IsChecked == true) ? MainWindow.PlayingModeE.ComputerPlayBlack : MainWindow.PlayingModeE.ComputerPlayWhite;
            } else if (radioButtonPlayerAgainstPlayer.IsChecked == true) {
                ParentWindow.PlayingMode = MainWindow.PlayingModeE.PlayerAgainstPlayer;
            } else if (radioButtonComputerAgainstComputer.IsChecked == true) {
                ParentWindow.PlayingMode = MainWindow.PlayingModeE.ComputerPlayBoth;
            }
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Called when the radio button value is changed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event arguments</param>
        private void radioButtonOpponent_CheckedChanged(object sender, RoutedEventArgs e) {
            CheckState();
        }

        /// <summary>
        /// Ask for the game parameter
        /// </summary>
        /// <param name="parent">               Parent window</param>
        /// <param name="settingSearchMode">    Search mode</param>
        /// <returns>
        /// true if succeed
        /// </returns>
        public static bool AskGameParameter(MainWindow parent, SettingSearchMode settingSearchMode) {
            bool                bRetVal;
            frmGameParameter    frm;
            
            frm         = new frmGameParameter(parent, settingSearchMode);
            frm.Owner   = parent;
            bRetVal     = (frm.ShowDialog() == true);
            if (bRetVal) {
                if (frm.radioButtonLevel1.IsChecked == true) {
                    settingSearchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.VeryEasy;
                } else if (frm.radioButtonLevel2.IsChecked == true) {
                    settingSearchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.Easy;
                } else if (frm.radioButtonLevel3.IsChecked == true) {
                    settingSearchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.Intermediate;
                } else if (frm.radioButtonLevel4.IsChecked == true) {
                    settingSearchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.Hard;
                } else if (frm.radioButtonLevel5.IsChecked == true) {
                    settingSearchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.VeryHard;
                } else if (frm.radioButtonLevelManual.IsChecked == true) {
                    settingSearchMode.DifficultyLevel = SettingSearchMode.DifficultyLevelE.Manual;
                }
                frm.ParentWindow.m_chessCtl.SearchMode = settingSearchMode.GetSearchMode();
            }
            return(bRetVal);
        }
    } // Class frmGameParameter
} // Namespace
