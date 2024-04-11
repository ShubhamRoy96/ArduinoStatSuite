using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using LibreHardwareMonitor;
using System.IO;
using System.Reflection;

namespace ArduinoStatSuite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Functions _functions;
        public MainWindow()
        {
            InitializeComponent();
            _functions = new Functions();
            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();            
            ni.Icon = new System.Drawing.Icon(@"Resources\Main.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }

        private bool IsConnectedtoArduino { get; set; }
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnectedtoArduino)
            {
                ConnecttoArduino();
            }
            else
            {
                DisconnectFromArduino();
            }
        }

        private void ConnecttoArduino()
        {
            bool isConnected;
            isConnected = _functions.ConnectToArduino(cmbPortSelector);
            if (!isConnected)
            {
                System.Windows.MessageBox.Show("Unable to connect to selected port!", "Connection Failed", MessageBoxButton.OK);
                return;
            }
            IsConnectedtoArduino = true;
            EnableDisableControls(true);
            btnConnect.Content = "Disconnect";
            _functions.EnableDisableTelemetry(true);
        }

        

        private void DisconnectFromArduino()
        {
            bool isDisconnected;
            _functions.EnableDisableTelemetry(false);
            isDisconnected = _functions.DisconnectFromArduino();
            if (!isDisconnected)
            {
                System.Windows.MessageBox.Show("Unable to disconnect from port!", "Disconnection Failed", MessageBoxButton.OK);
                return;
            }
            IsConnectedtoArduino = false;
            EnableDisableControls(false);
            btnConnect.Content = "Connect";
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableDisableControls(false);
            SetComPorts();
            btnConnect_Click(sender, e);
        }
        void SetComPorts()
        {
            try
            {
                cmbPortSelector.ItemsSource = _functions.GetAvailableComPorts();
                cmbPortSelector.SelectedIndex = 1;
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void EnableDisableControls(bool isEnabled)
        {
            grpLEDControls.IsEnabled = isEnabled;
            chkAdvancedMode.IsEnabled = isEnabled;
            txtCustomRGB.IsEnabled = isEnabled;            
            btnApply.IsEnabled = isEnabled;
            btnReset.IsEnabled = isEnabled;
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            var red = (255 - int.Parse(txtCustomRGB.Text.Substring(0,3))).ToString().PadRight(3,'0');
            var green = (255 - int.Parse(txtCustomRGB.Text.Substring(3, 3))).ToString().PadRight(3, '0');
            var blue = (255 - int.Parse(txtCustomRGB.Text.Substring(6))).ToString().PadRight(3, '0');
            string WriteData = "#RGB" + red + green + blue + "|";
            _functions.WriteData(WriteData);
        }

        private void btnPowerON_Click(object sender, RoutedEventArgs e)
        {
            string data = "#STA|";
            _functions.WriteData(data);
            blkLEDStatus.Text = "Lights ON";
        }

        private void btnPowerOFF_Click(object sender, RoutedEventArgs e)
        {
            string data = "#STO|";
            _functions.WriteData(data);
            blkLEDStatus.Text = "Lights OFF";
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (btnConnect.Content.ToString() == "Disconnect")
            {
                btnConnect_Click(sender, null);
            }
        }
    }
}
