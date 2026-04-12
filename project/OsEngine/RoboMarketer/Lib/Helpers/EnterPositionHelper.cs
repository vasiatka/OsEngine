using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.TraderNet.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.RoboMarketer.Lib.Helpers
{
    public class EnterPositionHelper
    {
        private BotPanel _bot;

        private StrategyParameterString _tradeEnterType;
        private StrategyParameterString _tradeExitType;
        private StrategyParameterInt _icebergCount;
        private StrategyParameterInt _icebergTimeDelta;

        private bool isMaxPositionsUsed = false;
        private StrategyParameterInt _maxPositions;

        public EnterPositionHelper(BotPanel bot)
        {
            _bot = bot;
            _tradeEnterType = _bot.CreateParameter("Тип входа в позицию", "Всем объемом", new[] { "Всем объемом", "Айсбергом" }, "Торги");
            _tradeExitType = _bot.CreateParameter("Тип выхода из позиции", "Всем объемом", new[] { "Всем объемом", "Айсбергом" }, "Торги");
            _icebergCount = _bot.CreateParameter("Число контрактов в одной части айсберга", 1, 1, 3, 1, "Торги");
            _icebergTimeDelta = _bot.CreateParameter("Интервалы между заявками в айсберге", 1000, 1000, 10000, 500, "Торги");
        }

        public StrategyParameterInt MaxPositions
        {
            get
            {// возвращаем значение поля
                if(isMaxPositionsUsed)
                { return _maxPositions; }
                else { return null;}
            }
            set
            { // устанавливаем новое значение поля
                _maxPositions = value;
                isMaxPositionsUsed = true;
            }
        }
        public EnterPositionHelper UseMaxPositions()
        {
            MaxPositions =  _bot.CreateParameter("Максимальное число позиций", 10, 0, 20, 1, "Торги");
            return this;
        }


        public Position BuyAtMarket(BotTabSimple tab, decimal volume)
        {
            if (MaxPositions != null && _bot.OpenPositions.Count>= MaxPositions.ValueInt) return null;

            if (_tradeEnterType.ValueString== "Всем объемом")
            {
               return tab.BuyAtMarket(volume);
            }

            if (_tradeEnterType.ValueString == "Айсбергом")
            {
                return tab.BuyAtIcebergMarket(volume, _icebergCount.ValueInt, _icebergTimeDelta.ValueInt);
            }
            return null;
        }

        public Position SellAtMarket(BotTabSimple tab, decimal volume)
        {
            if (MaxPositions != null && _bot.OpenPositions.Count >= MaxPositions.ValueInt) return null;

            if (_tradeEnterType.ValueString == "Всем объемом")
            {
                return tab.SellAtMarket(volume);
            }

            if (_tradeEnterType.ValueString == "Айсбергом")
            {
                return tab.SellAtIcebergMarket(volume, _icebergCount.ValueInt, _icebergTimeDelta.ValueInt);
            }
            return null;
        }

        public void ClosePositionAtMarket(BotTabSimple tab, Position pos)
        {
            if (_bot.StartProgram != StartProgram.IsOsTrader)
            {
                if (pos.State != PositionStateType.Open)
                {// в тестере и оптимизаторе не допускаем спама ордерами
                    return;
                }
            }

            if (_tradeExitType.ValueString == "Всем объемом")
            {
                tab.CloseAtMarket(pos, pos.OpenVolume);
            }

            if (_tradeEnterType.ValueString == "Частями")
            {
                tab.CloseAtIcebergMarket(pos, pos.OpenVolume, _icebergCount.ValueInt, _icebergTimeDelta.ValueInt);
            }
        }

        public void CloseAllPositionAtMarket(BotTabSimple tab)
        {
            List<Position> positions = tab.PositionsOpenAll;
            for (int i=0; i<positions.Count; i++)
            {
                ClosePositionAtMarket(tab, positions[i]);
            }
        }

    }
}
