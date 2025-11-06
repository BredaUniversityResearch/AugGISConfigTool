using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AugGISDataParser
{
	[System.Serializable]
	public struct Vector2
	{
		public double x;
		public double y;

		public Vector2()
		{
			x = 0;
			y = 0;
		}

		public Vector2(double a_x, double a_y)
		{
			x = a_x;
			y = a_y;
		}

		public double[] ToArray()
		{
			return new double[2] { x, y };
		}

		public override string ToString()
		{
			return string.Format("x: %d y:%d", x, y);
		}
	}
}