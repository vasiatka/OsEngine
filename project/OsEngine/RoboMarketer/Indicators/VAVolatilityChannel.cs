using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;

namespace OsEngine.Indicators
{
    [Indicator("VAVolatilityChanel")]
    public class VAVolatilityChanel : Aindicator
    {
        private IndicatorParameterString _volatilityLevelsCount;

        private IndicatorParameterString _typeVolatilityPeriod;

        private IndicatorParameterString _typeVolatilityVariable;

        private IndicatorParameterInt _lenSmaSlow;

        private IndicatorParameterInt _lenSmaFast;

        private IndicatorParameterString _channelDeviationType;

        private IndicatorParameterDecimal _channelDeviation;

        private IndicatorDataSeries _seriesVolatilityLevels;

        private IndicatorDataSeries _seriesCandleVolatility;
        private IndicatorDataSeries _seriesVolatility;

        private IndicatorDataSeries _seriesSmaSlow;

        private IndicatorDataSeries _seriesSmaFast;

        private IndicatorDataSeries _seriesUpChannel;

        private IndicatorDataSeries _seriesDownChannel;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _volatilityLevelsCount = CreateParameterStringCollection(
                    "Количество уровней волатильности",
                    "3",
                    new List<string> { "2", "3", "4" });

                _typeVolatilityPeriod = CreateParameterStringCollection(
                    "Период усреднения",
                    "День",
                    new List<string> {"День", "Неделя"});

                _typeVolatilityVariable = CreateParameterStringCollection(
                    "Представление волатильности",
                    "Абсолютные значения",
                    new List<string> { "В процентах", "Абсолютные значения" });

                _lenSmaSlow = CreateParameterInt("Период медленной SMA", 21);
                _lenSmaFast = CreateParameterInt("Период быстрой SMA", 7);


                _channelDeviation = CreateParameterDecimal("Отклонение канала", 0.25m);

                _seriesVolatilityLevels = CreateSeries("Уровни волатильности", Color.Green, IndicatorChartPaintType.Line, true);
                _seriesVolatilityLevels.CanReBuildHistoricalValues = true;

                _seriesUpChannel = CreateSeries("Up channel", Color.WhiteSmoke, IndicatorChartPaintType.Line, true);
                _seriesUpChannel.CanReBuildHistoricalValues = true;

                _seriesDownChannel = CreateSeries("Down channel", Color.WhiteSmoke, IndicatorChartPaintType.Line, true);
                _seriesDownChannel.CanReBuildHistoricalValues = true;

                _seriesCandleVolatility = CreateSeries("CandleVolatility", Color.Aquamarine, IndicatorChartPaintType.Point, false);
                _seriesCandleVolatility.CanReBuildHistoricalValues = true;

                _seriesVolatility = CreateSeries("Volatility", Color.DodgerBlue, IndicatorChartPaintType.Line, false);
                _seriesVolatility.CanReBuildHistoricalValues = true;

                _seriesSmaSlow = CreateSeries("Sma slow", Color.WhiteSmoke, IndicatorChartPaintType.Line, true);
                _seriesSmaSlow.CanReBuildHistoricalValues = true;

                _seriesSmaFast = CreateSeries("Sma fast", Color.DarkRed, IndicatorChartPaintType.Line, true);
                _seriesSmaFast.CanReBuildHistoricalValues = true;
            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {
            if (candles.Count == 0)
            {
                return;
            }

            UpDateVolatilityBase(candles, index);
        }

        private void UpDateVolatilityBase(List<Candle> candles, int index)
        {
            GetVolatilityPeriod(candles, index);
        }

        private decimal GetOneCandleVolatility(Candle candle)
        {
            return (candle.High - candle.Low);
        }

        private decimal GetTimePeriod(DateTime t,int index)
        {
            if (_typeVolatilityPeriod.ValueString == "День")
            {
                return t.Day;
            }
            
            return ISOWeek.GetWeekOfYear(t);
            
        }


        private void GetVolatilityPeriod(List<Candle> candles, int index)
        {
            decimal curPeriod = GetTimePeriod(candles[index].TimeStart, index);

            decimal value = 0;
            value = GetOneCandleVolatility(candles[index]);
            if (_typeVolatilityVariable.ValueString == "В процентах")
            {
                value = value / candles[index].Close * 100;
                value = Math.Round(value, 5);
                
            }
            _seriesCandleVolatility.Values[index] = value;

            decimal m = 0;
            decimal s = 0;
            decimal n = 0;
            //Средняя волатильность в течение дня
            for (int i = index; i >= 0; i--)
            {
                if (GetTimePeriod(candles[i].TimeStart,i) != curPeriod)
                {
                    break;
                }
                n++;
                m += _seriesCandleVolatility.Values[i];

            }
            if (n == 0) { return; }
            m /= n;

            

            for (int i = index; i >= 0; i--)
            {
                if (GetTimePeriod(candles[i].TimeStart, i) != curPeriod)
                {
                    break;
                }
                s += (_seriesCandleVolatility.Values[i]-m)* (_seriesCandleVolatility.Values[i] - m);
            }
            s = (decimal)Math.Sqrt((double)(s / n));
            

            for (int i = index; i >= 0; i--)
            {
                if (GetTimePeriod(candles[i].TimeStart, i) != curPeriod)
                {
                    break;
                }

                _seriesVolatility.Values[i] = s;
            }

            // SMA Fast

            decimal summValue = 0;
            int countChangeVola = 0;
            decimal lastValue = 0;

            for (int i = index; i >= 0; i--)
            {
                if (lastValue != 0 &&
                    _seriesVolatility.Values[i] == lastValue)
                {
                    continue;
                }

                lastValue = _seriesVolatility.Values[i];
                countChangeVola++;
                summValue += lastValue;

                if (countChangeVola >= _lenSmaFast.ValueInt)
                {
                    break;
                }
            }

            decimal valueSma = summValue / countChangeVola;

            for (int i = index; i >= 0; i--)
            {
                if (GetTimePeriod(candles[i].TimeStart, i) != curPeriod)
                {
                    break;
                }

                _seriesSmaFast.Values[i] = Math.Round(valueSma, 5);
            }

            // SMA Slow / Channel

            summValue = 0;
            countChangeVola = 0;
            lastValue = 0;

            for (int i = index; i >= 0; i--)
            {
                if (lastValue != 0 &&
                    _seriesVolatility.Values[i] == lastValue)
                {
                    continue;
                }

                lastValue = _seriesVolatility.Values[i];
                countChangeVola++;
                summValue += lastValue;

                if (countChangeVola >= _lenSmaSlow.ValueInt)
                {
                    break;
                }
            }

            valueSma = summValue / countChangeVola;

            for (int i = index; i >= 0; i--)
            {
                if (GetTimePeriod(candles[i].TimeStart, i) != curPeriod)
                {
                    break;
                }

                if (i >= _seriesSmaSlow.Values.Count
                    || i >= _seriesUpChannel.Values.Count
                    || i >= _seriesDownChannel.Values.Count)
                {
                    continue;
                }

                _seriesSmaSlow.Values[i] = Math.Round(valueSma, 5);
                _seriesUpChannel.Values[i] = Math.Round(valueSma + valueSma * _channelDeviation.ValueDecimal, 5);
                _seriesDownChannel.Values[i] = Math.Round(valueSma - valueSma * _channelDeviation.ValueDecimal, 5);
            }

            // Vola Stage

            decimal volaStage = GetVolatilityLevel(index);

            for (int i = index; i >= 0; i--)
            {
                if (GetTimePeriod(candles[i].TimeStart, i) != curPeriod)
                {
                    break;
                }
                _seriesVolatilityLevels.Values[i] = volaStage;
            }
        }

        private decimal GetVolatilityLevel(int index)
        {
            decimal volaStage = 0;
            decimal smaFast = _seriesSmaFast.Values[index];
            decimal smaSlow = _seriesSmaSlow.Values[index];
            decimal upperChannel = _seriesUpChannel.Values[index];
            decimal downChannel = _seriesDownChannel.Values[index];

            if (_volatilityLevelsCount.ValueString == "2")
            {
                if (smaFast == 0 || smaSlow == 0)
                {
                    return 0;
                }

                if (smaFast >= smaSlow)
                {
                    volaStage = 2;
                }
                else
                {
                    volaStage = 1;
                }
            }
            else if (_volatilityLevelsCount.ValueString == "3")
            {
                if (smaFast == 0
                    || smaSlow == 0
                    || upperChannel == 0
                    || downChannel == 0)
                {
                    return 0;
                }
                if (smaFast >= upperChannel)
                {
                    volaStage = 3;
                }
                else if (smaFast <= upperChannel
                    && smaFast >= downChannel)
                {
                    volaStage = 2;
                }
                else if (smaFast <= downChannel)
                {
                    volaStage = 1;
                }
            }
            else if (_volatilityLevelsCount.ValueString == "4")
            {
                if (smaFast == 0
                   || smaSlow == 0
                   || upperChannel == 0
                   || downChannel == 0)
                {
                    return 0;
                }
                if (smaFast >= upperChannel)
                {
                    volaStage = 4;
                }
                else if (smaFast <= upperChannel
                    && smaFast >= smaSlow)
                {
                    volaStage = 3;
                }
                else if (smaFast <= smaSlow
                    && smaFast >= downChannel)
                {
                    volaStage = 2;
                }
                else if (smaFast <= downChannel)
                {
                    volaStage = 1;
                }
            }

            return volaStage;
        }
    }
}