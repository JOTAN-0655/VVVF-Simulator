﻿using System;
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
using static VvvfSimulator.Yaml.TrainAudioSetting.YamlTrainSoundAnalyze;

namespace VvvfSimulator.GUI.TrainAudio.Pages.Motor
{
    /// <summary>
    /// TrainAudio_MotorSound_Setting_Page.xaml の相互作用ロジック
    /// </summary>
    public partial class MotorSetting : Page
    {
        readonly YamlTrainSoundData TrainSoundData;
        readonly bool IgnoreUpdate = true;
        public MotorSetting(YamlTrainSoundData train_Harmonic_Data)
        {
            InitializeComponent();
            this.TrainSoundData = train_Harmonic_Data;

            var motor = train_Harmonic_Data.MotorSpec;
            SR.Text = motor.R_s.ToString();
            RR.Text = motor.R_r.ToString();
            SI.Text = motor.L_s.ToString();
            RI.Text = motor.L_r.ToString();
            MI.Text = motor.L_m.ToString();
            PL.Text = motor.NP.ToString();
            D.Text = motor.DAMPING.ToString();
            RIM.Text = motor.INERTIA.ToString();
            SF.Text = motor.STATICF.ToString();

            Motor_Harmonics_List.ItemsSource = train_Harmonic_Data.HarmonicSound;
            if (train_Harmonic_Data.HarmonicSound.Count > 0)
            {
                Motor_Harmonics_List.SelectedIndex = 0;
                Motor_Harmonic_Edit_Frame.Navigate(new HarmonicSetting((YamlTrainSoundData.HarmonicData)Motor_Harmonics_List.SelectedItem,Motor_Harmonics_List));
            }
                

            IgnoreUpdate = false;
        }
        private static double ParseDouble(TextBox tb)
        {
            try
            {
                VisualStateManager.GoToState(tb, "Success", false);
                return double.Parse(tb.Text);
            }
            catch
            {
                VisualStateManager.GoToState(tb, "Error", false);
                return 0;
            }
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IgnoreUpdate) return;

            TextBox tb = (TextBox)sender;
            Object tag = tb.Tag;
            double d = ParseDouble(tb);
            var motor = TrainSoundData.MotorSpec;
            if (tag.Equals("SR")) motor.R_s = d;
            else if (tag.Equals("RR")) motor.R_r = d;
            else if (tag.Equals("SI")) motor.L_s = d;
            else if (tag.Equals("RI")) motor.L_r = d;
            else if (tag.Equals("MI")) motor.L_m = d;
            else if (tag.Equals("PL")) motor.NP = d;
            else if (tag.Equals("D")) motor.DAMPING = d;
            else if (tag.Equals("RIM")) motor.INERTIA = d;
            else if (tag.Equals("SF")) motor.STATICF = d;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (YamlTrainSoundData.HarmonicData)Motor_Harmonics_List.SelectedItem;
            if (item == null) return;
            Motor_Harmonic_Edit_Frame.Navigate(new HarmonicSetting(item, Motor_Harmonics_List));
        }

        private void Update_ListView()
        {
            Motor_Harmonics_List.ItemsSource = TrainSoundData.HarmonicSound;
            Motor_Harmonics_List.Items.Refresh();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            Object tag = mi.Tag;

            if (tag.Equals("Add"))
            {
                TrainSoundData.HarmonicSound.Add(new YamlTrainSoundData.HarmonicData());
                Update_ListView();
            }
            else if (tag.Equals("Remove"))
            {
                if (Motor_Harmonics_List.SelectedIndex < 0) return;
                Motor_Harmonic_Edit_Frame.Navigate(null);
                TrainSoundData.HarmonicSound.RemoveAt(Motor_Harmonics_List.SelectedIndex);
                Update_ListView();  
            }
            else if (tag.Equals("Clone"))
            {
                if (Motor_Harmonics_List.SelectedIndex < 0) return;
                YamlTrainSoundData.HarmonicData harmonic_Data = (YamlTrainSoundData.HarmonicData)Motor_Harmonics_List.SelectedItem;
                TrainSoundData.HarmonicSound.Add(harmonic_Data.Clone());
                Update_ListView();
            }
        }
    }
}
