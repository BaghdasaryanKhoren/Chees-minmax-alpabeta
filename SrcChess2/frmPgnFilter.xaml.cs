using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmPgnFilter.xaml
    /// </summary>
    public partial class frmPgnFilter : Window {
        
        /// <summary>Represent an ELO range in the checked list control</summary>
        private class RangeItem {
            public int              m_iRange;
            public                  RangeItem(int iRange) { m_iRange = iRange; }
            public override string  ToString() {
                return("Range " + m_iRange.ToString() + " - " + (m_iRange + 99).ToString());
            }
        }
        
        /// <summary>Clause use to filter PGN games</summary>
        public PgnUtil.FilterClause     m_filterClause;
        /// <summary>PGN Parser</summary>
        private PgnParser               m_pgnParser;
        /// <summary>PGN utility class</summary>
        private PgnUtil                 m_pgnUtil;
        /// <summary>PGN games without move list</summary>
        private List<PgnGame>           m_pgnGames;

        /// <summary>
        /// Class Ctor
        /// </summary>
        public frmPgnFilter() {
            InitializeComponent();
            m_filterClause = new PgnUtil.FilterClause();

        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="pgnParser">        PGN parser</param>
        /// <param name="pgnUtil">          PGN utility class</param>
        /// <param name="pgnGames">         Raw games</param>
        /// <param name="iMinELO">          Minimum ELO in the PGN file</param>
        /// <param name="iMaxELO">          Maximum ELO in the PGN file</param>
        /// <param name="arrPlayers">       List of players found in the PGN file</param>
        /// <param name="strInpFileName">   Name of the input file.</param>
        public frmPgnFilter(PgnParser pgnParser, PgnUtil pgnUtil, List<PgnGame> pgnGames, int iMinELO, int iMaxELO, string[] arrPlayers, string strInpFileName) : this() {
            CheckBox    checkBox;

            m_pgnParser = pgnParser;
            m_pgnUtil   = pgnUtil;
            m_pgnGames  = pgnGames;
            iMinELO     = iMinELO / 100 * 100;
            listBoxRange.Items.Clear();
            for (int iIndex = iMinELO; iIndex < iMaxELO; iIndex += 100) {
                checkBox            = new CheckBox();
                checkBox.Content    = new RangeItem(iIndex);
                checkBox.IsChecked  = true;
                listBoxRange.Items.Add(checkBox);
            }
            listBoxPlayer.Items.Clear();
            foreach (string strPlayer in arrPlayers) {
                checkBox            = new CheckBox();
                checkBox.Content    = strPlayer;
                checkBox.IsChecked  = true;
                listBoxPlayer.Items.Add(checkBox);
            }
            listBoxEnding.Items.Clear();
            checkBox            = new CheckBox();
            checkBox.Content    = "White Win";
            checkBox.IsChecked  = true;
            listBoxEnding.Items.Add(checkBox);
            checkBox            = new CheckBox();
            checkBox.Content    = "Black Win";
            checkBox.IsChecked  = true;
            listBoxEnding.Items.Add(checkBox);
            checkBox            = new CheckBox();
            checkBox.Content    = "Draws";
            checkBox.IsChecked  = true;
            listBoxEnding.Items.Add(checkBox);
            checkBoxAllRanges.IsChecked     = true;
            checkBoxAllPlayer.IsChecked     = true;
            checkBoxAllEndGame.IsChecked    = true;
            listBoxPlayer.IsEnabled         = false;
            listBoxRange.IsEnabled          = false;
            listBoxEnding.IsEnabled         = false;
            labelDesc.Content               = pgnGames.Count.ToString() + " games found in the file '" + strInpFileName + "'";
        }

        /// <summary>
        /// Checks or unchecks all items in a checked list control
        /// </summary>
        /// <param name="listBox">      Control</param>
        /// <param name="bChecked">     true to check, false to uncheck</param>
        private void CheckAllItems(ListBox listBox, bool bChecked) {
            foreach (CheckBox checkBox in listBox.Items.OfType<CheckBox>()) {
                checkBox.IsChecked  = bChecked;
            }
        }

        /// <summary>
        /// Gets the number of checked item
        /// </summary>
        /// <param name="listBox">      Control</param>
        private int GetCheckedCount(ListBox listBox) {
            int     iCount;
            
            iCount = listBox.Items.OfType<CheckBox>().Count(x => (x.IsChecked == true));
            return(iCount);
        }

        /// <summary>
        /// Gets and validates information coming from the user
        /// </summary>
        /// <returns>
        /// true if validation is ok, false if not
        /// </returns>
        private bool SyncInfo() {
            bool        bRetVal = true;
            int         iRangeCheckedCount;
            int         iPlayerCheckedCount;
            int         iEndGameCount;
            RangeItem   rangeItem;

            iRangeCheckedCount  = GetCheckedCount(listBoxRange);
            if (checkBoxAllRanges.IsChecked == true || iRangeCheckedCount == listBoxRange.Items.Count) {
                m_filterClause.m_bAllRanges  = true;
                m_filterClause.m_hashRanges  = null;
            } else {
                m_filterClause.m_bAllRanges = false;
                if (iRangeCheckedCount == 0 && checkBoxIncludeUnrated.IsChecked == false) {
                    MessageBox.Show("At least one range must be selected.");
                    bRetVal = false;
                } else {
                    m_filterClause.m_hashRanges = new Dictionary<int,int>(iRangeCheckedCount);
                    foreach (CheckBox checkBox in listBoxRange.Items.OfType<CheckBox>().Where(x => x.IsChecked == true)) {
                        rangeItem   = checkBox.Content as RangeItem;
                        m_filterClause.m_hashRanges.Add(rangeItem.m_iRange, 0);
                    }
                }
            }
            iPlayerCheckedCount = GetCheckedCount(listBoxPlayer);
            m_filterClause.m_bIncludesUnrated = checkBoxIncludeUnrated.IsChecked.Value;
            if (checkBoxAllPlayer.IsChecked == true || iPlayerCheckedCount == listBoxPlayer.Items.Count) {
                m_filterClause.m_bAllPlayers    = true;
                m_filterClause.m_hashPlayerList = null;
            } else {
                m_filterClause.m_bAllPlayers    = false;
                if (iPlayerCheckedCount == 0) {
                    MessageBox.Show("At least one player must be selected.");
                    bRetVal = false;
                } else {
                    m_filterClause.m_hashPlayerList = new Dictionary<string,string>(iPlayerCheckedCount);
                    foreach (CheckBox checkBox in listBoxPlayer.Items.OfType<CheckBox>().Where(x => x.IsChecked == true)) {
                        m_filterClause.m_hashPlayerList.Add(checkBox.Content as String, null);
                    }
                }
            }
            iEndGameCount   = GetCheckedCount(listBoxEnding);
            if (checkBoxAllEndGame.IsChecked == true || iEndGameCount == listBoxEnding.Items.Count) {
                m_filterClause.m_bAllEnding            = true;
                m_filterClause.m_bEndingWhiteWinning   = true;
                m_filterClause.m_bEndingBlackWinning   = true;
                m_filterClause.m_bEndingDraws          = true;
            } else {
                m_filterClause.m_bAllEnding            = false;
                if (iEndGameCount == 0) {
                    MessageBox.Show("At least one ending must be selected.");
                    bRetVal = false;
                } else {
                    m_filterClause.m_bEndingWhiteWinning   = ((CheckBox)listBoxEnding.Items[0]).IsChecked.Value;
                    m_filterClause.m_bEndingBlackWinning   = ((CheckBox)listBoxEnding.Items[1]).IsChecked.Value;
                    m_filterClause.m_bEndingDraws          = ((CheckBox)listBoxEnding.Items[2]).IsChecked.Value;
                }
            }
            if (m_filterClause.m_bAllRanges     &&
                m_filterClause.m_bAllPlayers    &&
                m_filterClause.m_bAllEnding     &&
                m_filterClause.m_bIncludesUnrated) {
                MessageBox.Show("At least one filtering option must be selected.");
                bRetVal = false;
            }
            return(bRetVal);
        }

        /// <summary>
        /// Clause use to filter the PGN file has defined by the user. Valid after the Ok button has been clicked.
        /// </summary>
        public PgnUtil.FilterClause FilteringClause {
            get {
                return(m_filterClause);
            }
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            if (SyncInfo()) {
                m_pgnUtil.CreateSubsetPGN(m_pgnParser,
                                          m_pgnGames,
                                          m_filterClause);
            }
        }

        /// <summary>
        /// Called when the Ok button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butTest_Click(object sender, RoutedEventArgs e) {
            int     iCount;
            
            if (SyncInfo()) {
                iCount = m_pgnUtil.FilterPGN(m_pgnParser, m_pgnGames, null, FilteringClause);
                MessageBox.Show("The specified filter will result in " + iCount.ToString() + " game(s) selected.");
            }
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butSelectAllRange_Click(object sender, RoutedEventArgs e) {
            CheckAllItems(listBoxRange, true);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butClearAllRange_Click(object sender, RoutedEventArgs e) {
            CheckAllItems(listBoxRange, false);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butSelectAllPlayers_Click(object sender, RoutedEventArgs e) {
            CheckAllItems(listBoxPlayer, true);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butClearAllPlayers_Click(object sender, RoutedEventArgs e) {
            CheckAllItems(listBoxPlayer, false);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butSelectAllEndGame_Click(object sender, RoutedEventArgs e) {
            CheckAllItems(listBoxEnding, true);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        /// <param name="sender">           Sender object</param>
        /// <param name="e">                Event argument</param>
        private void butClearAllEndGame_Click(object sender, RoutedEventArgs e) {
            CheckAllItems(listBoxEnding, false);
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void checkBoxAllRanges_CheckedChanged() {
            listBoxRange.IsEnabled      = !checkBoxAllRanges.IsChecked.Value;
            butClearAllRange.IsEnabled  = !checkBoxAllRanges.IsChecked.Value;
            butSelectAllRange.IsEnabled = !checkBoxAllRanges.IsChecked.Value;
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void checkBoxAllPlayer_CheckedChanged() {
            listBoxPlayer.IsEnabled         = !(bool)checkBoxAllPlayer.IsChecked;
            butClearAllPlayers.IsEnabled    = !(bool)checkBoxAllPlayer.IsChecked;
            butSelectAllPlayers.IsEnabled   = !(bool)checkBoxAllPlayer.IsChecked;
        }

        /// <summary>
        /// Called when the button is clicked
        /// </summary>
        private void checkBoxAllEndGame_CheckedChanged() {
            listBoxEnding.IsEnabled         = !(bool)checkBoxAllEndGame.IsChecked;
            butClearAllEndGame.IsEnabled    = !(bool)checkBoxAllEndGame.IsChecked;
            butSelectAllEndGame.IsEnabled   = !(bool)checkBoxAllEndGame.IsChecked;
        }

        /// <summary>
        /// Called when the All Range checkbox is checked
        /// </summary>
        /// <param name="sender">   sender object</param>
        /// <param name="e">        event argument</param>
        private void checkBoxAllRanges_Checked(object sender, RoutedEventArgs e) {
            checkBoxAllRanges_CheckedChanged();
        }

        /// <summary>
        /// Called when the All Ranges checkbox is unchecked
        /// </summary>
        /// <param name="sender">   sender object</param>
        /// <param name="e">        event argument</param>
        private void checkBoxAllRanges_Unchecked(object sender, RoutedEventArgs e) {
            checkBoxAllRanges_CheckedChanged();
        }

        /// <summary>
        /// Called when the All Players checkbox is checked
        /// </summary>
        /// <param name="sender">   sender object</param>
        /// <param name="e">        event argument</param>
        private void checkBoxAllPlayer_Checked(object sender, RoutedEventArgs e) {
            checkBoxAllPlayer_CheckedChanged();
        }

        /// <summary>
        /// Called when the All Players checkbox is unchecked
        /// </summary>
        /// <param name="sender">   sender object</param>
        /// <param name="e">        event argument</param>
        private void checkBoxAllPlayer_Unchecked(object sender, RoutedEventArgs e) {
            checkBoxAllPlayer_CheckedChanged();
        }

        /// <summary>
        /// Called when the All End Games checkbox is checked
        /// </summary>
        /// <param name="sender">   sender object</param>
        /// <param name="e">        event argument</param>
        private void checkBoxAllEndGame_Checked(object sender, RoutedEventArgs e) {
            checkBoxAllEndGame_CheckedChanged();
        }

        /// <summary>
        /// Called when the All End Games checkbox is unchecked
        /// </summary>
        /// <param name="sender">   sender object</param>
        /// <param name="e">        event argument</param>
        private void checkBoxAllEndGame_Unchecked(object sender, RoutedEventArgs e) {
            checkBoxAllEndGame_CheckedChanged();
        }
    } // Class frmPgnFilter
} // Namespace
