﻿using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static VvvfSimulator.VvvfCalculate;
using static VvvfSimulator.Generation.GenerateCommon;
using VvvfSimulator.Yaml.VvvfSound;
using static VvvfSimulator.VvvfStructs;
using static VvvfSimulator.Yaml.MasconControl.YamlMasconAnalyze;
using static VvvfSimulator.Generation.GenerateCommon.GenerationBasicParameter;
using VvvfSimulator.GUI.Util;

namespace VvvfSimulator.Generation.Video.WaveForm
{
    public class GenerateWaveFormUVW
    {
        private static int image_width = 1500;
        private static int image_height = 1000;
        private static int calculate_div = 10;
        public static Bitmap GetImage(VvvfValues control, ControlStatus cv, YamlVvvfSoundData vvvfData)
        {
            control.SetSawTime(0);
            control.SetSineTime(0);

            Bitmap image = new(image_width, image_height);
            Graphics g = Graphics.FromImage(image);
            g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);

            for (int i = 0; i < image_width * calculate_div; i++)
            {
                int[] points_U = new int[2];
                int[] points_V = new int[2];
                int[] points_W = new int[2];

                for (int j = 0; j < 2; j++)
                {
                    PwmCalculateValues calculated_Values = YamlVvvfWave.CalculateYaml(control, cv, vvvfData);
                    WaveValues value = CalculatePhases(control, calculated_Values, 0);

                    points_U[j] = value.U;
                    points_V[j] = value.V;
                    points_W[j] = value.W;

                    if (j == 0)
                    {
                        control.AddSawTime(Math.PI / (120000.0 * calculate_div));
                        control.AddSineTime(Math.PI / (120000.0 * calculate_div));
                    }
                }

                //U
                g.DrawLine(new Pen(Color.Black),
                    (int)Math.Round(i / (double)calculate_div),
                    points_U[0] * -100 + 300,
                    (int)Math.Round(((points_U[0] != points_U[1]) ? i : i + 1) / (double)calculate_div),
                    points_U[1] * -100 + 300
                );

                //V
                g.DrawLine(new Pen(Color.Black),
                    (int)Math.Round(i / (double)calculate_div),
                    points_V[0] * -100 + 600,
                    (int)Math.Round(((points_V[0] != points_V[1]) ? i : i + 1) / (double)calculate_div),
                    points_V[1] * -100 + 600
                );

                //W
                g.DrawLine(new Pen(Color.Black),
                    (int)Math.Round(i / (double)calculate_div),
                    points_W[0] * -100 + 900,
                    (int)Math.Round(((points_W[0] != points_W[1]) ? i : i + 1) / (double)calculate_div),
                    points_W[1] * -100 + 900
                );
            }

            g.Dispose();
            return image;
        }

        private BitmapViewerManager? Viewer { get; set; }
        public void ExportVideo(GenerationBasicParameter generationBasicParameter, String fileName)
        {
            MainWindow.Invoke(() => Viewer = new BitmapViewerManager());
            Viewer?.Show();

            YamlVvvfSoundData vvvfData = generationBasicParameter.vvvfData;
            YamlMasconDataCompiled masconData = generationBasicParameter.masconData;
            ProgressData progressData = generationBasicParameter.progressData;

            VvvfValues control = new();
            control.ResetControlValues();
            control.ResetMathematicValues();

            int fps = 60;
            VideoWriter vr = new(fileName, OpenCvSharp.FourCC.H264, fps, new OpenCvSharp.Size(image_width, image_height));
            if (!vr.IsOpened()) return;

            // PROGRESS INITIALIZE
            progressData.Total = masconData.GetEstimatedSteps(1.0 / fps) + 2 * fps;

            bool START_FRAMES = true;
            if (START_FRAMES)
            {

                ControlStatus cv = new()
                {
                    brake = true,
                    mascon_on = true,
                    free_run = false,
                    wave_stat = 0
                };
                Bitmap final_image = GetImage(control, cv, vvvfData);

                AddImageFrames(final_image, fps, vr);
                Viewer?.SetImage(final_image);
                final_image.Dispose();
            }

            //PROGRESS ADD
            progressData.Progress += fps;

            while (true)
            {
                ControlStatus cv = new()
                {
                    brake = control.IsBraking(),
                    mascon_on = !control.IsMasconOff(),
                    free_run = control.IsFreeRun(),
                    wave_stat = control.GetControlFrequency()
                };
                Bitmap final_image = GetImage(control, cv, vvvfData);

                MemoryStream ms = new();
                final_image.Save(ms, ImageFormat.Png);
                byte[] img = ms.GetBuffer();
                Mat mat = OpenCvSharp.Mat.FromImageData(img);
                vr.Write(mat);
                ms.Dispose();
                mat.Dispose();
                Viewer?.SetImage(final_image);
                final_image.Dispose();

                if (!CheckForFreqChange(control, masconData, vvvfData.MasconData, 1.0 / fps)) break;
                if (progressData.Cancel) break;
                progressData.Progress++;
            }

            bool END_FRAMES = true;
            if (END_FRAMES)
            {
                ControlStatus cv = new()
                {
                    brake = true,
                    mascon_on = true,
                    free_run = false,
                    wave_stat = 0
                };
                Bitmap final_image = GetImage(control, cv, vvvfData);
                AddImageFrames(final_image, fps, vr);
                Viewer?.SetImage(final_image);
                final_image.Dispose();
            }

            //PROGRESS ADD
            progressData.Progress += fps;

            vr.Release();
            vr.Dispose();

            Viewer?.Close();
        }

    }
}
