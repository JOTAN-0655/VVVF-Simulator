﻿using Microsoft.Win32;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VvvfSimulator.Yaml.MasconControl;
using static VvvfSimulator.Yaml.MasconControl.YamlMasconAnalyze;
using static VvvfSimulator.Yaml.MasconControl.YamlMasconAnalyze.YamlMasconData;

namespace VvvfSimulator.GUI.Mascon
{
    public class SmallTitleConverter : IValueConverter
    {
        // 2.Convertメソッドを実装
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not YamlMasconDataPoint)
                return DependencyProperty.UnsetValue;

            YamlMasconDataPoint val = (YamlMasconDataPoint)value;

            return "Rate : " + String.Format("{0:F2}", val.rate) + " , Duration : " + String.Format("{0:F2}", val.duration) + " , Mascon : " + val.mascon_on.ToString() + " , Brake : " + val.brake;
        }

        // 3.ConvertBackメソッドを実装
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception("Wow!");
        }
    }

    public class BigTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not YamlMasconDataPoint)
                return DependencyProperty.UnsetValue;

            YamlMasconDataPoint val = (YamlMasconDataPoint)value;

            return val.order;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception("Wow!");
        }
    }

    /// <summary>
    /// Generation_Mascon_Control_Window.xaml の相互作用ロジック
    /// </summary>
    public partial class MasconControlMain
    {
        public MasconControlMain()
        {
            InitializeComponent();
            mascon_control_list.ItemsSource = YamlMasconManage.CurrentData.points;
        }

        private String load_path = "";
        private String load_midi_path = "";
        private void FileMenuClick(object sender, RoutedEventArgs e)
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

                if (YamlMasconManage.LoadYaml(dialog.FileName))
                    MessageBox.Show("Load OK.", "Great", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Invalid yaml or path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                load_path = dialog.FileName;

                UpdateItemList();

            }
            else if (tag.Equals("Save"))
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Yaml (*.yaml)|*.yaml",
                    FileName = Path.GetFileName(load_path)
                };

                // ダイアログを表示する
                if (dialog.ShowDialog() == false) return;

                if (YamlMasconManage.SaveYaml(dialog.FileName))
                    MessageBox.Show("Save OK.", "Great", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Error occurred on saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem button = (MenuItem)sender;
            Object? tag = button.Tag;
            if (tag == null) return;
            if (tag.Equals("Midi"))
            {
                MasconControlMidi gmcm = new(Path.GetDirectoryName(load_midi_path));
                gmcm.ShowDialog();
                var load_data = gmcm.loadData;
                try
                {
                    load_midi_path = load_data.path;
                    YamlMasconData? data = YamlMasconMidi.Convert(load_data);
                    if (data == null) return;
                    YamlMasconManage.CurrentData = data;
                    UpdateItemList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (tag.Equals("Reset"))
            {
                YamlMasconManage.CurrentData = YamlMasconManage.DefaultData.Clone();
                UpdateItemList();
            }
        }

        private void ControlItemContextMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            Object? tag = mi.Tag;

            if (tag.Equals("Delete"))
            {
                int selected = mascon_control_list.SelectedIndex;
                if (selected < 0) return;
                YamlMasconManage.CurrentData.points.RemoveAt(selected);
                UpdateItemList();
            } else if (tag.Equals("Copy"))
            {
                var selected_item = mascon_control_list.SelectedItem;
                if (selected_item == null) return;

                YamlMasconDataPoint data = (YamlMasconDataPoint)selected_item;
                YamlMasconManage.CurrentData.points.Add(data.Clone());

                UpdateItemList();
            }           
        }

        private void ItemListContextMenuClick(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            Object? tag = mi.Tag;

            if (tag.Equals("Sort"))
            {
                YamlMasconManage.CurrentData.points.Sort((a, b) => Math.Sign(a.order - b.order));
                UpdateItemList();
            }
        }

        public void UpdateItemList()
        {
            mascon_control_list.ItemsSource = YamlMasconManage.CurrentData.points;
            mascon_control_list.Items.Refresh();
        }

        private void ControlItemSelected(object sender, SelectionChangedEventArgs e)
        {
            var selected_item = mascon_control_list.SelectedItem;
            if (selected_item == null) return;

            edit_view_frame.Navigate(new MasconControlEdit(this, (YamlMasconDataPoint)selected_item));
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            Object tag = btn.Tag;

            if (tag.Equals("Add"))
            {
                YamlMasconManage.CurrentData.points.Add(new(YamlMasconManage.CurrentData.points.Count));
                UpdateItemList();
            }
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
