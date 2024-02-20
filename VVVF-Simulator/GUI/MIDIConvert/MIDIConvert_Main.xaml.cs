﻿using Microsoft.Win32;
using NextMidi.Data;
using NextMidi.Data.Track;
using NextMidi.Filing.Midi;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using VvvfSimulator.Yaml.VVVFSound;
using static VvvfSimulator.Generation.GenerateCommon;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.Forms.MessageBox;

namespace VvvfSimulator.GUI.MIDIConvert
{
    /// <summary>
    /// MIDIConvert_Main.xaml の相互作用ロジック
    /// </summary>
    public partial class MIDIConvert_Main : Window
    {
        public MIDIConvert_Main()
        {
            InitializeComponent();
        }

        public static bool Conversion(String midi_path, String output_path, Midi_Convert_Config midi_Convert_Config)
        {
            //MIDIDataを変換
            MidiData midiData;
            try
            {
                midiData = MidiReader.ReadFrom(midi_path);
            }
            catch
            {
                MessageBox.Show("This MIDI cannot be converted.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            String file_path = output_path.Replace(Path.GetExtension(output_path) , "");

            TrackCollection tracks = midiData.Tracks;

            for(int i = 0; i < tracks.Count; i++)
            {
                int priority = 1;
                while (true)
                {
                    Mascon.MasconControlMidi.LoadData loadData = new()
                    {
                        track = i,
                        division = 1,
                        path = midi_path,
                        priority = priority,
                    };
                    Yaml.MasconControl.YamlMasconAnalyze.YamlMasconData? ymd = Yaml.MasconControl.YamlMasconMidi.Convert(loadData);
                    if (ymd == null) return false;
                    if (ymd.points.Count == 0) break;

                    GenerationBasicParameter generationBasicParameter = new(
                        ymd.GetCompiled(),
                        YamlVvvfManage.DeepClone(YamlVvvfManage.CurrentData),
                        new GenerationBasicParameter.ProgressData()
                    );

                    String task_description = "Generation of MIDI(" + Path.GetFileNameWithoutExtension(midi_path) + ") Sound part " + i + " of " + priority;
                    String export_path = Path.GetFullPath(file_path + "_" + i.ToString() + "_" + priority.ToString() + Path.GetExtension(output_path));

                    Task task = Task.Run(() =>
                    {
                        try
                        {
                            Generation.Audio.VvvfSound.GenerateVVVFAudio.Export_VVVF_Sound(generationBasicParameter, export_path, false, midi_Convert_Config.SampleFrequency);
                            System.Media.SystemSounds.Beep.Play();
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show("Error on " + task_description + "\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });

                    MainWindow.TaskProgressData taskProgressData = new(task, generationBasicParameter.progressData, task_description);
                    MainWindow.taskProgresses.Add(taskProgressData);

                    if(!midi_Convert_Config.MultiThread) task.Wait();

                    priority++;
                }
                
            }

            return true;
        }

        public Midi_Convert_Config config = new();
        public class Midi_Convert_Config
        {
            public bool MultiThread { get; set; } = true;
            public int SampleFrequency { get; set; } = 192000;
        }

        private string? midi_path;
        private bool midi_selected = false;
        private string? export_path;
        private bool export_selected = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button? btn = (Button)sender;
            if (btn == null) return;
            String? tag = btn.Tag.ToString();
            if(tag == null) return;

            if (tag.Equals("Browse"))
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Midi (*.mid)|*.mid|All (*.*)|*.*",
                };
                if(dialog.ShowDialog() == false) return;
                String path = dialog.FileName;
                if (path.Length == 0) return;

                midi_path = path;
                midi_selected = true;
            }
            else if (tag.Equals("Select"))
            {
                var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "wav (*.wav)|*.wav" };
                if (dialog.ShowDialog() == false) return;

                String path = dialog.FileName;
                if(path.Length == 0) return;

                export_path = path;
                export_selected = true;
                
            }
            else if (tag.Equals("Config"))
            {
                MIDIConvert_Config mIDIConvert_Config = new(config);
                mIDIConvert_Config.ShowDialog();
            }
            else if (tag.Equals("Convert"))
            {
                if(midi_path == null || export_path == null) return;

                Action action = new(() => Conversion((string)midi_path.Clone(), (string)export_path.Clone(), config));

                if (config.MultiThread) action();
                else Task.Run(action);

                System.Media.SystemSounds.Beep.Play();
                Close();
            }

            BtnConvert.IsEnabled = export_selected && midi_selected;
        }
    }
}
