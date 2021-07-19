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
using System.Windows.Navigation;
using System.Windows.Shapes;
using RJCP.IO.Ports;
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using System.Globalization;

namespace uSMU_Commander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPortStream sp;
        bool calibrationMode = false;
        List<double> smuCalibrationData = new List<double>();

        public MainWindow()
        {
            InitializeComponent();
            string[] ports = SerialPortStream.GetPortNames();
            portBox.ItemsSource = ports;
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string inLine = sp.ReadLine();
            Dispatcher.Invoke(() =>
            {
                try
                {


                }
                catch (Exception ex)
                {
                    //  MessageBox.Show("Error interpreting input data");
                }
            });
        }

        private void portRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            string[] ports = SerialPortStream.GetPortNames();
            portBox.ItemsSource = ports;
        }

        private void portConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                sp = new SerialPortStream();
                sp.PortName = portBox.SelectedItem.ToString();
                sp.BaudRate = 115200;
                sp.DtrEnable = true;
                sp.RtsEnable = true;
                sp.DataReceived += DataReceived;
                sp.ErrorReceived += SerialPortError;
                sp.Open();

                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                portTextBlock.Text = "Port - Connected";
                portTextBlock.Foreground = Brushes.Green;
            }


            catch (Exception)
            {
                //MessageBox.Show("Connection failed. Is the correct port selected?");
            }
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!calibrationMode)
            {
                string received = sp.ReadLine();
                StringParser(received);
            }
            else
            {
                string received = sp.ReadLine();
                CalibrationParser(received);

            }

        }

        private void SerialPortError(object sender, SerialErrorReceivedEventArgs e)
        {
            Debug.Print("Serial error received");

            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                portTextBlock.Text = "Port - Disconnected";
                portTextBlock.Foreground = Brushes.OrangeRed;
            });
        }

        private void StringParser(string SMUInput)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                smuRecievedText.Text = SMUInput;

                if (SMUInput.Contains(","))
                {
                    try
                    {
                        string str = SMUInput.Substring(0, SMUInput.Length);
                        str = str.Replace("\0", string.Empty);

                        string[] parts = str.Split(',');

                        voltageTextBox.Text = String.Format("{0} V", parts[0]);
                        currentTextBox.Text = String.Format("{0} A", parts[1]);
                    }
                    catch
                    {
                        Debug.Print("SMU data not understood");
                    }
                }

            });

        }

        private void CalibrationParser(string SMUInput)
        {
            string str = SMUInput.Substring(0, SMUInput.Length);
            str = str.Replace("\0", string.Empty);

            double calData = Double.Parse(str);
            smuCalibrationData.Add(calData);
        }

        private void idnButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender("*IDN?");
        }

        private void enableButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender("CH1:ENA");
        }

        private void disableButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender("CH1:DIS");
        }

        private void rstButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender("*RST");
        }

        private void curSetButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender(String.Format("CH1:CUR {0}", curSetBox.Text));
        }

        private void osrSetButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender(String.Format("CH1:OSR {0}", osrSetBox.Text));

        }

        private void volSetButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender(String.Format("CH1:VOL {0}", volSetBox.Text));

        }

        private void volMeasureButton_Click(object sender, RoutedEventArgs e)
        {
            stringSender(String.Format("CH1:MEA:VOL {0}", volMeasBox.Text));
        }

        private void stringSender(string command)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                sentCommandText.Text = command;
                smuRecievedText.Clear();
                voltageTextBox.Clear();
                currentTextBox.Clear();

            });

            try
            {
                sp.WriteLine(command);

            }
            catch
            {
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {

                    portTextBlock.Text = "Port - Disconnected";
                    portTextBlock.Foreground = Brushes.OrangeRed;
                });

                MessageBox.Show("μSMU not connected. Please select the μSMU port and try again");
            }
        }

        private void portDisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
            sp.Dispose();

            portTextBlock.Text = "Port - Disconnected";
            portTextBlock.Foreground = Brushes.OrangeRed;
        }

        private void TextBlock_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            calibrationMode = true;

            for (int i = 0; i <= 52; i = i + 4)
            {
                Debug.Print("Memory location: {0}", i);
                sp.WriteLine(String.Format("*READ {0}", i));
                Thread.Sleep(50);
            }

            sp.WriteLine("*RST");
            sp.Dispose();

            calibrationMode = false;

            double[] calArray = smuCalibrationData.ToArray();


            string dacCal = String.Format("CAL:DAC {0} {1}", calArray[0], calArray[1]);

            string voltCal = String.Format("CAL:VOL {0} {1}", calArray[2], calArray[3]);

            string cur1p375Cal = String.Format("CAL:CUR:RANGE1 {0} {1}", calArray[12], calArray[13]);
            string cur8Cal = String.Format("CAL:CUR:RANGE2 {0} {1}", calArray[4], calArray[5]);
            string cur64Cal = String.Format("CAL:CUR:RANGE3 {0} {1}", calArray[6], calArray[7]);
            string cur176Cal = String.Format("CAL:CUR:RANGE4 {0} {1}", calArray[8], calArray[9]);

            string ilimCal = String.Format("CAL:ILIM {0} {1}", calArray[10], calArray[11]);

            Debug.Print(dacCal);
            Debug.Print(voltCal);
            Debug.Print(cur1p375Cal);
            Debug.Print(cur8Cal);
            Debug.Print(cur64Cal);
            Debug.Print(cur176Cal);
            Debug.Print(ilimCal);

            string[] calibrationDataArray = { dacCal, voltCal, cur1p375Cal, cur8Cal, cur64Cal, cur176Cal, ilimCal };

            CalibrationWindow CalibrationDialog = new CalibrationWindow(calibrationDataArray, portBox.SelectedItem.ToString());

            CalibrationDialog.Show();

        }

        private void sendCommandButton_Click(object sender, RoutedEventArgs e)
        {

            string toSend = sentCommandText.Text;

            if (toSend.Contains("WRITE"))
            {
                if (MessageBox.Show("This will overwrite calibration data. Are you sure you want to continue?", "Overwrite calibration data", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    Debug.Print("Aborting cal write");
                    return;
                }
                else
                {
                    stringSender(toSend);
                }
            }

            stringSender(toSend);



        }
    }
}
