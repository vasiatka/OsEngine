using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Формирует настройки для режима торгов и времени торговли

namespace OsEngine.RoboMarketer.Lib.Helpers
{
    public class TradingRegimeHelper
    {
        private BotPanel _bot;

        // Basic settings
        public bool _isRegimeUsed;
        public StrategyParameterString _regime;
        // Trade periods
        bool _isTradeTimeUsed;
        public NonTradePeriods _tradePeriodsSettings;
        private StrategyParameterButton _tradePeriodsShowDialogButton;

        public TradingRegimeHelper(BotPanel bot)
        {
            _bot = bot;
            _isRegimeUsed = false;
            _isTradeTimeUsed = false;
        }

        public TradingRegimeHelper UseRegimeOnOff()
        {
            _regime = _bot.CreateParameter("Regime", "Off", new[] { "Off", "On"}, "Режим торгов");
            _isRegimeUsed = true;
            return this;
        }

        public TradingRegimeHelper UseRegimeAll()
        {
            _regime = _bot.CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort" }, "Режим торгов");
            _isRegimeUsed = true;
            return this;
        }

        public TradingRegimeHelper UseTradeTime()
        {
            _isTradeTimeUsed = true;
            _tradePeriodsShowDialogButton = _bot.CreateParameterButton("Неторговые периоды", "Режим торгов");
            _tradePeriodsShowDialogButton.UserClickOnButtonEvent += _tradePeriodsShowDialogButton_UserClickOnButtonEvent;

            // non trade periods
            _tradePeriodsSettings = new NonTradePeriods(_bot.NameStrategyUniq);
            
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod1Start = new TimeOfDay() { Hour = 0, Minute = 0 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod1End = new TimeOfDay() { Hour = 10, Minute = 05 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod1OnOff = true;

            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod2Start = new TimeOfDay() { Hour = 13, Minute = 54 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod2End = new TimeOfDay() { Hour = 14, Minute = 6 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod2OnOff = false;

            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod3Start = new TimeOfDay() { Hour = 18, Minute = 30 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod3End = new TimeOfDay() { Hour = 24, Minute = 00 };
            _tradePeriodsSettings.NonTradePeriodGeneral.NonTradePeriod3OnOff = true;

            _tradePeriodsSettings.TradeInSunday = false;
            _tradePeriodsSettings.TradeInSaturday = false;

            _tradePeriodsSettings.Load();

            return this;
        }
        public void _tradePeriodsShowDialogButton_UserClickOnButtonEvent()
        {
            _tradePeriodsSettings.ShowDialog();
        }


        public bool IsTradingAllowed(BotTabSimple tab)
        {
            if (IsOff()) { return false; }
            if (IsConnected(tab) == false) { return false; }
            if (IsTradingTime(tab)==false) { return false; }

            return true;
        }

        public bool IsConnected(BotTabSimple tab)
        {
            if (tab.IsConnected == false || tab.IsReadyToTrade == false) { return false; }
            List<Candle> candles = tab.CandlesFinishedOnly;
            if (candles == null || candles.Count < 20) { return false; }
            return true;
        }

        public bool IsOn()
        {
            if (_isRegimeUsed && (_regime.ValueString == "On")) { return true; }
            
            return false;
        }
        public bool IsOff()
        {
            if (_isRegimeUsed && (_regime.ValueString == "Off")) { return true; }

            return false;
        }

        public bool IsOnlyLong()
        {
            if (_isRegimeUsed && (_regime.ValueString == "OnlyLong")) { return true; }

            return false;
        }

        public bool IsShortLong()
        {
            if (_isRegimeUsed && (_regime.ValueString == "OnlyShort")) { return true; }

            return false;
        }

        public bool IsTradingTime(BotTabSimple tab)
        {
           List<Candle> candles = tab.CandlesFinishedOnly;
           if (candles == null || candles.Count < 20) { return false; }
           if (_isTradeTimeUsed && (_tradePeriodsSettings.CanTradeThisTime(candles[^1].TimeStart) == false)) { return false; }

           return true;
        }

    }
}
