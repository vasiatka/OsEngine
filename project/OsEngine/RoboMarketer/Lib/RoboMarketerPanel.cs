using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using OsEngine.RoboMarketer.Lib.Helpers;

namespace OsEngine.RoboMarketer.Lib
{
    public abstract class RoboMarketerPanel : BotPanel 
    {
        protected TradingRegimeHelper _regime;
        protected VolumeHelper _volume;
        protected EnterPositionHelper _seller;

        protected BotTabScreener _tab;

               
        public RoboMarketerPanel(string name, StartProgram startProgram) : base(name, startProgram)
        {
            /*Description = OsLocalization.ConvertToLocString(
              "Eng: Testing screener robot" +
              "Ru:Торговый робот основанный на скринере");*/
            Description = "Торговый робот основанный на скринере";

            TabCreate(BotTabType.Screener);
            _tab = TabsScreener[0];

            // Subscribe на обновление свечей
            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            // Subscribe to the indicator update even для обновления индикаторов и тд
            ParametrsChangeByUser += Event_ParametrsChangeByUser;
        }

        #region Events

        //события завершения свечей
        protected virtual void _tab_CandleFinishedEvent(List<Candle> candles, BotTabSimple _cur_tab)
        {
            //TryEntryLogic(_base1, _futs1);
        }

        protected virtual void Event_ParametrsChangeByUser()
        {
            _tab.UpdateIndicatorsParameters();
        }


        #endregion




        }
}