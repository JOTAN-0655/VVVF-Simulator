﻿using System;
using System.Windows;
using System.Windows.Controls;
using VvvfSimulator.GUI.Resource.MyUserControl;
using static VvvfSimulator.GUI.MIDIConvert.Main;

namespace VvvfSimulator.GUI.MIDIConvert
{
    /// <summary>
    /// MIDIConvert_Config.xaml の相互作用ロジック
    /// </summary>
    public partial class Config : Window
    {
        private MidiConvertConfig config;
        public Config(Window owner, MidiConvertConfig config)
        {
            Owner = owner;
            InitializeComponent();
            this.config = config;

            BtnMultiThread.SetToggled(config.MultiThread);

        }

        private void EnableButton_OnClicked(object sender, EventArgs e)
        {
            EnableButton? enableButton = sender as EnableButton;
            if (enableButton == null) return;
            String? tag = enableButton.Tag.ToString();
            if(tag == null) return;

            if(tag.Equals("MultiThread")) config.MultiThread = enableButton.IsToggled();
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
