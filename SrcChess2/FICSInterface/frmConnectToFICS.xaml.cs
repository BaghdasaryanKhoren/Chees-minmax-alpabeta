using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SrcChess2.FICSInterface {
    /// <summary>
    /// Interaction logic for frmConnectToFICS.xaml
    /// </summary>
    public partial class frmConnectToFICS : Window {
        /// <summary>Main chess control</summary>
        private SrcChess2.ChessBoardControl m_ctlMain;
        /// <summary>Connection to the chess server</summary>
        private FICSConnection              m_conn;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionSetting">    Connection setting</param>
        /// <param name="ctlMain">              Main chessboard control</param>
        public frmConnectToFICS(SrcChess2.ChessBoardControl ctlMain, FICSConnectionSetting connectionSetting) {
            InitializeComponent();
            m_ctlMain           = ctlMain;
            ConnectionSetting   = connectionSetting;
            HostName            = connectionSetting.HostName;
            PortNumber          = connectionSetting.HostPort;
            UserName            = connectionSetting.UserName;
            Password            = "";
            IsAnonymous         = string.Compare(connectionSetting.UserName, "guest", true) == 0;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public frmConnectToFICS() : this(null, new FICSConnectionSetting()) {
        }

        /// <summary>
        /// Setting for connecting to the FICS server
        /// </summary>
        public FICSConnectionSetting ConnectionSetting {
            get;
            private set;
        }

        /// <summary>
        /// Server Host Name
        /// </summary>
        public string HostName {
            get {
                return(textBoxServerName.Text.Trim());
            }
            set {
                textBoxServerName.Text = value;
            }
        }

        /// <summary>
        /// Host port number
        /// </summary>
        public int PortNumber {
            get {
                int     iRetVal;

                if (!Int32.TryParse(textBoxServerPort.Text.Trim(), out iRetVal)) {
                    iRetVal = -1;
                }
                return(iRetVal);
            }
            set {
                textBoxServerPort.Text = value.ToString();
            }
        }

        /// <summary>
        /// Enable/disable the login info
        /// </summary>
        /// <param name="bEnable"></param>
        private void EnableLoginInfo(bool bEnable) {
            textBoxUserName.IsEnabled   = bEnable;
            textBoxPassword.IsEnabled   = bEnable;
        }

        /// <summary>
        /// Return if the connection use an anonymous login
        /// </summary>
        public bool IsAnonymous {
            get {
                return(radioAnonymous.IsChecked == true);
            }
            set {
                if (value) {
                    radioAnonymous.IsChecked = true;
                    EnableLoginInfo(false);
                } else {
                    radioRated.IsChecked = true;
                    EnableLoginInfo(true);
                }
            }
        }

        /// <summary>
        /// Gets the user name
        /// </summary>
        public string UserName {
            get {
                string  strRetVal;

                if (IsAnonymous) {
                    strRetVal = "Guest";
                } else {
                    strRetVal = textBoxUserName.Text.Trim();
                }
                return(strRetVal);
            }
            set {
                textBoxUserName.Text = value;
            }
        }

        /// <summary>
        /// User password
        /// </summary>
        public string Password {
            private get {
                string  strRetVal;

                if (IsAnonymous) {
                    strRetVal   = "";
                } else {
                    strRetVal   = textBoxPassword.Password.Trim();
                }
                return(strRetVal);
            }
            set {
                textBoxPassword.Password = value;
            }
        }

        /// <summary>
        /// Connection to the FICS Chess Server
        /// </summary>
        public FICSConnection Connection {
            get {
                return(m_conn);
            }
        }

        /// <summary>
        /// Update the state of the OK button
        /// </summary>
        private void UpdateButtonState() {
            bool    bEnableOk;

            bEnableOk       = (!String.IsNullOrEmpty(HostName) && PortNumber >= 0) &&
                               (IsAnonymous || (!String.IsNullOrEmpty(UserName) && !String.IsNullOrEmpty(Password)));
            butOk.IsEnabled = bEnableOk;
        }

        /// <summary>
        /// Called when a textbox content change
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void textBox_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateButtonState();
        }

        /// <summary>
        /// Called when a password content change
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void textBoxPassword_PasswordChanged(object sender, RoutedEventArgs e) {
            UpdateButtonState();
        }

        /// <summary>
        /// Called when Radio button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void radio_Checked(object sender, RoutedEventArgs e) {
            EnableLoginInfo(radioRated.IsChecked == true);
            UpdateButtonState();
        }

        /// <summary>
        /// Called when connection has succeed or failed
        /// </summary>
        /// <param name="bSucceed"> true if succeed</param>
        /// <param name="conn">     Connection if any</param>
        /// <param name="strError"> Error if any</param>
        private void ConnectionDone(bool bSucceed, FICSConnection conn, string strError) {
            ProgressBar.Stop();
            ProgressBar.Visibility = Visibility.Hidden;
            if (bSucceed) {
                m_conn       = conn;
                MessageBox.Show("Connected to FICS Server");
                DialogResult = true;
            } else {
                MessageBox.Show(strError);
                butOk.IsEnabled     = true;
                butCancel.IsEnabled = true;

            }
        }

        /// <summary>
        /// Try to connect to the server
        /// </summary>
        /// <param name="strHostName">  Host name</param>
        /// <param name="iPortNumber">  Port number</param>
        /// <param name="strUserName">  User name</param>
        /// <param name="strPassword">  Password</param>
        private void InitializeConnection(string strHostName, int iPortNumber, string strUserName, string strPassword) {
            FICSConnection          conn;
            string                  strError;

            ConnectionSetting.HostName  = strHostName;
            ConnectionSetting.HostPort  = iPortNumber;
            ConnectionSetting.Anonymous = String.Compare(strUserName, "guest", true) == 0;
            ConnectionSetting.UserName  = strUserName;
            conn                        = new FICSConnection(m_ctlMain, ConnectionSetting);
            if (!conn.Login(strPassword, 10, out strError)) {
                conn.Dispose();
                Dispatcher.Invoke((Action)(() => {ConnectionDone(false /*bSucceed*/, null /*conn*/, strError); }));
            } else {
                Dispatcher.Invoke((Action)(() => {ConnectionDone(true /*bSucceed*/, conn, null /*strError*/); }));
            }
        }

        /// <summary>
        /// Called when a Ok button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            string  strHostName;
            int     iPortNumber;
            string  strUserName;
            string  strPassword;

            strHostName             = HostName;
            iPortNumber             = PortNumber;
            strUserName             = UserName;
            strPassword             = Password;
            ProgressBar.Visibility  = Visibility.Visible;
            ProgressBar.Start();
            butOk.IsEnabled         = false;
            butCancel.IsEnabled     = false;
            System.Threading.Tasks.Task.Factory.StartNew(() => {  InitializeConnection(strHostName, iPortNumber, strUserName, strPassword); });
        }

        /// <summary>
        /// Called when Cancel button is pressed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void butCancel_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }
    }
}
