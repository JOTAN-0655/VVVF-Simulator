﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVF_Simulator.Yaml.VVVF_Sound;
using static VVVF_Simulator.Generation.Generate_Common;
using static VVVF_Simulator.Generation.Generate_Common.GenerationBasicParameter;
using static VVVF_Simulator.MainWindow;
using static VVVF_Simulator.VVVF_Calculate;
using static VVVF_Simulator.VVVF_Structs;
using static VVVF_Simulator.Yaml.Mascon_Control.Yaml_Mascon_Analyze;

namespace VVVF_Simulator.Generation.Video.WaveForm
{
    public class Generate_WaveForm_UV
    {

        /// <summary>
        /// Do clone before call this!
        /// </summary>
        /// <param name="Control"></param>
        /// <param name="PWM_Data"></param>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="WaveHeight"></param>
        /// <param name="Delta"></param>
        /// <returns></returns>
        public static Bitmap Get_WaveForm_Image(
            VVVF_Values Control,
            PWM_Calculate_Values PWM_Data,
            int Width, 
            int Height, 
            int WaveHeight,
            int WaveWidth,
            int Delta,
            int Spacing
        )
        {
            int Count = (Width - Spacing * 2) * Delta;
            Wave_Values[] values = new Wave_Values[Count];
            for (int i = 0; i < Count; i++)
            {
                Wave_Values value = calculate_values(Control, PWM_Data, Math.PI / 6.0);
                values[i] = value;
                Control.add_Saw_Time(2 / (60.0 * Count));
                Control.add_Sine_Time(2 / (60.0 * Count));
            }
            return Get_WaveForm_Image(ref values, Width, Height, WaveHeight, WaveWidth, Spacing);
        }

        public static Bitmap Get_WaveForm_Image(
            ref Wave_Values[] UVW,
            int Width,
            int Height,
            int WaveHeight,
            int WaveWidth,
            int Spacing
        )
        {
            Bitmap image = new(Width, Height);
            Graphics g = Graphics.FromImage(image);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, Width, Height);

            List<int> points_x = new();
            List<int> points_y = new();

            points_x.Add(Spacing);
            points_y.Add((int)(Height / 2.0));

            int pre_pwm = 0;

            for (int i = 0; i < UVW.Length; i++)
            {
                int pwm = UVW[i].U - UVW[i].V;
                if (pre_pwm != pwm)
                {
                    points_x.Add((int)(i / (double)UVW.Length * (Width - Spacing * 2)) + Spacing);
                    points_y.Add((int)(-pre_pwm * WaveHeight + Height / 2.0));

                    points_x.Add((int)(i / (double)UVW.Length * (Width - Spacing * 2)) + Spacing);
                    points_y.Add((int)(-pwm * WaveHeight + Height / 2.0));
                    pre_pwm = pwm;
                }
            }

            points_x.Add(Width - Spacing);
            points_y.Add((int)(-pre_pwm * WaveHeight + Height / 2.0));

            for (int i = 0; i < points_x.Count - 1; i++)
            {
                int x_1 = points_x[i];
                int x_2 = points_x[i + 1];
                int y_1 = points_y[i];
                int y_2 = points_y[i + 1];
                g.DrawLine(new Pen(Color.Black, WaveWidth), x_1, y_1, x_2, y_2);
            }

            g.Dispose();
            return image;
        }

        public static void Generate_UV_1(GenerationBasicParameter generationBasicParameter, String fileName)
        {
            Yaml_VVVF_Sound_Data vvvfData = generationBasicParameter.vvvfData;
            Yaml_Mascon_Data_Compiled masconData = generationBasicParameter.masconData;
            ProgressData progressData = generationBasicParameter.progressData;

            VVVF_Values control = new();
            control.reset_control_variables();
            control.reset_all_variables();

            control.set_Allowed_Random_Freq_Move(false);

            int fps = 60;

            int image_width = 2880;
            int image_height = 540;

            int wave_height = 100;
            int calculate_div = 30;

            VideoWriter vr = new(fileName, OpenCvSharp.FourCC.H264, fps, new OpenCvSharp.Size(image_width, image_height));


            if (!vr.IsOpened())
            {
                return;
            }

            // Progress Initialize
            progressData.Total = masconData.GetEstimatedSteps(1.0 / fps) + 120;

            Boolean START_WAIT = true;
            if (START_WAIT)
            {
                Bitmap image = new(image_width, image_height);
                Graphics g = Graphics.FromImage(image);
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);
                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);
                for (int i = 0; i < 60; i++)
                {
                    // PROGRESS CHANGE
                    progressData.Progress++;

                    vr.Write(mat);
                }
                g.Dispose();
                image.Dispose();
            }

            Boolean loop = true;
            while (loop)
            {

                control.set_Sine_Time(0);
                control.set_Saw_Time(0);

                Control_Values cv = new()
                {
                    brake = control.is_Braking(),
                    mascon_on = !control.is_Mascon_Off(),
                    free_run = control.is_Free_Running(),
                    wave_stat = control.get_Control_Frequency()
                };
                PWM_Calculate_Values calculated_Values = Yaml_VVVF_Wave.calculate_Yaml(control, cv, vvvfData);

                Bitmap image = Get_WaveForm_Image(control, calculated_Values, image_width, image_height, wave_height, 2, calculate_div, 100);


                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);
                vr.Write(mat);
                mat.Dispose();
                ms.Dispose();

                MemoryStream resized_ms = new();
                Bitmap resized = new(image, image_width / 2, image_height / 2);
                resized.Save(resized_ms, ImageFormat.Png);
                byte[] resized_img = resized_ms.GetBuffer();
                Mat resized_mat = OpenCvSharp.Mat.FromImageData(resized_img);
                Cv2.ImShow("Wave Form", resized_mat);
                Cv2.WaitKey(1);
                resized_mat.Dispose();
                resized_ms.Dispose();

                image.Dispose();

                loop = Check_For_Freq_Change(control, masconData, vvvfData.mascon_data, 1.0 / fps);
                if (progressData.Cancel) loop = false;

                // PROGRESS CHANGE
                progressData.Progress++;
            }

            Boolean END_WAIT = true;
            if (END_WAIT)
            {
                Bitmap image = new(image_width, image_height);
                Graphics g = Graphics.FromImage(image);
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);
                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);
                for (int i = 0; i < 60; i++)
                {
                    // PROGRESS CHANGE
                    progressData.Progress++;

                    vr.Write(mat);
                }
                g.Dispose();
                image.Dispose();
            }

            vr.Release();
            vr.Dispose();
        }

        public static void Generate_UV_2(GenerationBasicParameter generationBasicParameter, String fileName)
        {
            Yaml_VVVF_Sound_Data vvvfData = generationBasicParameter.vvvfData;
            Yaml_Mascon_Data_Compiled masconData = generationBasicParameter.masconData;
            ProgressData progressData = generationBasicParameter.progressData;

            VVVF_Values control = new();
            control.reset_control_variables();
            control.reset_all_variables();
            control.set_Allowed_Random_Freq_Move(false);

            int fps = 60;

            int image_width = 2000;
            int image_height = 500;

            int wave_height = 100;
            int calculate_div = 10;

            VideoWriter vr = new(fileName, OpenCvSharp.FourCC.H264, fps, new OpenCvSharp.Size(image_width, image_height));


            if (!vr.IsOpened())
            {
                return;
            }

            // Progress Initialize
            progressData.Total = masconData.GetEstimatedSteps(1.0 / fps) + 120;

            Boolean START_WAIT = true;
            if (START_WAIT)
            {
                Bitmap image = new(image_width, image_height);
                Graphics g = Graphics.FromImage(image);
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);
                g.DrawLine(new Pen(Color.Gray), 0, image_height / 2, image_width, image_height / 2);
                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);

                for (int i = 0; i < 60; i++)
                {
                    // PROGRESS CHANGE
                    progressData.Progress++;

                    vr.Write(mat);
                }
                g.Dispose();
                image.Dispose();
            }

            Boolean loop = true;
            while (loop)
            {

                control.set_Sine_Time(0);
                control.set_Saw_Time(0);

                Control_Values cv = new()
                {
                    brake = control.is_Braking(),
                    mascon_on = !control.is_Mascon_Off(),
                    free_run = control.is_Free_Running(),
                    wave_stat = control.get_Control_Frequency()
                };
                PWM_Calculate_Values calculated_Values = Yaml_VVVF_Wave.calculate_Yaml(control, cv, vvvfData);

                Bitmap image = Get_WaveForm_Image(control, calculated_Values, image_width, image_height, wave_height, 1, calculate_div, 0);

                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);

                Cv2.ImShow("Wave Form View", mat);
                Cv2.WaitKey(1);

                vr.Write(mat);

                image.Dispose();
                loop = Check_For_Freq_Change(control, masconData, vvvfData.mascon_data, 1.0 / fps);
                if(progressData.Cancel) loop = false;

                // PROGRESS CHANGE
                progressData.Progress++;

            }

            Boolean END_WAIT = true;
            if (END_WAIT)
            {
                Bitmap image = new(image_width, image_height);
                Graphics g = Graphics.FromImage(image);
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);
                g.DrawLine(new Pen(Color.Gray), 0, image_height / 2, image_width, image_height / 2);
                MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);
                for (int i = 0; i < 60; i++)
                {
                    // PROGRESS CHANGE
                    progressData.Progress++;

                    vr.Write(mat);
                }
                g.Dispose();
                image.Dispose();
            }

            vr.Release();
            vr.Dispose();
        }
    }
}
