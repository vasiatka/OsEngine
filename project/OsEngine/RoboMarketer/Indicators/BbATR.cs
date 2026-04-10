using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace OsEngine.RoboMarketer.Common.Indicators
{
    [Indicator("BbATR")]
    public class BbATR : Aindicator
    {
        private IndicatorParameterInt _length;
        private IndicatorParameterInt _lengthFast;
        private IndicatorParameterDecimal _multDev;

        private IndicatorParameterString _typeSeries;

        private IndicatorDataSeries _series;

        private IndicatorDataSeries _seriesUp;
        //private IndicatorDataSeries _seriesDown;
        private IndicatorDataSeries _seriesFast;


        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _length = CreateParameterInt("Length", 14);
                _lengthFast = CreateParameterInt("LengthFast", 7);
                _multDev = CreateParameterDecimal("DevMult", 1.5m);

                _typeSeries = CreateParameterStringCollection("Series type",
                    "Absolute",
                    new List<string>() { "Absolute", "Percent" });

                _series = CreateSeries("Atr value", Color.DodgerBlue, IndicatorChartPaintType.Line, true);
                _seriesUp = CreateSeries("Atr value BB Up", Color.Green, IndicatorChartPaintType.Line, true);
                //_seriesDown = CreateSeries("Atr value BB Down", Color.Green, IndicatorChartPaintType.Line, true);
                _seriesFast = CreateSeries("Atr value Fast", Color.Red, IndicatorChartPaintType.Line, true);
            }
            else if (state == IndicatorState.Dispose)
            {
                if (_moving != null)
                {
                    _moving.Clear();
                }
                if (_trueRange != null)
                {
                    _trueRange.Clear();
                }
            }
        }

        private decimal StandardDeviation(List<decimal> valuesSeries, List<decimal> moving, int index)
        {
            if (_length.ValueInt > valuesSeries.Count)
            {
                return 0;
            }

            double summ = 0;
            
            for (int i = index - _length.ValueInt + 1, i2 = 0; i < index + 1; i++, i2++)
            {
                summ += Math.Pow(Convert.ToDouble(valuesSeries[i] - moving[i]),2);
            }

            if (_length.ValueInt > 30)
            {
                summ = summ / (_length.ValueInt - 1);
            }
            else
            {
                summ = summ / _length.ValueInt;
            }

            summ = Math.Sqrt(summ);

            return Convert.ToDecimal(summ);
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            TrueRangeReload(candles, index);
            _moving = MovingAverageWild(_trueRange, _moving, _length.ValueInt, index);
            _movingFast = MovingAverageWild(_trueRange, _movingFast, _lengthFast.ValueInt, index);
            decimal s = StandardDeviation(_trueRange, _moving, index);

            _series.Values[index] = Math.Round(_moving[index], 9);
            _seriesFast.Values[index] = Math.Round(_movingFast[index], 9);
            _seriesUp.Values[index] = Math.Round(_series.Values[index] + _multDev.ValueDecimal * s,9);
            //_seriesDown.Values[index] = Math.Round(_series.Values[index] - _multDev.ValueDecimal * s, 9);
        }

        private List<decimal> _moving = new List<decimal>();
        private List<decimal> _movingFast = new List<decimal>();

        private List<decimal> _trueRange = new List<decimal>();

        private void TrueRangeReload(List<Candle> candles, int index)
        {
            if (index == 0)
            {
                _trueRange = new List<decimal>();
                _trueRange.Add(0);
                return;
            }

            while (_trueRange.Count - 1 < index)
            {
                _trueRange.Add(0);
            }

            decimal hiToLow = Math.Abs(candles[index].High - candles[index].Low);
            decimal closeToHigh = Math.Abs(candles[index - 1].Close - candles[index].High);
            decimal closeToLow = Math.Abs(candles[index - 1].Close - candles[index].Low);

            decimal value = Math.Max(Math.Max(hiToLow, closeToHigh), closeToLow);

            if (_typeSeries.ValueString == "Percent"
                && value != 0
                && candles[index - 1].Open != 0)
            {
                value = value / (candles[index - 1].Open / 100);
            }

            _trueRange[index] = value;
        }

        private List<decimal> MovingAverageWild(List<decimal> valuesSeries, List<decimal> moving, int length, int index)
        {
            if (moving == null || length > valuesSeries.Count)
            {
                moving = new List<decimal>();
                for (int i = 0; i < index + 1; i++)
                {
                    moving.Add(0);
                }
            }
            else if (length == valuesSeries.Count)
            {
                decimal lastMoving = 0;

                for (int i = index; i > -1 && i > valuesSeries.Count - 1 - length; i--)
                {
                    lastMoving += valuesSeries[i];
                }

                if (lastMoving != 0)
                {
                    moving.Add(lastMoving / length);
                }
                else
                {
                    moving.Add(0);
                }
            }
            else
            {
                while (moving.Count < index)
                {
                    moving.Add(moving[moving.Count - 1]);
                }

                decimal lastValueSeries = Math.Round(valuesSeries[index], 9);
                decimal lastValueMoving = moving[index - 1];

                if (index > moving.Count - 1)
                {
                    moving.Add(0);
                }
                moving[index] = Math.Round((lastValueMoving * (length - 1) + lastValueSeries) / length, 9);
            }

            return moving;
        }
    }
}