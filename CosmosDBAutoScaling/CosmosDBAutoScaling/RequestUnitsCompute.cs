using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosDBAutoScaling
{
    public interface IRequestUnitsCompute
    {
        double MinimumRU { get; set; }
        double MaximumRU { get; set; }
        int Steps { get; set; }

        int ScaleUp(int RU);
        int ScaleDown(int RU);
    }
    public class RequestUnitsScaleFactor : IRequestUnitsCompute
    {
        public double MinimumRU { get; set; }
        public double MaximumRU { get; set; }
        public int Steps { get; set; }

        private double ScaleFactor { get { return Math.Pow((this.MaximumRU / this.MinimumRU), (1.0 / this.Steps)); } }
        public static RequestUnitsScaleFactor CreateRequestUnitsCompute<T>(T mimimumRU, T maximumRU, int steps)
        {
            return new RequestUnitsScaleFactor()
            {
                MinimumRU = Convert.ToDouble(mimimumRU),
                MaximumRU = Convert.ToDouble(maximumRU),
                Steps = steps
            };
        }

        public int ScaleUp(int ru)
        {
            double vulgarRu = Convert.ToDouble(ru) * this.ScaleFactor;
            return Convert.ToInt32(Math.Round(vulgarRu, MidpointRounding.AwayFromZero));
        }

        public int ScaleDown(int ru)
        {
            double vulgarRu = Convert.ToDouble(ru) / this.ScaleFactor;
            return Convert.ToInt32(Math.Round(vulgarRu, MidpointRounding.AwayFromZero));
        }
    }
}
