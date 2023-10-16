﻿using static VvvfSimulator.Generation.GenerateCommon;
using static VvvfSimulator.VvvfValues;
using static VvvfSimulator.MyMath;
using System;
using static VvvfSimulator.VvvfCalculate.Amplitude_Argument;
using System.Threading.Tasks;
using static VvvfSimulator.VvvfStructs;
using static VvvfSimulator.VvvfStructs.PulseMode;
using static VvvfSimulator.Yaml.VVVFSound.YamlVvvfSoundData.YamlControlData;

namespace VvvfSimulator
{
	public class VvvfCalculate
	{
		//
		// Basic Calculation
		//
		public static double GetSaw(double x)
		{
			double val;
			double fixed_x = x - (double)((int)(x * M_1_2PI) * M_2PI);
			if (0 <= fixed_x && fixed_x < M_PI_2)
				val = M_2_PI * fixed_x;
			else if (M_PI_2 <= fixed_x && fixed_x < 3.0 * M_PI_2)
				val = -M_2_PI * fixed_x + 2;
			else
				val = M_2_PI * fixed_x - 4;

			return -val;
		}

		public static double GetSine(double x)
		{
			return MyMath.sin(x);
		}

		public static double GetSquare(double x)
        {
			double fixed_x = x - (double)((int)(x * M_1_2PI) * M_2PI);
			if (fixed_x / M_PI > 1) return -1;
			return 1;
		}

		public static double GetModifiedSine(double x, int level)
        {
			double sine = GetSine(x) * level;
			double value = Math.Round(sine) / level;
			return value;
		}

		public static double GetModifiedSaw(double x)
        {
			double sine = -GetSaw(x) * M_PI_2;
			int D = sine > 0 ? 1 : -1;
			if (Math.Abs(sine) > 0.5) sine = D;

			return sine;
        }

		public static double GetSineValueWithHarmonic(PulseMode mode,double x,double amplitude)
        {
			double sin_value = 0;

			if (mode.Base_Wave.Equals(BaseWaveType.Saw))
				sin_value = -GetSaw(x);
			else if (mode.Base_Wave.Equals(BaseWaveType.Sine))
				sin_value = GetSine(x);
			else if (mode.Base_Wave.Equals(BaseWaveType.Modified_Sine_1))
				sin_value = GetModifiedSine(x, 1);
			else if (mode.Base_Wave.Equals(BaseWaveType.Modified_Sine_2))
				sin_value = GetModifiedSine(x, 2);

			else if (mode.Base_Wave.Equals(BaseWaveType.Modified_Saw_1))
				sin_value = GetModifiedSaw(x);


			for (int i = 0; i < mode.pulse_harmonics.Count; i++)
			{
				PulseHarmonic harmonic = mode.pulse_harmonics[i];
				double harmonic_value = 0, harmonic_x = harmonic.harmonic * (x + harmonic.initial_phase);
				if (harmonic.type == PulseHarmonic.PulseHarmonicType.Sine)
					harmonic_value = GetSine(harmonic_x);
				else if(harmonic.type == PulseHarmonic.PulseHarmonicType.Saw)
					harmonic_value = -GetSaw(harmonic_x);
				else if (harmonic.type == PulseHarmonic.PulseHarmonicType.Square)
					harmonic_value = GetSquare(harmonic_x);

				sin_value += harmonic_value * harmonic.amplitude;
			}

			sin_value = sin_value > 1 ? 1 : sin_value < -1 ? -1 : sin_value;

			sin_value *= amplitude;
			return sin_value;
		}

		public static int ModulateSin(double sin_value, double saw_value)
		{
			return sin_value > saw_value ? 1 : 0;
		}

		//
		// Pulse Calculation
		//
		public static int GetWideP3(double time, double angle_frequency, double initial_phase, double voltage, bool saw_oppose)
		{
			double sin = GetSine(time * angle_frequency + initial_phase);
			double saw = GetSaw(time * angle_frequency + initial_phase);
			if (saw_oppose)
				saw = -saw;
			double pwm = ((sin - saw > 0) ? 1 : -1) * voltage;
			double nega_saw = (saw > 0) ? saw - 1 : saw + 1;
			int gate = ModulateSin(pwm, nega_saw) * 2;
			return gate;
		}
		public static int Get3LevelP1(double x, double voltage)
        {
			double sine = GetSine(x);
			int D = sine > 0 ? 1 : -1;
			double voltage_fix = D * (1 - voltage);

			int gate = (D * (sine - voltage_fix) > 0) ? D : 0;
			gate += 1;
			return gate;
        }
		public static int GetPulseWithSaw(double x, double carrier_initial_phase, double voltage, double carrier_mul, bool saw_oppose)
		{
			double carrier_saw = -GetSaw(carrier_mul * x + carrier_initial_phase);
			double saw = -GetSaw(x);
			if (saw_oppose)
				saw = -saw;
			double pwm = (saw > 0) ? voltage : -voltage;
			int gate = ModulateSin(pwm, carrier_saw) * 2;
			return gate;
		}
		public static int GetPulseWithSwitchAngle(
			double alpha1,
			double alpha2,
			double alpha3,
			double alpha4,
			double alpha5,
			double alpha6,
			double alpha7,
			int flag,
			double time, double sin_angle_frequency, double initial_phase)
		{
			double theta = (initial_phase + time * sin_angle_frequency) - (double)((int)((initial_phase + time * sin_angle_frequency) * M_1_2PI) * M_2PI);

			int PWM_OUT = (((((theta <= alpha2) && (theta >= alpha1)) || ((theta <= alpha4) && (theta >= alpha3)) || ((theta <= alpha6) && (theta >= alpha5)) || ((theta <= M_PI - alpha1) && (theta >= M_PI - alpha2)) || ((theta <= M_PI - alpha3) && (theta >= M_PI - alpha4)) || ((theta <= M_PI - alpha5) && (theta >= M_PI - alpha6))) && ((theta <= M_PI) && (theta >= 0))) || (((theta <= M_PI - alpha7) && (theta >= alpha7)) && ((theta <= M_PI) && (theta >= 0)))) || ((!(((theta <= alpha2 + M_PI) && (theta >= alpha1 + M_PI)) || ((theta <= alpha4 + M_PI) && (theta >= alpha3 + M_PI)) || ((theta <= alpha6 + M_PI) && (theta >= alpha5 + M_PI)) || ((theta <= M_2PI - alpha1) && (theta >= M_2PI - alpha2)) || ((theta <= M_2PI - alpha3) && (theta >= M_2PI - alpha4)) || ((theta <= M_2PI - alpha5) && (theta >= M_2PI - alpha6))) && ((theta <= M_2PI) && (theta >= M_PI))) && !((theta <= M_2PI - alpha7) && (theta >= M_PI + alpha7)) && (theta <= M_2PI) && (theta >= M_PI)) ? 1 : -1;

			int gate = flag == 'A' ? -PWM_OUT + 1 : PWM_OUT + 1;
			return gate;

		}

		//
		// Amplitude Calculation
		//
		public enum Amplitude_Mode
        {
			Linear, Wide_3_Pulse, Inv_Proportional , Exponential,
			Linear_Polynomial,Sine
		}
		public class Amplitude_Argument
        {
			public double min_freq = 0;
			public double min_amp = 0;
			public double max_freq = 0;
			public double max_amp = 0;

			public bool disable_range_limit = false;
			public double change_const = 0.43;
			public double polynomial = 1.0;

			public double current = 0;

			public Amplitude_Argument() { }
			public Amplitude_Argument(YamlControlDataAmplitudeControl.YamlControlDataAmplitude.Yaml_Control_Data_Amplitude_Single_Parameter config, double current)
            {
				change_const = config.curve_change_rate;
				this.current = current;
				disable_range_limit = config.disable_range_limit;
				polynomial = config.polynomial;

				max_amp = config.end_amp;
				max_freq = config.end_freq;
				min_amp = config.start_amp;
				min_freq = config.start_freq;
			}
		}

		public static double get_Amplitude(Amplitude_Mode mode , Amplitude_Argument arg)
        {
			double val = 0;
			if (mode == Amplitude_Mode.Linear)
            {
				if (!arg.disable_range_limit)
				{
					if (arg.current < arg.min_freq) arg.current = arg.min_freq;
					if (arg.current > arg.max_freq) arg.current = arg.max_freq;
				}
				val = (arg.max_amp - arg.min_amp) / (arg.max_freq - arg.min_freq) * (arg.current - arg.min_freq) + arg.min_amp;
			}
				
			else if(mode == Amplitude_Mode.Wide_3_Pulse)
            {
				if (!arg.disable_range_limit)
				{
					if (arg.current < arg.min_freq) arg.current = arg.min_freq;
					if (arg.current > arg.max_freq) arg.current = arg.max_freq;
				}
				val = (0.2 * ((arg.current - arg.min_freq) * ((arg.max_amp - arg.min_amp) / (arg.max_freq - arg.min_freq)) + arg.min_amp)) + 0.8;
			}
				

			else if(mode == Amplitude_Mode.Inv_Proportional)
            {
				if (!arg.disable_range_limit)
				{
					if (arg.current < arg.min_freq) arg.current = arg.min_freq;
					if (arg.current > arg.max_freq) arg.current = arg.max_freq;
				}

				double x = get_Amplitude(Amplitude_Mode.Linear, new Amplitude_Argument()
                {
					min_freq = arg.min_freq, 
					min_amp = 1 / arg.min_amp, 
					max_freq = arg.max_freq, 
					max_amp = 1 / arg.max_amp, 
					current = arg.current,
					disable_range_limit = arg.disable_range_limit
				});

				double c = -arg.change_const;
				double k = arg.max_amp;
				double l = arg.min_amp;
				double a = 1 / ((1 / l) - (1 / k)) * (1 / (l - c) - 1 / (k - c));
				double b = 1 / (1 - (1 / l) * k) * (1 / (l - c) - (1 / l) * k / (k - c));

				//val = 1 / (6.25*x - 2.5) + 0.4;
				val = 1 / (a * x + b) + c;
			}
			else if(mode == Amplitude_Mode.Exponential)
            {

				if (!arg.disable_range_limit)
				{
					if (arg.current > arg.max_freq) arg.current = arg.max_freq;
				}

				double t = 1 / arg.max_freq *  Math.Log(arg.max_amp + 1);

				val = Math.Pow(Math.E, t * arg.current) - 1;
			}
			else if(mode == Amplitude_Mode.Linear_Polynomial)
            {
				if (!arg.disable_range_limit)
				{
					if (arg.current > arg.max_freq) arg.current = arg.max_freq;
				}

				val = Math.Pow(arg.current, arg.polynomial) / Math.Pow(arg.max_freq, arg.polynomial) * arg.max_amp;

			}
			else if (mode == Amplitude_Mode.Sine)
			{
				if (!arg.disable_range_limit)
				{
					if (arg.current > arg.max_freq) arg.current = arg.max_freq;
				}

				double x = (Math.PI * arg.current) / (2.0 * arg.max_freq);

				val = MyMath.sin(x) * arg.max_amp;
			}


			return val;
		}

		//
		// Carrier Freq Calculation
		//
		private static double GetRandomFrequency(CarrierFreq data, VvvfValues control)
		{
			if (data.range == 0) return data.base_freq;

            if (control.is_Allowed_Random_Freq_Move())
            {
				double random_freq;
				if (control.get_Random_Freq_Pre_Time() == 0 || control.get_Pre_Saw_Random_Freq() == 0)
				{
					int random_v = MyMath.my_random();
					double diff_freq = MyMath.mod_d(random_v, data.range);
					if ((random_v & 0x01) == 1)
						diff_freq = -diff_freq;
					double silent_random_freq = data.base_freq + diff_freq;
					random_freq = silent_random_freq;
					control.set_Pre_Saw_Random_Freq(silent_random_freq);
					control.set_Random_Freq_Pre_Time(control.Get_Generation_Current_Time());
				}
				else
				{
					random_freq = control.get_Pre_Saw_Random_Freq();
				}

				if (control.get_Random_Freq_Pre_Time() + data.interval < control.Get_Generation_Current_Time())
					control.set_Random_Freq_Pre_Time(0);

				return random_freq;
            }
            else
            {
				return data.base_freq;
            }
		}
		public static double GetVibratoFrequency(double lowest, double highest, double interval_time, bool continuous , VvvfValues control)
		{

			if (!control.is_Allowed_Random_Freq_Move())
				return (highest + lowest) / 2.0;

			double random_freq;
			double current_t = control.Get_Generation_Current_Time();
			double solve_t = control.get_Vibrato_Freq_Pre_Time();

			if (continuous)
			{
				if (interval_time / 2.0 > current_t - solve_t)
					random_freq = lowest + (highest - lowest) / (interval_time / 2.0) * (current_t - solve_t);
				else
					random_freq = highest + (lowest - highest) / (interval_time / 2.0) * (current_t - solve_t - interval_time / 2.0);
			}
            else
            {
				if (interval_time / 2.0 > current_t - solve_t)
					random_freq = highest;
				else
					random_freq = lowest;
			}

			if (current_t - solve_t > interval_time)
				control.set_Vibrato_Freq_Pre_Time(current_t);
			return random_freq;
        }

		//
		// VVVF Calculation
		//
		public static double check_for_mascon_off(ControlStatus cv, VvvfValues control, double max_voltage_freq)
        {
			if (cv.free_run && !cv.mascon_on && cv.wave_stat > max_voltage_freq)
			{
				control.set_Control_Frequency(max_voltage_freq);
				return max_voltage_freq;
				
			}
			else if (cv.free_run && cv.mascon_on && cv.wave_stat > max_voltage_freq)
			{
				double rolling_freq = control.get_Sine_Freq();
				control.set_Control_Frequency(rolling_freq);
				return rolling_freq;
			}
			return -1;
		}

		public static WaveValues CalculatePhases(VvvfValues control,PwmCalculateValues value, double add_initial)
        {

			if (control.get_Sine_Freq() < value.min_sine_freq && control.get_Control_Frequency() > 0) control.set_Video_Sine_Freq(value.min_sine_freq);
			else control.set_Video_Sine_Freq(control.get_Sine_Freq());

			if (value.none) return new WaveValues() { U = 0, V = 0, W = 0 };

			control.set_Video_Pulse_Mode(value.pulse_mode);
			control.set_Video_Sine_Amplitude(value.amplitude);
			if(value.carrier_freq != null) control.set_Video_Carrier_Freq_Data(value.carrier_freq.Clone());
			control.set_Video_Dipolar(value.dipolar);

			int U=0, V = 0, W = 0;
			for(int i = 0; i < 3; i++)
            {
				
				int val;
				double initial = M_2PI / 3.0 * i + add_initial;
				if (value.level == 2) val = CalculateTwoLevel(control, value, initial);
				else val = CalculateThreeLevel(control, value, initial);
				if (i == 0) U = val;
				else if (i == 1) V = val;
				else W = val;

			}

			return new WaveValues() { U = U, V = V, W = W };
        }

        public static int CalculateThreeLevel(VvvfValues control, PwmCalculateValues calculate_values, double initial_phase)
		{
			double sine_angle_freq = control.get_Sine_Angle_Freq();
			double sine_time = control.get_Sine_Time();
			double min_sine_angle_freq = calculate_values.min_sine_freq * M_2PI;
			PulseMode pulse_mode = calculate_values.pulse_mode;
			CarrierFreq freq_data = calculate_values.carrier_freq;
			double dipolar = calculate_values.dipolar;

			if (sine_angle_freq < min_sine_angle_freq && control.get_Control_Frequency() > 0)
            {
				control.set_Allowed_Sine_Time_Change(false);
				sine_angle_freq = min_sine_angle_freq;
            }
            else
				control.set_Allowed_Sine_Time_Change(true);

			if (pulse_mode.pulse_name == PulseModeNames.Async)
            {

				double desire_saw_angle_freq = (freq_data.range == 0) ? freq_data.base_freq * M_2PI : GetRandomFrequency(freq_data, control) * M_2PI;

				double saw_time = control.get_Saw_Time();
				double saw_angle_freq = control.get_Saw_Angle_Freq();

				if (desire_saw_angle_freq == 0)
					saw_time = 0;
				else
					saw_time = saw_angle_freq / desire_saw_angle_freq * saw_time;
				saw_angle_freq = desire_saw_angle_freq;

				control.set_Saw_Angle_Freq(saw_angle_freq);
				control.set_Saw_Time(saw_time);

				double sine_x = sine_time * sine_angle_freq + initial_phase;
				double sin_value = GetSineValueWithHarmonic(pulse_mode.Clone(), sine_x, calculate_values.amplitude);

				double saw_value = GetSaw(control.get_Saw_Time() * control.get_Saw_Angle_Freq());
				if (pulse_mode.Shift)
					saw_value = -saw_value;

				double changed_saw = ((dipolar != -1) ? dipolar : 0.5) * saw_value;
				int pwm_value = ModulateSin(sin_value, changed_saw + 0.5) + ModulateSin(sin_value, changed_saw - 0.5);

				return pwm_value;

				

			}
			else
			{
				double sine_x = sine_time * sine_angle_freq + initial_phase;

				if (pulse_mode.pulse_name == PulseModeNames.P_1)
                {
					if(pulse_mode.Alt_Mode == PulseAlternativeMode.Alt1)
						return Get3LevelP1(sine_x, calculate_values.amplitude);
				}
					

				int pulses = Get_Pulse_Num(pulse_mode,3);
				double saw_value = GetSaw(pulses * (sine_angle_freq * sine_time + initial_phase));
				if (pulse_mode.Shift)
					saw_value = -saw_value;
				
				double sin_value = GetSineValueWithHarmonic(pulse_mode.Clone(), sine_x, calculate_values.amplitude);

				double changed_saw = ((dipolar != -1) ? dipolar : 0.5) * saw_value;
				int pwm_value = ModulateSin(sin_value, changed_saw + 0.5) + ModulateSin(sin_value, changed_saw - 0.5);

				control.set_Saw_Angle_Freq(sine_angle_freq * pulses);
				control.set_Saw_Time(sine_time);

				return pwm_value;
			}

			
		}

		public static int CalculateTwoLevel (VvvfValues control , PwmCalculateValues calculate_values, double initial_phase)
		{
			double sin_angle_freq = control.get_Sine_Angle_Freq();
			double sin_time = control.get_Sine_Time();
			double min_sine_angle_freq = calculate_values.min_sine_freq * M_2PI;
			if (sin_angle_freq < min_sine_angle_freq && control.get_Control_Frequency() > 0)
			{
				control.set_Allowed_Sine_Time_Change(false);
				sin_angle_freq = min_sine_angle_freq;
			}
			else
				control.set_Allowed_Sine_Time_Change(true);

			double saw_time = control.get_Saw_Time();
			double saw_angle_freq = control.get_Saw_Angle_Freq();

			double amplitude = calculate_values.amplitude;
			PulseMode pulse_mode = calculate_values.pulse_mode;
			PulseModeNames pulse_name = pulse_mode.pulse_name;
			CarrierFreq carrier_freq_data = calculate_values.carrier_freq;

			if (calculate_values.none) 
				return 0;


            switch (pulse_name)
            {

				case PulseModeNames.P_Wide_3 : return GetWideP3(sin_time, sin_angle_freq, initial_phase, amplitude, false);

				case PulseModeNames.Async: 
					{
						double desire_saw_angle_freq = (carrier_freq_data.range == 0) ? carrier_freq_data.base_freq * M_2PI : GetRandomFrequency(carrier_freq_data, control) * M_2PI;

						if (desire_saw_angle_freq == 0)
							saw_time = 0;
						else
							saw_time = saw_angle_freq / desire_saw_angle_freq * saw_time;
						saw_angle_freq = desire_saw_angle_freq;

						double sine_x = sin_time * sin_angle_freq + initial_phase;
						double sin_value = GetSineValueWithHarmonic(pulse_mode.Clone(), sine_x, amplitude);


						double saw_value = GetSaw(saw_time * saw_angle_freq);
						int pwm_value = ModulateSin(sin_value, saw_value) * 2;

						control.set_Saw_Angle_Freq(saw_angle_freq);
						control.set_Saw_Time(saw_time);

						return pwm_value;
					}

				case PulseModeNames.CHMP_15:
                    {
						if (pulse_mode.Alt_Mode == PulseAlternativeMode.Default)
						{
							return GetPulseWithSwitchAngle(
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 5] * M_PI_180,
								SwitchAngles._7Alpha[(int)(1000 * amplitude) + 1, 6] * M_PI_180,
								SwitchAngles._7Alpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase
							);
						}
						else if (pulse_mode.Alt_Mode == PulseAlternativeMode.Alt1)
						{
							return GetPulseWithSwitchAngle(
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 5] * M_PI_180,
								SwitchAngles._7Alpha_Old[(int)(1000 * amplitude) + 1, 6] * M_PI_180,
								SwitchAngles._7OldAlpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase
							);
						}
						break;
					}

				case PulseModeNames.CHMP_Wide_15:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 0] * M_PI_180,
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 1] * M_PI_180,
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 2] * M_PI_180,
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 3] * M_PI_180,
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 4] * M_PI_180,
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 5] * M_PI_180,
						   SwitchAngles._7WideAlpha[(int)(1000 * amplitude) - 999, 6] * M_PI_180,
						   'B', sin_time, sin_angle_freq, initial_phase);
					}

				case PulseModeNames.CHMP_13:
                    {
						if (pulse_mode.Alt_Mode == PulseAlternativeMode.Default)
						{
							return GetPulseWithSwitchAngle(
								SwitchAngles._6Alpha[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
								SwitchAngles._6Alpha[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
								SwitchAngles._6Alpha[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
								SwitchAngles._6Alpha[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
								SwitchAngles._6Alpha[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
								SwitchAngles._6Alpha[(int)(1000 * amplitude) + 1, 5] * M_PI_180,
								M_PI_2,
								SwitchAngles._6Alpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase
							);
						}
						else if (pulse_mode.Alt_Mode == PulseAlternativeMode.Alt1)
						{
							return GetPulseWithSwitchAngle(
								SwitchAngles._6Alpha_Old[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
								SwitchAngles._6Alpha_Old[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
								SwitchAngles._6Alpha_Old[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
								SwitchAngles._6Alpha_Old[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
								SwitchAngles._6Alpha_Old[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
								SwitchAngles._6Alpha_Old[(int)(1000 * amplitude) + 1, 5] * M_PI_180,
								M_PI_2,
								SwitchAngles._6OldAlpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase
							);
						}
						break;
					}

				case PulseModeNames.CHMP_Wide_13:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._6WideAlpha[(int)(1000 * amplitude) - 999, 0] * M_PI_180,
						   SwitchAngles._6WideAlpha[(int)(1000 * amplitude) - 999, 1] * M_PI_180,
						   SwitchAngles._6WideAlpha[(int)(1000 * amplitude) - 999, 2] * M_PI_180,
						   SwitchAngles._6WideAlpha[(int)(1000 * amplitude) - 999, 3] * M_PI_180,
						   SwitchAngles._6WideAlpha[(int)(1000 * amplitude) - 999, 4] * M_PI_180,
						   SwitchAngles._6WideAlpha[(int)(1000 * amplitude) - 999, 5] * M_PI_180,
						   M_PI_2,
						   'A', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_11:
                    {
						if (pulse_mode.Alt_Mode == PulseAlternativeMode.Default)
						{
							return GetPulseWithSwitchAngle(
								SwitchAngles._5Alpha[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
								SwitchAngles._5Alpha[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
								SwitchAngles._5Alpha[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
								SwitchAngles._5Alpha[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
								SwitchAngles._5Alpha[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
								M_PI_2,
								M_PI_2,
								SwitchAngles._5Alpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase
							);
						}
						else if (pulse_mode.Alt_Mode == PulseAlternativeMode.Alt1)
						{
							return GetPulseWithSwitchAngle(
								SwitchAngles._5Alpha_Old[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
								SwitchAngles._5Alpha_Old[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
								SwitchAngles._5Alpha_Old[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
								SwitchAngles._5Alpha_Old[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
								SwitchAngles._5Alpha_Old[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
								M_PI_2,
								M_PI_2,
								SwitchAngles._5OldAlpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase
							);
						}
						break;
					}

				case PulseModeNames.CHMP_Wide_11:
                    {
						return GetPulseWithSwitchAngle(
							SwitchAngles._5WideAlpha[(int)(1000 * amplitude) - 999, 0] * M_PI_180,
							SwitchAngles._5WideAlpha[(int)(1000 * amplitude) - 999, 1] * M_PI_180,
							SwitchAngles._5WideAlpha[(int)(1000 * amplitude) - 999, 2] * M_PI_180,
							SwitchAngles._5WideAlpha[(int)(1000 * amplitude) - 999, 3] * M_PI_180,
							SwitchAngles._5WideAlpha[(int)(1000 * amplitude) - 999, 4] * M_PI_180,
							M_PI_2,
							M_PI_2,
							'B', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_9:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._4Alpha[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
						   SwitchAngles._4Alpha[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
						   SwitchAngles._4Alpha[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
						   SwitchAngles._4Alpha[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   SwitchAngles._4Alpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_Wide_9:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._4WideAlpha[(int)(1000 * amplitude) - 799, 0] * M_PI_180,
						   SwitchAngles._4WideAlpha[(int)(1000 * amplitude) - 799, 1] * M_PI_180,
						   SwitchAngles._4WideAlpha[(int)(1000 * amplitude) - 799, 2] * M_PI_180,
						   SwitchAngles._4WideAlpha[(int)(1000 * amplitude) - 799, 3] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   'A', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_7:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._3Alpha[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
						   SwitchAngles._3Alpha[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
						   SwitchAngles._3Alpha[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   SwitchAngles._3Alpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_Wide_7:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._3WideAlpha[(int)(1000 * amplitude) - 799, 0] * M_PI_180,
						   SwitchAngles._3WideAlpha[(int)(1000 * amplitude) - 799, 1] * M_PI_180,
						   SwitchAngles._3WideAlpha[(int)(1000 * amplitude) - 799, 2] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   'B', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_5:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._2Alpha[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
						   SwitchAngles._2Alpha[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   SwitchAngles._2Alpha_Polary[(int)(1000 * amplitude) + 1], sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_Wide_5:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._2WideAlpha[(int)(1000 * amplitude) - 799, 0] * M_PI_180,
						   SwitchAngles._2WideAlpha[(int)(1000 * amplitude) - 799, 1] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   'A', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.CHMP_Wide_3:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._WideAlpha[(int)(500 * amplitude) + 1] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   'B', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.SHEP_3:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._1Alpha_SHE[(int)(1000 * amplitude) + 1] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   'B', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.SHEP_5:
                    {
						return GetPulseWithSwitchAngle(
						   SwitchAngles._2Alpha_SHE[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
						   SwitchAngles._2Alpha_SHE[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   M_PI_2,
						   'A', sin_time, sin_angle_freq, initial_phase);
					}
				case PulseModeNames.SHEP_7:
                    {
						return GetPulseWithSwitchAngle(
							  SwitchAngles._3Alpha_SHE[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
							  SwitchAngles._3Alpha_SHE[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
							  SwitchAngles._3Alpha_SHE[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
							  M_PI_2,
							  M_PI_2,
							  M_PI_2,
							  M_PI_2,
							  'B', sin_time, sin_angle_freq, initial_phase);

					}
				case PulseModeNames.SHEP_11:
                    {
						return GetPulseWithSwitchAngle(
							  SwitchAngles._5Alpha_SHE[(int)(1000 * amplitude) + 1, 0] * M_PI_180,
							  SwitchAngles._5Alpha_SHE[(int)(1000 * amplitude) + 1, 1] * M_PI_180,
							  SwitchAngles._5Alpha_SHE[(int)(1000 * amplitude) + 1, 2] * M_PI_180,
							  SwitchAngles._5Alpha_SHE[(int)(1000 * amplitude) + 1, 3] * M_PI_180,
							  SwitchAngles._5Alpha_SHE[(int)(1000 * amplitude) + 1, 4] * M_PI_180,
							  M_PI_2,
							  M_PI_2,
							  'A', sin_time, sin_angle_freq, initial_phase);
					}
				default: break;
			}


			if (
				pulse_name == PulseModeNames.CHMP_3 ||
				pulse_mode.Square && Is_Square_Available(pulse_mode, 2)
			)
			{
				bool is_shift = pulse_mode.Shift;
				int pulse_num = Get_Pulse_Num(pulse_mode,2);
				double pulse_initial_phase = Get_Pulse_Initial(pulse_mode,2);
				return GetPulseWithSaw(sin_angle_freq * sin_time + initial_phase, pulse_initial_phase, amplitude, pulse_num, is_shift);
			}


			//sync mode but no the above.
			{
				int pulse_num = Get_Pulse_Num(pulse_mode,2);
				double x = sin_angle_freq * sin_time + initial_phase;
				double saw_value = GetSaw(pulse_num * x);
				double sin_value = GetSineValueWithHarmonic(pulse_mode.Clone(), x, amplitude);

				if (pulse_mode.Shift)
					saw_value = -saw_value;

				int pwm_value = ModulateSin(sin_value, saw_value) * 2;

				control.set_Saw_Angle_Freq(sin_angle_freq * pulse_num);
				control.set_Saw_Time(sin_time);
				//Console.WriteLine(pwm_value);
				return pwm_value;
			}

			


		}
	}

	public class SVM
    {
		private double SQRT3 = Math.Sqrt(3);
		private double SQRT3_2 = Math.Sqrt(3)/2.0;
		//private double __2PI_3 = Math.PI * 2 / 3.0;
		public class Function_time
		{
			public double T0;
			public double T1;
			public double T2;
		};

		public class Vabc
		{
			public double Ua;
			public double Ub;
			public double Uc;

			public static Vabc operator +(Vabc a, double d) => new Vabc()
			{
				Ua = a.Ua + d,
				Ub = a.Ub + d,
				Uc = a.Uc + d
			};

			public static Vabc operator *(Vabc a, double d) => new Vabc()
            {
				Ua = a.Ua * d,
				Ub = a.Ub * d,
				Uc = a.Uc * d
			};

			public static Vabc operator -(Vabc a) => new Vabc()
            {
				Ua = -a.Ua,
				Ub = -a.Ub,
				Uc = -a.Uc,
			};

			public static Vabc operator -(Vabc a, Vabc b) => new Vabc()
			{
				Ua = a.Ua - b.Ua,
				Ub = a.Ub - b.Ub,
				Uc = a.Uc - b.Uc
			};
		};

		public class Valbe
		{
			public double Ualpha;
			public double Ubeta;
		};
		public int Sector_estimate(Valbe U)
		{
			int A = U.Ubeta > 0.0 ? 0 : 1;
			int B = U.Ubeta - SQRT3 * U.Ualpha > 0.0 ? 0 : 1;
			int C = U.Ubeta + SQRT3 * U.Ualpha > 0.0 ? 0 : 1;
			switch (4 * A + 2 * B + C)
			{
				case 0: return 2;
				case 1: return 3;
				case 2: return 1;
				case 3: return 0;
				case 4: return 0;
				case 5: return 4;
				case 6: return 6;
				case 7: return 5;
				default: return 2;
			}
		}
		Function_time getfunctiontime(Valbe Vin, int sector)
		{
			Function_time ft = new();
			switch (sector)
			{
				case 1:
					{
						ft.T1 = SQRT3_2 * Vin.Ualpha - 0.5 * Vin.Ubeta;
						ft.T2 = Vin.Ubeta;
					}
					break;
				case 2:
					{
						ft.T1 = SQRT3_2 * Vin.Ualpha + 0.5 * Vin.Ubeta;
						ft.T2 = 0.5 * Vin.Ubeta - SQRT3_2 * Vin.Ualpha;
					}
					break;
				case 3:
					{
						ft.T1 = Vin.Ubeta;
						ft.T2 = -(SQRT3_2 * Vin.Ualpha + 0.5 * Vin.Ubeta);
					}
					break;
				case 4:
					{
						ft.T1 = 0.5 * Vin.Ubeta - SQRT3_2 * Vin.Ualpha;
						ft.T2 = -Vin.Ubeta;
					}
					break;
				case 5:
					{
						ft.T1 = -(SQRT3_2 * Vin.Ualpha + 0.5 * Vin.Ubeta);
						ft.T2 = SQRT3_2 * Vin.Ualpha - 0.5 * Vin.Ubeta;
					}
					break;
				case 6:
					{
						ft.T1 = -Vin.Ubeta;
						ft.T2 = SQRT3_2 * Vin.Ualpha + 0.5 * Vin.Ubeta;
					}
					break;
			}
			ft.T0 = 1.0 - ft.T1 - ft.T2;
			return ft;
		}
		Vabc get_abcvolteage(Function_time Tin, int sector)
		{
			Vabc v_out = new();
			switch (sector)
			{
				case 1:
					{
						v_out.Ua = 0.5 * Tin.T0;
						v_out.Ub = Tin.T1 + 0.5 * Tin.T0;
						v_out.Uc = Tin.T1 + Tin.T2 + 0.5 * Tin.T0;
					}
					break;
				case 2:
					{
						v_out.Ua = Tin.T2 + 0.5 * Tin.T0;
						v_out.Ub = 0.5 * Tin.T0;
						v_out.Uc = Tin.T1 + Tin.T2 + 0.5 * Tin.T0;
					}
					break;
				case 3:
					{
						v_out.Ua = Tin.T1 + Tin.T2 + 0.5 * Tin.T0;
						v_out.Ub = 0.5 * Tin.T0;
						v_out.Uc = Tin.T1 + 0.5 * Tin.T0;
					}
					break;
				case 4:
					{
						v_out.Ua = Tin.T1 + Tin.T2 + 0.5 * Tin.T0;
						v_out.Ub = Tin.T2 + 0.5 * Tin.T0;
						v_out.Uc = 0.5 * Tin.T0;
					}
					break;
				case 5:
					{
						v_out.Ua = Tin.T1 + 0.5 * Tin.T0;
						v_out.Ub = Tin.T1 + Tin.T2 + 0.5 * Tin.T0;
						v_out.Uc = 0.5 * Tin.T0;
					}
					break;
				case 6:
					{
						v_out.Ua = 0.5 * Tin.T0;
						v_out.Ub = Tin.T1 + Tin.T2 + 0.5 * Tin.T0;
						v_out.Uc = Tin.T2 + 0.5 * Tin.T0;
					}
					break;
			}
			return v_out;
		}

		Valbe Clark(Vabc V0)
		{
			Valbe Vin = new();
			Vin.Ualpha = 0.66666666666666666666666666666667 * (V0.Ua - 0.5 * V0.Ub - 0.5 * V0.Uc);
			Vin.Ubeta = 0.66666666666666666666666666666667 * (SQRT3_2 * (V0.Ub - V0.Uc));
			return Vin;
		}
		
	}
}