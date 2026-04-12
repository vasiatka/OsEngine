using OsEngine.Entity;
using OsEngine.Indicators;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OsEngine.RoboMarketer.Indicators
{
    [Indicator("AOChannel")]
    public class AOChannel : Aindicator
    {
        private IndicatorParameterInt _lengthFastLine;
        private IndicatorParameterInt _lengthSlowLine;
        public IndicatorParameterString _candlePoint;
        private Aindicator _AO;

        //private IndicatorParameterDecimal _multDev;

        private IndicatorDataSeries _seriesUp;
        private IndicatorDataSeries _seriesDown;
        
        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _lengthFastLine = CreateParameterInt("Fast line length", 5);
                _lengthSlowLine = CreateParameterInt("Slow line length", 32);
                _candlePoint = CreateParameterStringCollection("Candle point", "Typical", OsEngine.Indicators.Entity.CandlePointsArray);

                
                _seriesUp = CreateSeries("Resistance", Color.Red, IndicatorChartPaintType.Line, true);
                _seriesDown = CreateSeries("Support", Color.Green, IndicatorChartPaintType.Line, true);

                _AO = IndicatorsFactory.CreateIndicatorByName("AO", Name + "AO", false);
                _AO.Parameters[0].Bind(_lengthFastLine);
                _AO.Parameters[1].Bind(_lengthSlowLine);
                _AO.Parameters[2].Bind(_candlePoint);
                ProcessIndicator("AO", _AO);


                _seriesUp.CanReBuildHistoricalValues = true;
                _seriesDown.CanReBuildHistoricalValues = true;
            }
            else if (state == IndicatorState.Dispose)
            {
                _seriesUp = null;
                _seriesDown = null;
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (index - _lengthSlowLine.ValueInt <= 0 ||
                index - _lengthFastLine.ValueInt <= 0)
            {
                _seriesUp.Values[index] = 0;
                _seriesDown.Values[index] = 0;
                return;
            }

            _seriesUp.Values[index] = _seriesUp.Values[index - 1];
            _seriesDown.Values[index] = _seriesDown.Values[index - 1];

            decimal AO3 = _AO.DataSeries[0].Values[index];
            decimal AO2 = _AO.DataSeries[0].Values[index-1];
            decimal AO1 = _AO.DataSeries[0].Values[index-2];


            if (AO1>0 && AO2>0 && AO3>0 && AO1<AO2 && AO2>AO3)
            {
                decimal r = candles[index].GetPoint(_candlePoint.ValueString);
                for (int i = index - 1; (i > 0) && (_AO.DataSeries[0].Values[i]>0); i--)
                { 
                   if(candles[i].GetPoint(_candlePoint.ValueString) > r) { r = candles[i].GetPoint(_candlePoint.ValueString); }
                }
                _seriesUp.Values[index] = r;
            }

            if (AO1 < 0 && AO2 < 0 && AO3 < 0 && AO1 > AO2 && AO2 < AO3)
            {
                decimal s = candles[index].GetPoint(_candlePoint.ValueString);
                for (int i = index - 1; (i > 0) && (_AO.DataSeries[0].Values[i] < 0); i--)
                {
                    if (candles[i].GetPoint(_candlePoint.ValueString) < s ) { s = candles[i].GetPoint(_candlePoint.ValueString); }
                }
                _seriesDown.Values[index] = s;
            }

        }
    }
}