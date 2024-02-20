using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using LibreHardwareMonitor.Hardware;
using System.IO;

namespace ArduinoStatSuite
{
    public interface IFunctions
    {
        string[] GetAvailableComPorts();
    }
    class Functions
    {
        private SerialPort port;
        readonly Computer _thisPCTelemetry;
        readonly BackgroundWorker bgWorker;
        readonly Queue<string> DataQ;
        readonly System.Timers.Timer dataTimer;
        private bool canEnqueueData = true;

        public Functions()
        {
            _thisPCTelemetry = new Computer();
            bgWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            DataQ = new Queue<string>();
            dataTimer = new System.Timers.Timer(500);
            dataTimer.Elapsed += DataTimer_Elapsed;
        }

        private void DataTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                SendDatatoArduino(false, string.Empty);
            }
            catch (Exception)
            {

                throw;
            };
        }

        private void SendDatatoArduino(bool isPriority, string data)
        {
            try
            {
                if (isPriority)
                {
                    canEnqueueData = false;
                    DataQ.Clear();
                    port.Write(data);
                    Thread.Sleep(2000);
                    canEnqueueData = true;
                    //DataQ.Enqueue(data);
                }
                if (DataQ.Count > 0)
                {
                    if (port.IsOpen)
                    {
                        port.Write(DataQ.Dequeue());
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        internal string[] GetAvailableComPorts()
        {
            string[] ports;
            try
            {
                ports = SerialPort.GetPortNames();
            }
            catch (Exception)
            {

                throw;
            }
            return ports;
        }

        internal bool ConnectToArduino(ComboBox cmb)
        {
            bool isConnected = false;
            try
            {
                string selectedPort = cmb.Text;
                port = new SerialPort(selectedPort, 9600, Parity.None, 8, StopBits.One);
                if (!port.IsOpen)
                {
                    port.Open();
                    isConnected = true;
                    dataTimer.Start();
                }

            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(cmb.Text + " is not available. Please try again.");
            }
            return isConnected;
        }


        internal bool DisconnectFromArduino()
        {
            bool isDisconnected = false;
            try
            {
                if (port.IsOpen)
                {
                    port.Close();
                    isDisconnected = true;
                    dataTimer.Stop();
                }
            }
            catch (Exception)
            {

                throw;
            }

            return isDisconnected;
        }

        private void RunBGWorker()
        {
            try
            {
                bgWorker.DoWork += BgWorker_DoWork;
                bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;
                if (!bgWorker.IsBusy)
                {
                    bgWorker.RunWorkerAsync(port);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SerialPort port = e.Argument as SerialPort;
            if (port != null)
            {
                while (!bgWorker.CancellationPending)
                {
                    Thread.Sleep(1000);
                    DataCheck(port);
                }
            }
        }
        private void DataCheck(SerialPort port)
        {
            string cpuTemp = "";
            string gpuTemp = "";
            string gpuLoad = "";
            string cpuLoad = "";
            string ramUsed = "";
            string CPUName = string.Empty;
            string GPUName = string.Empty;


            // enumerating all the hardware
            if (port.IsOpen)
            {
                foreach (IHardware hw in _thisPCTelemetry.Hardware)
                {
                    try
                    {
                        //hw.Update();
                    }
                    catch { 
                        continue;
                    }
                    if (hw.HardwareType == HardwareType.Cpu)
                    {
                        CPUName = hw.Name.Substring(4);
                    }
                    if (hw.HardwareType == HardwareType.GpuAmd)
                    {
                        GPUName = hw.Name;
                    }
                    foreach (ISensor s in hw.Sensors)
                    {
                        s.Hardware.Update();
                        if (s.SensorType == SensorType.Temperature)
                        {
                            if (s.Value != null)
                            {
                                int curTemp = (int)s.Value;
                                switch (s.Name)
                                {
                                    case "Core (Tctl/Tdie)":
                                        cpuTemp = curTemp.ToString();
                                        break;
                                    case "GPU Core":
                                        gpuTemp = curTemp.ToString();
                                        break;

                                }
                            }
                        }
                        if (s.SensorType == SensorType.Load)
                        {
                            if (s.Value != null)
                            {
                                int curLoad = (int)s.Value;
                                switch (s.Name)
                                {
                                    case "CPU Total":
                                        cpuLoad = curLoad.ToString();
                                        break;
                                    case "GPU Core":
                                        gpuLoad = curLoad.ToString();
                                        break;
                                }
                            }
                        }
                        if (s.SensorType == SensorType.Data)
                        {
                            if (s.Value != null)
                            {
                                switch (s.Name)
                                {
                                    case "Used Memory":
                                        decimal decimalRam = Math.Round((decimal)s.Value, 1);
                                        ramUsed = decimalRam.ToString();
                                        break;
                                }
                            }
                        }
                    }
                }

                string arduinoData = "CPU:" + CPUName + "GPU:" + GPUName + "CT:" + cpuTemp + "CL:" + cpuLoad + "GT:" + gpuTemp + "GL:" + gpuLoad + "RAM:" + ramUsed + "|";
                //SendDatatoArduino(false, arduinoData, true);
                if (canEnqueueData)
                {
                    DataQ.Enqueue(arduinoData);
                }
            }

        }
        private void StopBGWorker()
        {
            try
            {                 
                if (!bgWorker.CancellationPending)
                {
                    bgWorker.CancelAsync();
                }
                WriteData("#DSC|");
            }
            catch (Exception)
            {

                throw;
            }
        }

        internal void EnableDisableTelemetry(bool isTelemetryEnabled)
        {
            try
            {
                _thisPCTelemetry.IsCpuEnabled = isTelemetryEnabled;
                _thisPCTelemetry.IsGpuEnabled = isTelemetryEnabled;
                _thisPCTelemetry.IsStorageEnabled = isTelemetryEnabled;
                _thisPCTelemetry.IsMotherboardEnabled = isTelemetryEnabled;
                _thisPCTelemetry.IsMemoryEnabled = isTelemetryEnabled;
                if (isTelemetryEnabled)
                {
                    try
                    {
                        _thisPCTelemetry.Open();
                    }
                    catch
                    {

                    }
                    finally
                    {
                        _thisPCTelemetry.Accept(new UpdateVisitor());
                        RunBGWorker();
                    }
                }
                else
                {
                    StopBGWorker();
                    _thisPCTelemetry.Close();
                }
            }
            catch (Exception)
            {
                StopBGWorker();
                _thisPCTelemetry.Close();
                throw;
            }
        }

        internal void WriteData(string data)
        {
            try
            {
                dataTimer.Stop();
                //bgWorker.CancelAsync();
                //Thread.Sleep(10);
                SendDatatoArduino(true, data);
                //bgWorker.RunWorkerAsync(port);
                dataTimer.Start();
            }
            catch (Exception)
            {
                EnableDisableTelemetry(false);
                throw;
            }
        }

    }
}
