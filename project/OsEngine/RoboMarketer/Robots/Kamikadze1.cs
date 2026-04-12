using OsEngine.Candles.Series;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Connectors;
using OsEngine.Market.Servers;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.RoboMarketer.Lib;
using OsEngine.RoboMarketer.Lib.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OsEngine.RoboMarketer.Robots
{
    [Bot("Kamikadze1")]
    public class Kamikadze1 : RoboMarketerPanel
    {
        
        public Kamikadze1(string name, StartProgram startProgram) : base(name, startProgram)
        {
            //Инициализируем общие параметры роботов
            _regime = new TradingRegimeHelper(this).UseRegimeAll().UseTradeTime();
            _volume = new VolumeHelper(this);
            _seller = new EnterPositionHelper(this).UseMaxPositions();

            CreateIndicators();

        }

        #region Logic
        private void TryEntryLogic(List<Candle> candles, BotTabSimple tab)
        {
            if (_regime.IsTradingAllowed(tab) == false) return;

            List<Position> positions = tab.PositionsOpenAll;

            decimal price = candles[^1].Close;
            Aindicator _nrtr = (Aindicator)tab.Indicators[0];

            if (positions.Count == 0)
            {

                if (_tab.PositionsOpenAll.Count >= _seller.MaxPositions.ValueInt)
                {
                    return;
                }

                if (candles[^1].IsUp
                    && price > _nrtr.DataSeries[0].Last
                    && !_regime.IsOnlyShort()
                    )

                {
                    _seller.BuyAtMarket(tab, _volume.GetVolume(tab));
                }


                if (candles[^1].IsDown
                    && price < _nrtr.DataSeries[0].Last
                    && !_regime.IsOnlyLong()
                    )

                {
                    _seller.SellAtMarket(tab, _volume.GetVolume(tab));
                }

            }
            else
            {
                if (positions[0].Direction == Side.Buy && price < _nrtr.DataSeries[0].Last)
                {
                    _seller.CloseAllPositionAtMarket(tab);
                }
                if (positions[0].Direction == Side.Sell && price > _nrtr.DataSeries[0].Last)
                {
                    _seller.CloseAllPositionAtMarket(tab);
                }

            }

        }

        #endregion

        #region UsedIndicators
        //Параметры индикаторов
        private StrategyParameterInt _periodNRTR;
        private StrategyParameterInt _multNRTR;
        private void CreateIndicators()//создаем индикаторы используем в конструкторе
        {
            //Example
            _periodNRTR = CreateParameter("NRTRPeriod", 140, 10, 20, 5);
            _multNRTR = CreateParameter("NRTRMult", 6, 1, 15, 1);
            _tab.CreateCandleIndicator(1, "NRTR_WATR", new List<string>() { _periodNRTR.ValueInt.ToString(), _multNRTR.ValueInt.ToString() }, "Prime");
        }



        private void UpdateIndicators()//создаем индикаторы используем в конструкторе
        {
            //Example
            BotTabSimple tab;
            for (int i =0; i<_tab.Tabs.Count;i++)
            {
                tab = _tab.Tabs[i];
                if (tab!=null)
                {
                    Aindicator _nrtr = (Aindicator)tab.Indicators[0];
            
                    if (_nrtr.ParametersDigit[0].Value != _periodNRTR.ValueInt
                        || _nrtr.ParametersDigit[1].Value != _multNRTR.ValueInt)
                    {
                        _nrtr.ParametersDigit[0].Value = _periodNRTR.ValueInt;
                        _nrtr.ParametersDigit[1].Value = _multNRTR.ValueInt;
                        _nrtr.Save();
                        _nrtr.Reload();
                    }
                }
            }
            
            //_tab.UpdateIndicatorsParameters();
        }



        #endregion




        #region Events

        //события завершения свечей
        protected override void _tab_CandleFinishedEvent(List<Candle> candles,BotTabSimple tab)
        {
            TryEntryLogic(candles, tab);
        }

        protected override void Event_ParametrsChangeByUser()
        {
          UpdateIndicators();
        }

        #endregion
    }
}
