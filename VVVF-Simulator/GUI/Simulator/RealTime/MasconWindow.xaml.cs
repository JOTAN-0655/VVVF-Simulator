﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VvvfSimulator.GUI.Simulator.RealTime.Setting;
using VvvfSimulator.Yaml.VvvfSound;
using static VvvfSimulator.Generation.Audio.GenerateRealTimeCommon;
using static VvvfSimulator.VvvfCalculate;
using static VvvfSimulator.VvvfStructs;
using static VvvfSimulator.VvvfStructs.PulseMode;

namespace VvvfSimulator.GUI.Simulator.RealTime
{
    /// <summary>
    /// Mascon.xaml の相互作用ロジック
    /// </summary>
    public partial class MasconWindow : Window
    {
        RealTimeParameter realTime_Parameter;
        public MasconWindow(RealTimeParameter parameter)
        {
            realTime_Parameter = parameter;

            InitializeComponent();
            SetState(0);
            DataContext = view_model;
        }

        public void Start_Task()
        {

            Task.Run(() => {
                while (!realTime_Parameter.quit)
                {
                    System.Threading.Thread.Sleep(20);
                    view_model.sine_freq = realTime_Parameter.Control.GetVideoSineFrequency();
                    view_model.pulse_state = GetPulseName();
                }
            });
            Task.Run(() => {
                while (!realTime_Parameter.quit)
                {
                    VvvfValues solve_control = realTime_Parameter.Control.Clone();
                    solve_control.SetRandomFrequencyMoveAllowed(false);
                    double voltage = Generation.Video.ControlInfo.GenerateControlCommon.GetVoltageRate(solve_control, realTime_Parameter.VvvfSoundData, false) * 100;
                    view_model.voltage = voltage;
                }
            });
        }

        private readonly ViewModel view_model = new ();
        public class ViewModel : ViewModelBase
        {

            private Brush _B4 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B4 { get { return _B4; } set { _B4 = value; RaisePropertyChanged(nameof(B4)); } }


            private Brush _B3 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B3 { get { return _B3; } set { _B3 = value; RaisePropertyChanged(nameof(B3)); } }


            private Brush _B2 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B2 { get { return _B2; } set { _B2 = value; RaisePropertyChanged(nameof(B2)); } }


            private Brush _B1 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B1 { get { return _B1; } set { _B1 = value; RaisePropertyChanged(nameof(B1)); } }

            private Brush _B0 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush B0 { get { return _B0; } set { _B0 = value; RaisePropertyChanged(nameof(B0)); } }


            private Brush _N = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush N { get { return _N; } set { _N = value; RaisePropertyChanged(nameof(N)); } }

            private Brush _P0 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P0 { get { return _P0; } set { _P0 = value; RaisePropertyChanged(nameof(P0)); } }

            private Brush _P1 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P1 { get { return _P1; } set { _P1 = value; RaisePropertyChanged(nameof(P1)); } }

            private Brush _P2 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P2 { get { return _P2; } set { _P2 = value; RaisePropertyChanged(nameof(P2)); } }

            private Brush _P3 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P3 { get { return _P3; } set { _P3 = value; RaisePropertyChanged(nameof(P3)); } }


            private Brush _P4 = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));
            public Brush P4 { get { return _P4; } set { _P4 = value; RaisePropertyChanged(nameof(P4)); } }



            private double _sine_freq = 0;
            public double sine_freq { get { return _sine_freq; } set { _sine_freq = value; RaisePropertyChanged(nameof(sine_freq)); } }

            private double _voltage = 0;
            public double voltage { get { return _voltage; } set { _voltage = value; RaisePropertyChanged(nameof(voltage)); } }

            private String _pulse_state = PulseModeNames.Async.ToString();
            public String pulse_state { get { return _pulse_state; } set { _pulse_state = value; RaisePropertyChanged(nameof(pulse_state)); } }
        };
        public class ViewModelBase : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void RaisePropertyChanged(string propertyName)
            {
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private String GetPulseName()
        {
            // Recalculate
            VvvfValues solve_control = realTime_Parameter.Control.Clone();
            Task re_calculate = Task.Run(() =>
            {
                solve_control.SetRandomFrequencyMoveAllowed(false);
                ControlStatus cv = new ControlStatus
                {
                    brake = solve_control.IsBraking(),
                    mascon_on = !solve_control.IsMasconOff(),
                    free_run = solve_control.IsFreeRun(),
                    wave_stat = solve_control.GetControlFrequency()
                };
                PwmCalculateValues calculated_Values = Yaml.VvvfSound.YamlVvvfWave.CalculateYaml(solve_control, cv, realTime_Parameter.VvvfSoundData);
                CalculatePhases(solve_control, calculated_Values, 0);
            });
            re_calculate.Wait();

            PulseMode mode_p = solve_control.GetVideoPulseMode();
            PulseModeNames mode = mode_p.PulseName;
            //Not in sync
            if (mode == PulseModeNames.Async)
            {
                CarrierFreq carrier_freq_data = solve_control.GetVideoCarrierFrequency();
                String default_s = String.Format(carrier_freq_data.base_freq.ToString("F2"));
                return default_s;
            }

            //Abs
            if (mode == PulseModeNames.P_Wide_3)
                return "W 3";

            if (mode.ToString().StartsWith("CHM"))
            {
                String mode_name = mode.ToString();
                bool contain_wide = mode_name.Contains("Wide");
                mode_name = mode_name.Replace("_Wide", "");

                String[] mode_name_type = mode_name.Split("_");

                String final_mode_name = ((contain_wide) ? "W " : "") + mode_name_type[1];

                return "CHM " + final_mode_name;
            }
            else if (mode.ToString().StartsWith("SHE"))
            {
                String mode_name = mode.ToString();
                bool contain_wide = mode_name.Contains("Wide");
                mode_name = mode_name.Replace("_Wide", "");

                String[] mode_name_type = mode_name.Split("_");

                String final_mode_name = (contain_wide) ? "W " : "" + mode_name_type[1];

                return "SHE " + final_mode_name;
            }
            else if (mode.ToString().StartsWith("HOP"))
            {
                String mode_name = mode.ToString();
                bool contain_wide = mode_name.Contains("Wide");
                mode_name = mode_name.Replace("_Wide", "");

                String[] mode_name_type = mode_name.Split("_");

                String final_mode_name = (contain_wide) ? "W " : "" + mode_name_type[1];

                return "HOP " + final_mode_name;
            }
            else
            {
                String[] mode_name_type = mode.ToString().Split("_");
                return mode_name_type[1];
            }
        }

        private void set_Color(int c,Color col)
        {
            SolidColorBrush brush = new SolidColorBrush(col);
            if (c == 0) view_model.N = brush;

            if (c == -1) view_model.B0 = brush;
            if (c == -2) view_model.B1 = brush;
            if (c == -3) view_model.B2 = brush;
            if (c == -4) view_model.B3 = brush;
            if (c == -5) view_model.B4 = brush;

            if (c == 1) view_model.P0 = brush;
            if (c == 2) view_model.P1 = brush;
            if (c == 3) view_model.P2 = brush;
            if (c == 4) view_model.P3 = brush;
            if (c == 5) view_model.P4 = brush;
        }

        private void SetState(int at)
        {
            
            int at_abs = (at < 0) ? -at : at;
            bool nega = at < 0;

            realTime_Parameter.change_amount = (nega ? -1 : 1) * ((at_abs - 1 < 0) ? 0 : at_abs - 1) * 3 * Math.PI;

            bool pre_braking = realTime_Parameter.braking;
            realTime_Parameter.free_run = at == 0;
            realTime_Parameter.braking = (at == 0) ? pre_braking : at < 0;

            Color gray = Color.FromRgb(0xA0, 0xA0, 0xA0);

            if (at == 0)
            {
                Color neutral = Color.FromRgb(0x4c, 0xeb, 0x34);
                for (int i = -5; i < 6; i++)
                {
                    set_Color(i, i == 0 ? neutral : gray);
                }
                return;
            } else
                set_Color(0, gray);

            for(int i = 1; i <= 5; i++)
            {
                Color target;

                if (nega)
                {
                    // 255,193,0
                    // 255,77,0

                    double r = (255 - 255) / 4.0 * (i - 1) + 255;
                    double g = (77 - 193) / 4.0 * (i - 1) + 193;
                    double b = (0 - 0) / 4.0 * (i - 1) + 0;
                    target = Color.FromRgb((byte)r, (byte)g, (byte)b);
                }
                else
                {
                    // 120,193,255
                    // 40,113,204
                    double r = (40 - 120) / 4.0 * (i - 1) + 120;
                    double g = (113 - 193) / 4.0 * (i - 1) + 193;
                    double b = (204 - 255) / 4.0 * (i - 1) + 255;
                    target = Color.FromRgb((byte)r, (byte)g, (byte)b);
                }
                
                if(at_abs >= i)
                    set_Color(nega ? -i : i, target);
                else
                    set_Color(nega ? -i : i, gray);

                set_Color(nega ? i : -i, gray);
            }
            

            
        }

        public int current_stat = 0;
        public DeviceMode current_mode = DeviceMode.KeyBoard;

        // Serial Port
        public string current_port = "COM3";
        public SerialPort serialPort = new();
        public void SetConfig()
        {
            if(current_mode == DeviceMode.PicoMascon)
            {
                if (serialPort.IsOpen) serialPort.Close();
                try
                {
                    serialPort = new SerialPort(current_port);

                    serialPort.BaudRate = 9600;
                    serialPort.Parity = Parity.None;
                    serialPort.StopBits = StopBits.One;
                    serialPort.DataBits = 8;
                    serialPort.Handshake = Handshake.None;
                    serialPort.DtrEnable = true;

                    serialPort.DataReceived += new SerialDataReceivedEventHandler(OnReceived);

                    serialPort.Open();



                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error" , MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if(current_mode == DeviceMode.KeyBoard)
            {
                if (serialPort.IsOpen) serialPort.Close();
            }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (current_mode != DeviceMode.KeyBoard) return;
            Key key = e.Key;

            if (key.Equals(Key.S))
                current_stat++;
            else if (key.Equals(Key.W))
                current_stat--;

            if (current_stat > 5) current_stat = 5;
            if (current_stat < -5) current_stat = -5;

            SetState(current_stat);

        }
        
        private void OnReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!serialPort.IsOpen) return;
            String read = serialPort.ReadExisting();
            
            if(current_mode == DeviceMode.PicoMascon)
            {
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        int current = Int32.Parse(read);
                        current_stat = current - 5;
                        SetState(current_stat);
                    });
                }
                catch (Exception)
                {

                }
            }

        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            Object tag = menuItem.Tag;

            if (tag.Equals("DeviceSetting"))
            {
                Device rtds = new(this);
                rtds.ShowDialog();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (serialPort.IsOpen) serialPort.Close();
            realTime_Parameter.quit = true;
        }

        private void OnWindowControlButtonClick(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;
            string? tag = btn.Tag.ToString();
            if (tag == null) return;

            if (tag.Equals("Close"))
                Close();
            else if (tag.Equals("Maximize"))
            {
                if (WindowState.Equals(WindowState.Maximized))
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
            }
            else if (tag.Equals("Minimize"))
                WindowState = WindowState.Minimized;
        }

    }
}
