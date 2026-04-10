using OsEngine.Entity;
using OsEngine.Indicators;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OsEngine.RoboMarketer.Indicators
{
    [Indicator("BbVolume")]
    public class BbVolume : Aindicator
    {
        private IndicatorParameterInt _length;
        private IndicatorParameterDecimal _multDev;

        private IndicatorDataSeries _series;
        private IndicatorDataSeries _seriesUp;
        private IndicatorDataSeries _seriesMean;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _length = CreateParameterInt("Length", 20);
                _multDev = CreateParameterDecimal("DevMult", 1.5m);

                _series = CreateSeries("Volume", Color.Red, IndicatorChartPaintType.Column, true);
                _seriesUp = CreateSeries("VolumeUp", Color.Green, IndicatorChartPaintType.Line, true);
                _seriesMean = CreateSeries("VolumeMean", Color.DodgerBlue, IndicatorChartPaintType.Line, true);

                _series.CanReBuildHistoricalValues = true;
            }
            else if (state == IndicatorState.Dispose)
            {
                _series = null;
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            _series.Values[index] = candles[index].Volume;

            if (candles.Count>= _length.ValueInt && index> _length.ValueInt)
            {
                decimal m = 0;
                double s = 0;
                for (int i = index; i > index - _length.ValueInt; i--)
                {
                    m += _series.Values[i];
                }
                m = Math.Round(m / _length.ValueInt,9);
                for (int i = index; i > index - _length.ValueInt; i--)
                {
                    s+= Math.Pow(Convert.ToDouble(_series.Values[i] - m), 2);
                }
                s = Math.Sqrt(s / _length.ValueInt);
                _seriesUp.Values[index] = m+Convert.ToDecimal(s)*_multDev.ValueDecimal;
                _seriesMean.Values[index] = m;

            }
            else
            {
                _seriesUp.Values[index] = 0;
                _seriesMean.Values[index] = 0;

            }
        }
    }
}