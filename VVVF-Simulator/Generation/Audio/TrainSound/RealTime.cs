﻿using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using VvvfSimulator.Properties;
using VvvfSimulator.Yaml.VvvfSound;
using static VvvfSimulator.Generation.Audio.GenerateRealTimeCommon;
using static VvvfSimulator.Generation.Audio.TrainSound.Audio;
using static VvvfSimulator.Generation.Motor.GenerateMotorCore;
using static VvvfSimulator.Yaml.TrainAudioSetting.YamlTrainSoundAnalyze;
using static VvvfSimulator.Generation.Audio.TrainSound.AudioFilter;
using System.Windows;

namespace VvvfSimulator.Generation.Audio.TrainSound
{
    public class RealTime
    {
        //---------- TRAIN SOUND --------------
        private static readonly int calcCount = 512;
        private static readonly int SamplingFrequency = 192000;
        private static int Calculate(BufferedWaveProvider provider, YamlVvvfSoundData sound_data, VvvfValues control, RealTimeParameter realTime_Parameter)
        {
            static void AddSample(float value, BufferedWaveProvider provider)
            {
                byte[] soundSample = BitConverter.GetBytes((float)value);
                if (!BitConverter.IsLittleEndian) Array.Reverse(soundSample);
                provider.AddSamples(soundSample, 0, 4);
            }

            while (true)
            {
                int v = RealTime_CheckForFreq(control, realTime_Parameter, calcCount / (double)SamplingFrequency);
                if (v != -1) return v;

                for (int i = 0; i < calcCount; i++)
                {
                    control.AddSineTime(1.0 / SamplingFrequency);
                    control.AddSawTime(1.0 / SamplingFrequency);
                    control.AddGenerationCurrentTime(1.0 / SamplingFrequency);

                    double value = CalculateTrainSound(control, sound_data , realTime_Parameter.Motor, realTime_Parameter.TrainSoundData);
                    AddSample((float)value, provider);
                }

                while (provider.BufferedBytes - calcCount > Settings.Default.RealTime_Train_BuffSize) ;
            }
        }

        public static void Generate(YamlVvvfSoundData Sound, RealTimeParameter Param)
        {
            Param.quit = false;
            Param.VvvfSoundData = Sound;
            Param.Motor = new MotorData() { 
                SIM_SAMPLE_FREQ = SamplingFrequency ,
                motor_Specification = Param.TrainSoundData.MotorSpec.Clone(),
            };

            YamlTrainSoundData SoundConfiguration = Param.TrainSoundData;

            VvvfValues control = new();
            control.ResetMathematicValues();
            control.ResetControlValues();
            Param.Control = control;

            while (true)
            {
                var bufferedWaveProvider = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(SamplingFrequency,1));
                ISampleProvider sampleProvider = bufferedWaveProvider.ToSampleProvider();
                if (SoundConfiguration.UseFilteres) sampleProvider = new MonauralFilter(sampleProvider, SoundConfiguration.GetFilteres(SamplingFrequency));
                if (SoundConfiguration.UseImpulseResponse) sampleProvider = new CppConvolutionFilter(sampleProvider, 4096, SoundConfiguration.ImpulseResponse);

                var mmDevice = new MMDeviceEnumerator().GetDevice(Param.AudioDeviceId);
                IWavePlayer wavPlayer = new WasapiOut(mmDevice, AudioClientShareMode.Shared, false, 0);

                wavPlayer.Init(sampleProvider);
                wavPlayer.Play();

                int stat;
                try
                {
                    stat = Calculate(bufferedWaveProvider, Sound, control, Param);
                }
                catch
                {
                    wavPlayer.Stop();
                    wavPlayer.Dispose();

                    mmDevice.Dispose();
                    bufferedWaveProvider.ClearBuffer();

                    MessageBox.Show("An error occured on Audio processing.");

                    throw;
                }

                wavPlayer.Stop();
                wavPlayer.Dispose();

                mmDevice.Dispose();
                bufferedWaveProvider.ClearBuffer();

                if (stat == 0) break;
            }


        }
    }
}
