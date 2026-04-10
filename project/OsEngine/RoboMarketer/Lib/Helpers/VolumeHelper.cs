using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.RoboMarketer.Lib.Helpers
{
    public class VolumeHelper
    {
        private BotPanel _bot;

        // GetVolume settings
        private StrategyParameterString _volumeType;
        private StrategyParameterDecimal _volume;
        private StrategyParameterString _tradeAssetInPortfolio;

        public VolumeHelper(BotPanel bot)
        {
            _bot = bot;
            // GetVolume settings
            _volumeType = _bot.CreateParameter("Алгоритм расчета объема", "Процент от депозита", new[] { "Количество лотов", "Заданная сумма в валюте счета", "Процент от депозита" }, "Торги");
            _volume = _bot.CreateParameter("Объем", 1m, 1.0m, 50, 1, "Торги");
            _tradeAssetInPortfolio = _bot.CreateParameter("Валюта торгов", "Prime", "Торги");
        }

        protected decimal GetLots()
        {
            return _volume.ValueDecimal;
        }

        public decimal GetLotsByFixedMoney(BotTabSimple tab)
        {
            decimal volume = 0;
            decimal contractPrice = tab.PriceBestAsk;
            //сколько штук можем купить
            volume = _volume.ValueDecimal / contractPrice;

            if (_bot.StartProgram == StartProgram.IsOsTrader)//Если робот на боевом сервере
            {
                IServerPermission serverPermission = ServerMaster.GetServerPermission(tab.Connector.ServerType);
                //если торговля идет лотами больше 1 то делим на объем лота
                if (serverPermission != null && serverPermission.IsUseLotToCalculateProfit && tab.Security.Lot != 0 && tab.Security.Lot > 1)
                {
                    volume = volume / tab.Security.Lot;
                }
                //округляем
                volume = Math.Round(volume, tab.Security.DecimalsVolume);
            }
            else // Tester or Optimizer
            {
                //в оптимизаторе не паримся
                volume = Math.Round(volume, 6);
            }

            return volume;
        }

        public decimal GetLotsByPercentDeposit(BotTabSimple tab)
        {
            Portfolio myPortfolio = tab.Portfolio;

            if (myPortfolio == null) { return 0;}
            decimal portfolioPrimeAsset = 0;

            if (_tradeAssetInPortfolio.ValueString == "Prime") 
            {
                portfolioPrimeAsset = myPortfolio.ValueCurrent;
            }
            else
            {//если валюта не поумолчанию
                List<PositionOnBoard> positionOnBoard = myPortfolio.GetPositionOnBoard();
                if (positionOnBoard == null){ return 0; }

                for (int i = 0; i < positionOnBoard.Count; i++)
                {
                    if (positionOnBoard[i].SecurityNameCode == _tradeAssetInPortfolio.ValueString)
                    {
                        portfolioPrimeAsset = positionOnBoard[i].ValueCurrent;
                        break;
                    }
                }
            }

            if (portfolioPrimeAsset == 0)
            {
                _bot.SendNewLogMessage("Can`t found portfolio " + _tradeAssetInPortfolio.ValueString, Logging.LogMessageType.Error);
                return 0;
            }

            decimal moneyOnPosition = portfolioPrimeAsset * (_volume.ValueDecimal / 100);

            decimal qty = moneyOnPosition / tab.PriceBestAsk / tab.Security.Lot;

            if (tab.StartProgram == StartProgram.IsOsTrader)
            {
                if (tab.Security.UsePriceStepCostToCalculateVolume == true
                   && tab.Security.PriceStep != tab.Security.PriceStepCost
                   && tab.PriceBestAsk != 0
                   && tab.Security.PriceStep != 0
                   && tab.Security.PriceStepCost != 0)
                {// расчёт количества контрактов для фьючерсов и опционов на Мосбирже
                    qty = moneyOnPosition / (tab.PriceBestAsk / tab.Security.PriceStep * tab.Security.PriceStepCost);
                }
                qty = Math.Round(qty, tab.Security.DecimalsVolume);
            }
            else
            {
                qty = Math.Round(qty, 7);
            }

            return qty;
        }

        public decimal GetVolume(BotTabSimple tab)
        {
            decimal volume = 0;

            if (_volumeType.ValueString == "Количество лотов") { return GetLots();}
            if (_volumeType.ValueString == "Заданная сумма в валюте счета"){ return GetLotsByFixedMoney(tab);}
            if (_volumeType.ValueString == "Процент от депозита") { return GetLotsByPercentDeposit(tab); }
            
            return volume;
        }


    }
}
