﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Media;
using System.Windows.Media;
using System.Threading.Tasks;
using YamlDotNet.Core;
using VvvfSimulator.Yaml.VvvfSound;
using VvvfSimulator.Generation.Pi3Generator;
using VvvfSimulator.GUI.Create.Waveform;
using VvvfSimulator.GUI.Util;
using VvvfSimulator.GUI.Mascon;
using VvvfSimulator.GUI.TrainAudio;
using VvvfSimulator.GUI.TaskViewer;
using VvvfSimulator.GUI.Simulator.RealTime;
using VvvfSimulator.GUI.Simulator.RealTime.Setting;
using VvvfSimulator.GUI.Simulator.RealTime.UniqueWindow;
using static VvvfSimulator.Generation.GenerateCommon;
using static VvvfSimulator.Yaml.VvvfSound.YamlVvvfSoundData;
using static VvvfSimulator.Yaml.MasconControl.YamlMasconAnalyze;
using static VvvfSimulator.Generation.Audio.GenerateRealTimeCommon;
using static VvvfSimulator.Yaml.TrainAudioSetting.YamlTrainSoundAnalyze;
using static VvvfSimulator.Generation.GenerateCommon.GenerationBasicParameter;

namespace VvvfSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewData BindingData = new();
        public class ViewData : ViewModelBase
        {
            private bool _Blocked = false;
            public bool Blocked
            {
                get
                {
                    return _Blocked;
                }
                set
                {
                    _Blocked = value;
                    RaisePropertyChanged(nameof(Blocked));
                }
            }
        };
        public class ViewModelBase : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void RaisePropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static MainWindow? Instance;
        public static MainWindow? GetInstance()
        {
            return Instance;
        }
        public static void Invoke(Action callBack)
        {
            Instance?.Dispatcher.Invoke(callBack);
        }

        public MainWindow()
        {
            Instance = this;
            DataContext = BindingData;
            InitializeComponent();
        }      

        private void SettingButtonClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            string name = button.Name;
            if (name.Equals("settings_level"))
                setting_window.Navigate(new Uri("GUI/Create/Settings/level_setting.xaml", UriKind.Relative));
            else if(name.Equals("settings_minimum"))
                setting_window.Navigate(new Uri("GUI/Create/Settings/minimum_freq_setting.xaml", UriKind.Relative));
            else if(name.Equals("settings_mascon"))
                setting_window.Navigate(new Uri("GUI/Create/Settings/jerk_setting.xaml", UriKind.Relative));
        }

        private void SettingEditClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            object? tag = btn.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;
            String[] command = tag_str.Split("_");

            var list_view = command[0].Equals("accelerate") ? accelerate_settings : brake_settings;
            var settings = command[0].Equals("accelerate") ? YamlVvvfManage.CurrentData.AcceleratePattern : YamlVvvfManage.CurrentData.BrakingPattern;

            if (command[1].Equals("remove"))
            {
                if(list_view.SelectedIndex >= 0)
                    settings.RemoveAt(list_view.SelectedIndex);
            }
                
            else if (command[1].Equals("add"))
                settings.Add(new YamlControlData());
            else if (command[1].Equals("reset"))
                settings.Clear();

            list_view.Items.Refresh();
        }
        private void SettingsLoad(object sender, RoutedEventArgs e)
        {
            ListView btn = (ListView)sender;
            object? tag = btn.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;

            if (tag.Equals("accelerate"))
            {
                UpdateControlList();
                AccelerateSelectedShow();
            }
            else
            {
                UpdateControlList();
                BrakeSelectedShow();
            }
        }
        private void SettingsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView btn = (ListView)sender;
            object? tag = btn.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;


            if(tag.Equals("accelerate"))
                AccelerateSelectedShow();
            else
                BrakeSelectedShow();
        }
       
        private void AccelerateSelectedShow()
        {
            int selected = accelerate_settings.SelectedIndex;
            if (selected < 0) return;

            YamlVvvfSoundData ysd = YamlVvvfManage.CurrentData;
            var selected_data = ysd.AcceleratePattern[selected];
            setting_window.Navigate(new Control_Setting_Page_Common(selected_data, ysd.Level));

        }
        private void BrakeSelectedShow()
        {
            int selected = brake_settings.SelectedIndex;
            if (selected < 0) return;

            YamlVvvfSoundData ysd = YamlVvvfManage.CurrentData;
            var selected_data = ysd.BrakingPattern[selected];
            setting_window.Navigate(new Control_Setting_Page_Common(selected_data, ysd.Level));
        }

        public void UpdateControlList()
        {
            accelerate_settings.ItemsSource = YamlVvvfManage.CurrentData.AcceleratePattern;
            brake_settings.ItemsSource = YamlVvvfManage.CurrentData.BrakingPattern;
            accelerate_settings.Items.Refresh();
            brake_settings.Items.Refresh();
        }
        public void UpdateContentSelected()
        {
            if (setting_tabs.SelectedIndex == 1)
            {
                AccelerateSelectedShow();
            }
            else if (setting_tabs.SelectedIndex == 2)
            {
                BrakeSelectedShow();
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            Object? tag = mi.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;
            String[] command = tag_str.Split(".");
            if (command[0].Equals("brake"))
            {
                if (command[1].Equals("sort"))
                {
                    YamlVvvfManage.CurrentData.BrakingPattern.Sort((a, b) => Math.Sign(b.ControlFrequencyFrom - a.ControlFrequencyFrom));
                    UpdateControlList();
                    BrakeSelectedShow();
                }
                else if (command[1].Equals("copy"))
                {
                    int selected = brake_settings.SelectedIndex;
                    if (selected < 0) return;

                    YamlVvvfSoundData ysd = YamlVvvfManage.CurrentData;
                    var selected_data = ysd.BrakingPattern[selected];
                    YamlVvvfManage.CurrentData.BrakingPattern.Add(selected_data.Clone());
                    UpdateControlList();
                    BrakeSelectedShow();
                }
            }
            else if (command[0].Equals("accelerate"))
            {
                if (command[1].Equals("sort"))
                {
                    YamlVvvfManage.CurrentData.AcceleratePattern.Sort((a, b) => Math.Sign(b.ControlFrequencyFrom - a.ControlFrequencyFrom));
                    UpdateControlList();
                    AccelerateSelectedShow();
                }
                else if (command[1].Equals("copy"))
                {
                    int selected = accelerate_settings.SelectedIndex;
                    if (selected < 0) return;

                    YamlVvvfSoundData ysd = YamlVvvfManage.CurrentData;
                    YamlControlData selected_data = ysd.AcceleratePattern[selected];
                    YamlVvvfManage.CurrentData.AcceleratePattern.Add(selected_data.Clone());
                    UpdateControlList();
                    BrakeSelectedShow();
                }
            }
        }



        private String load_path = "";
        public String GetLoadedYamlName()
        {
            return Path.GetFileNameWithoutExtension(load_path);
        }
        private void File_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            if (tag.Equals("Load"))
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Yaml (*.yaml)|*.yaml|All (*.*)|*.*"
                };
                if (dialog.ShowDialog() == false) return;

                try
                {
                    YamlVvvfManage.Load(dialog.FileName);
                    MessageBox.Show("Load OK.", "Great", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch(YamlException ex)
                {
                    String error_message = "";
                    error_message += "Invalid yaml\r\n";
                    error_message += "\r\n" + ex.End.ToString() + "\r\n";
                    MessageBox.Show(error_message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }


                load_path = dialog.FileName;
                UpdateControlList();
                //update_Control_Showing();
                setting_window.Navigate(null);

            }
            else if (tag.Equals("Save_As"))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Yaml (*.yaml)|*.yaml",
                    FileName = GetLoadedYamlName()
                };

                // ダイアログを表示する
                if (dialog.ShowDialog() == false) return;
                load_path = dialog.FileName;
                if (YamlVvvfManage.Save(dialog.FileName))
                    MessageBox.Show("Save OK.", "Great", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Error occurred on saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (tag.Equals("Save"))
            {
                String save_path = load_path;
                if(save_path.Length == 0)
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "Yaml (*.yaml)|*.yaml",
                        FileName = "VVVF"
                    };

                    // ダイアログを表示する
                    if (dialog.ShowDialog() == false) return;
                    load_path = dialog.FileName;
                    save_path = load_path;
                }
                if (YamlVvvfManage.Save(save_path))
                    MessageBox.Show("Save OK.", "Great", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Error occurred on saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (tag.Equals("Export_As_C"))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "C (*.c)|*.c",
                    FileName = GetLoadedYamlName()
                };

                // ダイアログを表示する
                if (dialog.ShowDialog() == false) return;

                try
                {
                    using (StreamWriter outputFile = new(dialog.FileName))
                    {
                        outputFile.Write(Pi3Generate.GenerateC(YamlVvvfManage.CurrentData, Path.GetFileNameWithoutExtension(dialog.FileName)));
                    }
                    MessageBox.Show("Export as C complete.", "Great", MessageBoxButton.OK, MessageBoxImage.Information);
                }catch
                {
                    MessageBox.Show("Error occurred.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
        }

        private void Generation_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;
            String[] command = tag_str.Split("_");


            BindingData.Blocked = true;

            bool unblock = SolveCommand(command);

            if (!unblock) return;
            BindingData.Blocked = false;
            SystemSounds.Beep.Play();

        }

        public class TaskProgressData
        {
            public ProgressData Data { get; set; }
            public Task Task { get; set; }
            public string Description { get; set; }
            public bool Cancelable
            {
                get
                {
                    return (!Data.Cancel && Data.RelativeProgress < 99.9);
                }
            }

            public String Status
            {
                get
                {
                    if (Data.Cancel) return "Canceled";
                    if (Data.RelativeProgress > 99.9) return "Complete";
                    return "Running";
                }
            }

            public SolidColorBrush StatusColor
            {
                get
                {
                    if (Data.Cancel) return new SolidColorBrush(Color.FromRgb(0xFF,0xCB,0x47));
                    if (Data.RelativeProgress > 99.9) return new SolidColorBrush(Color.FromRgb(0x95, 0xE0, 0x6C));
                    return new SolidColorBrush(Color.FromRgb(0x4F, 0x86, 0xC6));
                }
            }

            public TaskProgressData(Task Task, ProgressData progressData, string Description)
            {
                this.Task = Task;
                this.Data = progressData;
                this.Description = Description;
            }
        }

        public static List<TaskProgressData> taskProgresses = new();

        private static GenerationBasicParameter GetGenerationBasicParameter()
        {
            GenerationBasicParameter generationBasicParameter = new(
                Yaml_Mascon_Manage.CurrentData.GetCompiled(),
                YamlVvvfManage.DeepClone(YamlVvvfManage.CurrentData),
                new ProgressData()
            );

            return generationBasicParameter;
        }
        private Boolean SolveCommand(String[] command)
        {

            if (command[0].Equals("VVVF"))
            {
                if (command[1].Equals("WAV"))
                {
                    var dialog = new SaveFileDialog
                    {
                        Filter = "normal(192k)|*.wav|normal(5M)|*.wav|raw(192k)|*.wav|raw(5M)|*.wav",
                        FilterIndex = 1
                    };
                    if (dialog.ShowDialog() == false) return true;

                    int sample_freq = new int[] { 192000, 5000000, 192000, 5000000 }[dialog.FilterIndex - 1];
                    bool raw = new bool[] {false, false, true, true}[dialog.FilterIndex - 1];

                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();

                    Task task = Task.Run(() => {
                        try
                        {
                            Generation.Audio.VvvfSound.Audio.ExportWavFile(generationBasicParameter, sample_freq, raw, dialog.FileName);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "VVVF sound generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }
               
                else if(command[1].Equals("RealTime"))
                {
                    RealTimeParameter parameter = new()
                    {
                        quit = false
                    };

                    BindingData.Blocked = true;

                    System.IO.Ports.SerialPort? serial = null;
                    if (command.Length == 3)
                    {
                        try
                        {
                            ComPortSelector com = new();
                            com.ShowDialog();
                            serial = new()
                            {
                                PortName = com.GetComPortName(),
                            };
                        }
                        catch
                        {
                            MessageBox.Show("Invalid COM port", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return true;
                        }
                    }

                    MasconWindow mascon = new(parameter);
                    mascon.Show();
                    mascon.Start_Task();                  

                    if (Properties.Settings.Default.RealTime_VVVF_WaveForm_Show)
                    {
                        RealtimeWindows.WaveForm window = new(parameter);
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_VVVF_Control_Show)
                    {
                        RealtimeWindows.ControlStatus window = new(
                            parameter,
                            (RealtimeWindows.ControlStatus.RealTimeControlStatStyle)Properties.Settings.Default.RealTime_VVVF_Control_Style,
                            Properties.Settings.Default.RealTime_VVVF_Control_Precise
                        );
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_VVVF_Hexagon_Show)
                    {
                        RealtimeWindows.Hexagon window = new(
                            parameter,
                            (RealtimeWindows.Hexagon.RealTimeHexagonStyle)Properties.Settings.Default.RealTime_VVVF_Hexagon_Style,
                            Properties.Settings.Default.RealTime_VVVF_Hexagon_ZeroVector
                        );
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_VVVF_FFT_Show)
                    {
                        RealtimeWindows.Fft window = new(parameter);
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_VVVF_FS_Show)
                    {
                        Fs window = new(parameter);
                        window.Show();
                        window.RunTask();
                    }

                    bool do_clone = !Properties.Settings.Default.RealTime_VVVF_EditAllow;
                    YamlVvvfSoundData data = do_clone ? YamlVvvfManage.DeepClone(YamlVvvfManage.CurrentData) : YamlVvvfManage.CurrentData;

                    Task task = Task.Run(() => {
                        try
                        {
                            Generation.Audio.VvvfSound.RealTime.Calculate(data, parameter, serial);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        BindingData.Blocked = false;
                        SystemSounds.Beep.Play();
                    });

                    return Properties.Settings.Default.RealTime_VVVF_EditAllow;
                }
                else if (command[1].Equals("Setting"))
                {
                    Basic setting = new( Basic.RealTime_Basic_Setting_Type.VVVF );
                    setting.ShowDialog();
                }
            }
            else if (command[0].Equals("Train"))
            {
                if (command[1].Equals("WAV"))
                {

                    var dialog = new SaveFileDialog { Filter = "wav|*.wav|raw|*.wav" };
                    if (dialog.ShowDialog() == false) return true;

                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();

                    Task task = Task.Run(() => {
                        try
                        {
                            bool raw = dialog.FilterIndex == 2;
                            YamlTrainSoundData trainSound_Data_clone = YamlTrainSoundDataManage.CurrentData.Clone();
                            Generation.Audio.TrainSound.Audio.ExportWavFile(generationBasicParameter, trainSound_Data_clone, 192000, raw, dialog.FileName);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "Train sound generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }
                else if (command[1].Equals("RealTime"))
                {
                    RealTimeParameter parameter = new()
                    {
                        quit = false
                    };

                    MasconWindow mascon = new(parameter);
                    mascon.Show();
                    mascon.Start_Task();

                    if (Properties.Settings.Default.RealTime_Train_WaveForm_Show)
                    {
                        RealtimeWindows.WaveForm window = new(parameter);
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_Train_Control_Show)
                    {
                        RealtimeWindows.ControlStatus window = new(
                            parameter,
                            (RealtimeWindows.ControlStatus.RealTimeControlStatStyle)Properties.Settings.Default.RealTime_Train_Control_Style,
                            Properties.Settings.Default.RealTime_Train_Control_Precise
                        );
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_Train_Hexagon_Show)
                    {
                        RealtimeWindows.Hexagon window = new(
                            parameter,
                            (RealtimeWindows.Hexagon.RealTimeHexagonStyle)Properties.Settings.Default.RealTime_Train_Hexagon_Style,
                            Properties.Settings.Default.RealTime_Train_Hexagon_ZeroVector
                        );
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_Train_FFT_Show)
                    {
                        RealtimeWindows.Fft window = new(parameter);
                        window.Show();
                        window.RunTask();
                    }

                    if (Properties.Settings.Default.RealTime_Train_FS_Show)
                    {
                        Fs window = new(parameter);
                        window.Show();
                        window.RunTask();
                    }


                    BindingData.Blocked = true;
                    Task task = Task.Run(() => {
                        try
                        {
                            bool do_clone = !Properties.Settings.Default.RealTime_Train_EditAllow;
                            YamlVvvfSoundData data;
                            if (do_clone)
                                data = YamlVvvfManage.DeepClone(YamlVvvfManage.CurrentData);
                            else
                                data = YamlVvvfManage.CurrentData;
                            Generation.Audio.TrainSound.RealTime.Generate(data , parameter);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        BindingData.Blocked = false;
                        SystemSounds.Beep.Play();
                    });
                    return Properties.Settings.Default.RealTime_Train_EditAllow;
                }
                else if (command[1].Equals("Setting"))
                {
                    Basic setting = new( Basic.RealTime_Basic_Setting_Type.Train );
                    setting.ShowDialog();
                }
            }
            else if (command[0].Equals("Control"))
            {
                int[] valid_fps = [120, 60, 30, 10, 5];
                string filter = "";
                for(int i = 0; i < valid_fps.Length; i++)
                {
                    filter += valid_fps[i] + "fps|*.mp4" + (i + 1 == valid_fps.Length ? "" : "|");
                }
                var dialog = new SaveFileDialog { Filter = filter, FilterIndex = 2 };
                if (dialog.ShowDialog() == false) return true;
                int fps = valid_fps[dialog.FilterIndex - 1];

                if (command[1].Equals("Original"))
                {
                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();
                    Task task = Task.Run(() => {
                        try
                        {
                            Generation.Video.ControlInfo.GenerateControlOriginal generate = new();
                            generate.ExportVideo(generationBasicParameter, dialog.FileName, fps);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "Control video(O1) generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }

                else if (command[1].Equals("Original2"))
                {
                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();

                    Task task = Task.Run(() => {
                        try
                        {
                            Generation.Video.ControlInfo.GenerateControlOriginal2 generation = new();
                            generation.ExportVideo(generationBasicParameter, dialog.FileName, fps);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "Control video(O2) generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }

                    
            }
            else if (command[0].Equals("WaveForm"))
            {
                var dialog = new SaveFileDialog { Filter = "mp4 (*.mp4)|*.mp4" };
                if (dialog.ShowDialog() == false) return true;

                GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();
                Task task = Task.Run(() => {
                    try
                    {
                        if (command[1].Equals("Original"))
                            new Generation.Video.WaveForm.GenerateWaveFormUV().ExportVideo2(generationBasicParameter, dialog.FileName);
                        else if (command[1].Equals("Spaced"))
                            new Generation.Video.WaveForm.GenerateWaveFormUV().ExportVideo1(generationBasicParameter, dialog.FileName);
                        else if (command[1].Equals("UVW"))
                            new Generation.Video.WaveForm.GenerateWaveFormUVW().ExportVideo(generationBasicParameter, dialog.FileName);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    SystemSounds.Beep.Play();
                });

                TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "Waveform video(" + command[1] + ") generation of " + GetLoadedYamlName());
                taskProgresses.Add(taskProgressData);
            }
            else if (command[0].Equals("Hexagon"))
            {
                MessageBoxResult result = MessageBox.Show("Enable zero vector circle?", "Info", MessageBoxButton.YesNo, MessageBoxImage.Question);
                bool circle = result == MessageBoxResult.Yes;

                if (command[1].Equals("Original"))
                {
                    var dialog = new SaveFileDialog { Filter = "mp4 (*.mp4)|*.mp4" };
                    if (dialog.ShowDialog() == false) return true;

                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();
                    Task task = Task.Run(() => {
                        try
                        {
                            new Generation.Video.Hexagon.GenerateHexagonOriginal().ExportVideo(generationBasicParameter, dialog.FileName, circle);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "Hexagon video(Original) generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }
                else if (command[1].Equals("Explain"))
                {
                    var dialog = new SaveFileDialog { Filter = "mp4 (*.mp4)|*.mp4" };
                    if (dialog.ShowDialog() == false) return true;

                    DoubleNumberInput double_Ask_Dialog = new("Enter the frequency.");
                    double_Ask_Dialog.ShowDialog();

                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();
                    Task task = Task.Run(() => {
                        try
                        {
                            new Generation.Video.Hexagon.GenerateHexagonExplain().generate_wave_hexagon_explain(generationBasicParameter, dialog.FileName, circle, double_Ask_Dialog.EnteredValue);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "Hexagon video(Explain) generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }
                else if (command[1].Equals("OriginalImage"))
                {
                    var dialog = new SaveFileDialog { Filter = "png (*.png)|*.png" };
                    if (dialog.ShowDialog() == false) return true;

                    DoubleNumberInput double_Ask_Dialog = new ("Enter the frequency.");
                    double_Ask_Dialog.ShowDialog();

                    Task task = Task.Run(() => {
                        try
                        {
                            YamlVvvfSoundData clone = YamlVvvfManage.DeepClone(YamlVvvfManage.CurrentData);
                            new Generation.Video.Hexagon.GenerateHexagonOriginal().ExportImage(dialog.FileName, clone, circle, double_Ask_Dialog.EnteredValue);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });
                }
                
            }
            else if (command[0].Equals("FFT"))
            {
                if (command[1].Equals("Video"))
                {
                    var dialog = new SaveFileDialog { Filter = "mp4 (*.mp4)|*.mp4" };
                    if (dialog.ShowDialog() == false) return true;

                    GenerationBasicParameter generationBasicParameter = GetGenerationBasicParameter();
                    Task task = Task.Run(() => {
                        try
                        {
                            new Generation.Video.FFT.GenerateFFT().ExportVideo(generationBasicParameter, dialog.FileName);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });

                    TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, "FFT video generation of " + GetLoadedYamlName());
                    taskProgresses.Add(taskProgressData);
                }
                else if (command[1].Equals("Image"))
                {
                    var dialog = new SaveFileDialog { Filter = "png (*.png)|*.png" };
                    if (dialog.ShowDialog() == false) return true;

                    DoubleNumberInput double_Ask_Dialog = new("Enter the frequency.");
                    double_Ask_Dialog.ShowDialog();

                    Task task = Task.Run(() => {
                        try
                        {
                            YamlVvvfSoundData clone = YamlVvvfManage.DeepClone(YamlVvvfManage.CurrentData);
                            new Generation.Video.FFT.GenerateFFT().ExportImage(dialog.FileName, clone, double_Ask_Dialog.EnteredValue);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        SystemSounds.Beep.Play();
                    });
                }

            }
            return true;
        }

        private void Window_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;

            if (tag_str.Equals("LCalc"))
            {
                Linear_Calculator lc = new();
                lc.Show();
            }
            else if (tag_str.Equals("TaskProgressView"))
            {
                TaskViewer_Main tvm = new();
                tvm.Show();
            }
        }

        private void Setting_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;

            if (tag_str.Equals("AccelPattern"))
            {
                BindingData.Blocked = true;
                Mascon_Control_Main gmcw = new();
                gmcw.ShowDialog();
                BindingData.Blocked = false;
            }
            else if (tag_str.Equals("TrainSoundSetting"))
            {
                BindingData.Blocked = true;
                YamlTrainSoundData _TrainSound_Data = YamlTrainSoundDataManage.CurrentData;
                SettingsWindow tahw = new(_TrainSound_Data);
                tahw.ShowDialog();
                BindingData.Blocked = false;
            }
            
        }

        private void Process_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;

            if (tag_str.Equals("AutoVoltage"))
            {
                BindingData.Blocked = true;
                Task.Run(() =>
                {
                    MessageBox.Show("The settings which is not using `Linear` will be skipped.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    bool result = YamlVvvfUtil.Auto_Voltage(YamlVvvfManage.CurrentData);
                    if(!result)
                        MessageBox.Show("Please check next things.\r\nAll of the amplitude mode are linear.\r\nAccel and Braking has more than 2 settings.\r\nFrom is grater or equal to 0", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    BindingData.Blocked = false;
                    SystemSounds.Beep.Play();
                });
                
            }else if (tag_str.Equals("FreeRunAmpZero"))
            {
                BindingData.Blocked = true;
                Task.Run(() =>
                {
                    bool result = YamlVvvfUtil.Set_All_FreeRunAmp_Zero(YamlVvvfManage.CurrentData);
                    if (!result)
                        MessageBox.Show("Something went wrong.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                    BindingData.Blocked = false;
                    SystemSounds.Beep.Play();
                });
            }

            
        }

        private void Util_Menu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            String? tag_str = tag.ToString();
            if (tag_str == null) return;

            if(tag_str.Equals("MIDI"))
            {
                GUI.MIDIConvert.MIDIConvert_Main mIDIConvert_Main = new();
                mIDIConvert_Main.Show();
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
