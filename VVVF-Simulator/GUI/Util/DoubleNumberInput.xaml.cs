﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VvvfSimulator.GUI.Util
{
    /// <summary>
    /// Double_Ask_Form.xaml の相互作用ロジック
    /// </summary>
    public partial class DoubleNumberInput : Window
    {
        public DoubleNumberInput(Window Owner,string Title,double DefaultValue=10.0)
        {
            this.Owner = Owner;
            InitializeComponent();
            DescriptionBox.Content = Title;
            NumberEnterBox.Text = DefaultValue.ToString();
            ShowDialog();
        }

        private double EnteredValue = 0.0;
        public double GetEnteredValue()
        {
            return EnteredValue;
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBox tb = NumberEnterBox;
            double d = ParseDouble(tb);
            EnteredValue = d;
            Close();
        }

        private void NumberEnterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = NumberEnterBox;
            ParseDouble(tb);
        }

        private void OnWindowControlButtonClick(object sender, RoutedEventArgs e)
        {
            Button? btn = sender as Button;
            if (btn == null) return;
            string? tag = btn.Tag.ToString();
            if (tag == null) return;

            if (tag.Equals("Close"))
            {
                EnteredValue = double.NaN;
                Close();
            }
                
        }
    }
}
