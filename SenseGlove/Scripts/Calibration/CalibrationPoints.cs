using SGCore.Kinematics;
using System.Collections.Generic;
using UnityEngine;

namespace SGCore.Calibration
{

	public class CalibrationPoints
	{
		public List<Vect3D[]> dataPoints = new List<Vect3D[]>();

		private readonly int maxDataPoints = 6000; //6000 samples = 60 seconds of points.

		protected Vect3D[] minValues = null;
		protected Vect3D[] maxValues = null;


		public Vect3D[] MinimumVals
        {
			get 
			{ 
				if (minValues == null) { return new Vect3D[5] { Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero }; }
				return minValues;
			}
		}

		public Vect3D[] MaximumVals
        {
			get
			{
				if (maxValues == null) { return new Vect3D[5] { Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero, Vect3D.zero }; }
				return maxValues;
			}
		}

		public Vect3D[] SensorRange
        {
			get
			{
				Vect3D[] min = MinimumVals;
				Vect3D[] max = MaximumVals;
				Vect3D[] res = new Vect3D[min.Length];
				for (int f=0; f<min.Length; f++)
                {
					res[f] = max[f] - min[f];
                }
				return res;
			}
        }

		public SensorRange GetAsRange()
        {
			return new SensorRange(MinimumVals, MaximumVals);
        }


		protected static Vect3D[] Copy(Vect3D[] values)
        {
			Vect3D[] copy = new Vect3D[values.Length];
			for (int f = 0; f < values.Length; f++) { copy[f] = values[f]; }
			return copy;
		}

		public static void CheckMax(Vect3D currVals, ref Vect3D maxVals)
        {
			if (currVals.x > maxVals.x) { maxVals.x = currVals.x; }
			if (currVals.y > maxVals.y) { maxVals.y = currVals.y; }
			if (currVals.z > maxVals.z) { maxVals.z = currVals.z; }
        }

		public static void CheckMin(Vect3D currVals, ref Vect3D minVals)
		{
			if (currVals.x < minVals.x) { minVals.x = currVals.x; }
			if (currVals.y < minVals.y) { minVals.y = currVals.y; }
			if (currVals.z < minVals.z) { minVals.z = currVals.z; }
		}


		public void AddData(Vect3D[] calibrationValues)
		{
			if (dataPoints.Count == maxDataPoints)
			{
				dataPoints.RemoveAt(0);
			}
			Vect3D[] cop = Copy(calibrationValues);
			dataPoints.Add(cop);

			if (maxValues == null && minValues == null)
			{
				maxValues = new Vect3D[cop.Length];
				minValues = new Vect3D[cop.Length];
				for (int i = 0; i < cop.Length; i++)
				{
					maxValues[i] = new Vect3D(cop[i]);
					minValues[i] = new Vect3D(cop[i]);
				}
			}
			else
			{
				for (int f = 0; f < calibrationValues.Length; f++)
				{
					CheckMax(cop[f], ref maxValues[f]);
					CheckMin(cop[f], ref minValues[f]);
				}
			}
		}


		public void ResetPoints()
		{
			dataPoints.Clear();
			maxValues = null;
			minValues = null;
		}


		void TestUnit()
        {
			Vect3D[] mins = (new Vect3D[]
			{
				new Vect3D(0, 1, 0),
				new Vect3D(0, 2, 0),
				new Vect3D(0, 3, 0),
				new Vect3D(0, 4, 0),
				new Vect3D(0, 5, 0)
			});

			Vect3D[] maxs = (new Vect3D[]
			{
				new Vect3D(0, 5, 0),
				new Vect3D(0, 4, 0),
				new Vect3D(0, 3, 0),
				new Vect3D(0, 2, 0),
				new Vect3D(0, 1, 0)
			});


			this.AddData(mins);
			this.AddData(maxs);

			Vect3D[] myMax = MaximumVals;
			Vect3D[] myMin = MinimumVals;

			string min = "";
			for (int i = 0; i < myMin.Length; i++)
			{
				min += myMin[i].ToString() + "\r\n";
			}
			Debug.Log(min);

			string max = "";
			for (int i = 0; i < myMax.Length; i++)
			{
				max += myMax[i].ToString() + "\r\n";
			}
			Debug.Log(max);
		}




		void Start()
        {
			
		}



	}

}