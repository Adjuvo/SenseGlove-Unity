using System.Collections;
using System.Collections.Generic;
using SGCore.Kinematics;
using UnityEngine;

namespace SGCore.Calibration
{
	/// <summary> A calibrationRange that contains minimum / maximum calibration values of a HapticGlove. </summary>
	public class SensorRange
	{

		/// <summary> The minimum sensor values of this sensor range </summary>
		private Vect3D[] minVals = new Vect3D[5] { Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero };
		/// <summary> The maximum sensor values of this sensor range </summary>
		private Vect3D[] maxVals = new Vect3D[5] { Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero };
		


		/// <summary> Access the lowest values values of this range. </summary>
		public Vect3D[] MinValues
        {
			get { return minVals; }
			set
            {
				for (int f=0; f<minVals.Length && f < value.Length; f++)
                {
					minVals[f] = new Vect3D(value[f]);
                }
				UpdateRange();
			}
		}

		/// <summary> Access the highest values values of this range. </summary>
		public Vect3D[] MaxValues
        {
			get { return maxVals; }
			set
            {
				for (int f = 0; f < minVals.Length && f < value.Length; f++)
				{
					maxVals[f] = new Vect3D(value[f]);
				}
				UpdateRange();
			}
        }


		public Vect3D[] Range
        {
			get; private set;
        }



		public SensorRange()
		{
			UpdateRange();
		}


		public SensorRange(Vect3D[] min, Vect3D[] max)
        {
			minVals = min;
			maxVals = max;
			UpdateRange();
        }

		/// <summary> Deep copy another calibration range. </summary>
		/// <param name="copy"></param>
		public SensorRange(SensorRange copy)
        {
			this.minVals = new Vect3D[copy.minVals.Length];
			for (int i = 0; i < this.minVals.Length; i++) { this.minVals[i] = new Vect3D(copy.minVals[i]); }
			this.maxVals = new Vect3D[copy.maxVals.Length];
			for (int i = 0; i < this.maxVals.Length; i++) { this.maxVals[i] = new Vect3D(copy.maxVals[i]); }
			UpdateRange();
		}


		public void UpdateRange()
        {
			Vect3D[] newRange = new Vect3D[Mathf.Min(minVals.Length, maxVals.Length)];
			for (int f=0; f< newRange.Length; f++)
            {
				newRange[f] = maxVals[f] - minVals[f];
            }
			Range = newRange;
        }


		public string Serialize()
        {
			string ser = "";
			ser += Util.Serializer.Serialize(this.MinValues);
			ser += Util.Serializer.Serialize(this.MaxValues);
			return ser;
		}

		public string ToString(bool yOnly = true)
        {
			string res = "";
			if (yOnly)
			{
				for (int f = 0; f < this.MinValues.Length; f++)
				{
					res += "[" + this.MinValues[f].y + " .. " + this.MaxValues[f].y + "]";
					if (res.Length > MinValues.Length - 1) { res += "\r\n"; }
				}
				return res;
			}
			return "NotYetImplemented";
        }

		public string RangeString(bool yOnly = true)
        {
			string res = "";
			if (yOnly)
			{
				for (int f = 0; f < this.Range.Length; f++)
				{
					res += this.Range[f].y;
					if (f < Range.Length - 1) { res += ", "; }
				}
				return res;
			}
			return res;
		}


		public static SensorRange Deserialize(string serializedString)
		{
			try
			{
				string[] blocks = Util.Serializer.SplitBlocks(serializedString);
				Vect3D[] mins = SGCore.Util.Serializer.DeserializeVects(blocks[0]);
				Vect3D[] maxs = SGCore.Util.Serializer.DeserializeVects(blocks[1]);
				return new SensorRange(mins, maxs);
			}
			catch (System.Exception ex)
			{
				Diagnostics.Debugger.Log(ex.Message, Diagnostics.DebugLevel.ErrorsOnly);
			}
			return new SensorRange();
		}



		/// <summary> Generates a Sensor Range where minumum values are float.MaxValue and max values are float.MinValue. </summary>
		/// <returns></returns>
		public static SensorRange ForCalibration()
        {
			Vect3D min = new Vect3D(float.MinValue, float.MinValue, float.MinValue);
			Vect3D max = new Vect3D(float.MaxValue, float.MaxValue, float.MaxValue);
			return new SensorRange(
				new Vect3D[5] { new Vect3D(max), new Vect3D(max), new Vect3D(max), new Vect3D(max), new Vect3D(max) },
				new Vect3D[5] { new Vect3D(min), new Vect3D(min), new Vect3D(min), new Vect3D(min), new Vect3D(min) }
			);
        }

		/// <summary> In otherValues are greater/small than the current range, update to them </summary>
		/// <param name="otherValues"></param>
		public void CheckForExtremes(Vect3D[] otherValues)
        {
			for (int f = 0; f < this.minVals.Length && f < otherValues.Length; f++)
			{
				CalibrationPoints.CheckMin(otherValues[f], ref this.minVals[f]);
				CalibrationPoints.CheckMax(otherValues[f], ref this.maxVals[f]);
			}
			this.UpdateRange();
        }

    }
}