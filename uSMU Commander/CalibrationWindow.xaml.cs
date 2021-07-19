using Microsoft.Win32;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace uSMU_Commander
{
    /// <summary>
    /// Interaction logic for CalibrationWindow.xaml
    /// </summary>
    public partial class CalibrationWindow : Window
    {
        string[] calibrationData;
        SerialPortStream sp = new SerialPortStream();
        string smuPortName;

        public CalibrationWindow(string[] calibrationDataInput, string portName)
        {
            InitializeComponent();
            calibrationData = calibrationDataInput;
            smuPortName = portName;

            foreach (string item in calibrationData)
            {
                calibrationSettings.Text += item;
                calibrationSettings.Text += "\n";
            }
        }

        private void exportCalButton_Click(object sender, RoutedEventArgs e)
        {

            var csv = new StringBuilder();

            for (int i = 0; i < calibrationData.Length; i++)
            {
                var newLine = string.Format("{0}", calibrationData[i]);
                csv.AppendLine(newLine);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Comma separated value (*.csv)|*.csv";

            if (saveFileDialog.ShowDialog() == true)

                File.WriteAllText(saveFileDialog.FileName, csv.ToString());

        }

        private void importCalButton_Click(object sender, RoutedEventArgs e)
        {
            string[] calibrationDataToWrite;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {

            }

            try
            {
                var lines = File.ReadLines(openFileDialog.FileName);

                string[] read_lines;
                List<string> read_lines_list = new List<string>();

                foreach (string line in lines)
                {

                    read_lines_list.Add(line);

                }

                calibrationDataToWrite = read_lines_list.ToArray();

                ConnectPort(smuPortName);


                if (MessageBox.Show("This will overwrite calibration data. Are you sure you want to continue?", "Overwrite calibration data", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    Debug.Print("Aborting cal write");
                }
                else
                {
                    SendCalibrationData(calibrationDataToWrite);
                }



            }
            catch
            {
                MessageBox.Show("There was a problem importing calibration data");
            }

        }

        private void ConnectPort(string portName)
        {
            try
            {
                sp = new SerialPortStream();
                sp.PortName = portName;
                sp.BaudRate = 115200;
                sp.DtrEnable = true;
                sp.RtsEnable = true;

                sp.Open();

                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
            }


            catch (Exception)
            {
                //MessageBox.Show("Connection failed. Is the correct port selected?");
                Debug.Print("Port connection error");
            }
        }

        private void SendCalibrationData(string[] calibrationArray)
        {
            foreach (string line in calibrationArray)
            {
                Debug.Print("Sending line to uSMU: {0}", line);
                sp.WriteLine(line);
                Thread.Sleep(100);
            }

            Debug.Print("Finished writing. Resetting uSMU");
            sp.WriteLine("*RST");
            sp.Close();
            sp.Dispose();


        }
    }
}
