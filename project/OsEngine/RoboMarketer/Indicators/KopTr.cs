using OsEngine.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OsEngine.Entity;

namespace OsEngine.RoboMarketer.Indicators
{
    [Indicator("KopTr")]
    public class KopTr : Aindicator
    {
        private IndicatorParameterString _valType;
        private IndicatorParameterInt _period;
        private IndicatorParameterDecimal _multiplier;

        //sar
        private IndicatorParameterInt _sarPeriod;
        private IndicatorParameterInt _sarPeriod2;
        private IndicatorParameterInt _sarDeviation;

        
        private IndicatorDataSeries _TrBuy;
        private IndicatorDataSeries _TrSell;
        private IndicatorDataSeries _SarBuy;
        private IndicatorDataSeries _SarSell;
        private IndicatorDataSeries _sar;
        private IndicatorDataSeries _ema;

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _valType = CreateParameterStringCollection("По каким значениям считаем:", "ATR", new List<string> { "ATR", "Open", "Close", "High", "Low" });
                _period = CreateParameterInt("Период", 84);

                _multiplier = CreateParameterDecimal("Множитель", 1.2m);

                _sarPeriod = CreateParameterInt("SarPeriod", 32);
                _sarPeriod2 = CreateParameterInt("SarPeriod2", 256);
                _sarDeviation = CreateParameterInt("SarDeviation", 3);

                _TrBuy = CreateSeries("TrBuy", Color.BlueViolet, IndicatorChartPaintType.Line, true);
                _TrBuy.CanReBuildHistoricalValues = false;

                _TrSell = CreateSeries("TrSell", Color.OrangeRed, IndicatorChartPaintType.Line, true);
                _TrSell.CanReBuildHistoricalValues = false;

                _SarBuy = CreateSeries("SarBuy", Color.DarkRed, IndicatorChartPaintType.Line, true);
                _SarBuy.CanReBuildHistoricalValues = false;

                _SarSell = CreateSeries("SarSell", Color.LimeGreen, IndicatorChartPaintType.Line, true);
                _SarSell.CanReBuildHistoricalValues = false;

                _sar = CreateSeries("Sar", Color.Coral, IndicatorChartPaintType.Line, true);
                _sar.CanReBuildHistoricalValues = false;

                _ema = CreateSeries("ema", Color.Yellow, IndicatorChartPaintType.Line, true);
                _ema.CanReBuildHistoricalValues = false;

            }
        }

        public override void OnProcess(List<Candle> candles, int index)
        {


        }
     }
}


