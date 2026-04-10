using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.GateIo.GateIoFutures.Entities;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.RoboMarketer.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.RoboMarketer.Common.Helpers
{
    public class FutSpotArbitrageHelper
    {
        private BotPanel _bot;

        // GetVolume settings
        private StrategyParameterDecimal _riskFreeRate;
        private StrategyParameterDecimal _comission;
        private StrategyParameterDecimal _grettings;

        public FutSpotArbitrageHelper(BotPanel bot)
        {
            _bot = bot;

            _riskFreeRate = _bot.CreateParameter("Безрисковая ставка", 15.5m, 15.5m, 15.5m, 0, "Арбитраж");
            _comission = _bot.CreateParameter("Комиссия за сделку", 15.5m, 15.5m, 15.5m, 0, "Арбитраж");
            _grettings = _bot.CreateParameter("Сколько пунктов цены хотим взять с фьючерса", 50m, 50m, 200m, 10, "Арбитраж");

        }

        public decimal Slider(BotTabSimple botLegSec, BotTabSimple botLegFut)
        {
            if (botLegSec.IsConnected == false
                || botLegSec.IsReadyToTrade == false)
            {
                return 0;
            }

            List<Candle> baseCandles = botLegSec.CandlesAll;

            if (baseCandles == null
                || baseCandles.Count < 20)
            {
                return 0;
            }

            
            DateTime currentTime = baseCandles[^1].TimeStart;



            if (botLegFut == null)
            {
                return 0;
            }

            Security fut = botLegFut.Security;

            if (fut.Expiration == DateTime.MinValue)
            {
                return 0;
            }

            decimal daysByExpiration = Convert.ToDecimal((fut.Expiration - currentTime).TotalDays);

            if(_bot.StartProgram == StartProgram.IsTester)
            {
                return _riskFreeRate.ValueDecimal / 365 * daysByExpiration * baseCandles[^1].Close;
            }

            return _riskFreeRate.ValueDecimal / 365 * daysByExpiration * Convert.ToDecimal(botLegSec.MarketDepth.Bids[0].Price);
        }

        public bool IsSpreadBiggerSlider(BotTabSimple botLegSec, BotTabSimple botLegFut)
        {
            decimal _slider = Slider(botLegSec, botLegFut);

            decimal _bid = Convert.ToDecimal(botLegFut.MarketDepth.Bids[0].Price);
            decimal _ask = Convert.ToDecimal(botLegSec.MarketDepth.Bids[0].Price);

            if(_bid<=_ask)//в случае беквордации все плохо 
            {
                return false;
            }

            return _bid - _ask - (_bid + _ask)* _comission.ValueDecimal*2-_slider- _grettings.ValueDecimal > 0;
        }



        


    }
}
