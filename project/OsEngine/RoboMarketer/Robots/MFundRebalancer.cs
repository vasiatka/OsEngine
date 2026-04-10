/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.Bybit.Entities;
using OsEngine.OsTrader;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/* Description
Робот предназначен для покупки фондов денежного рынка вечером на свободные средства и продаже при необходимости.

 */

namespace OsEngine.RoboMarketer.Robots
{
    [Bot("MFundRebalancer")]
    public class MFundRebalancer : BotPanel
    {
        private BotTabSimple _tab;

        private StrategyParameterString _regimeParameter;
        private StrategyParameterString _logging;
        private StrategyParameterString _mFundName;
        private StrategyParameterDecimal _minBalanceToBuy;
        private StrategyParameterDecimal _minBalanceToSell;
        private StrategyParameterDecimal _allowedSpreadSize;
        private StrategyParameterInt _icebergCount;

        private StrategyParameterCheckBox _tradeMonday;
        private StrategyParameterTimeOfDay _timeToBuyMonday;
        private StrategyParameterCheckBox _tradeTuesday;
        private StrategyParameterTimeOfDay _timeToBuyTuesday;
        private StrategyParameterCheckBox _tradeWednesday;
        private StrategyParameterTimeOfDay _timeToBuyWednesday;
        private StrategyParameterCheckBox _tradeThursday;
        private StrategyParameterTimeOfDay _timeToBuyThursday;
        private StrategyParameterCheckBox _tradeFriday;
        private StrategyParameterTimeOfDay _timeToBuyFriday;
        private StrategyParameterCheckBox _tradeSaturday;
        private StrategyParameterTimeOfDay _timeToBuySaturday;
        private StrategyParameterCheckBox _tradeSunday;
        private StrategyParameterTimeOfDay _timeToBuySunday;
        private StrategyParameterButton _rebalanceNowButton;
        private bool _botBuyPositionToday;

        public MFundRebalancer(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _regimeParameter = CreateParameter("Regime", "Off", new[] { "Off", "RebalancingContinously", "RebalancingOnceADay", "OnlyClose" });
            _minBalanceToBuy = CreateParameter("Minimum balance to buy Fund", 100m, 5000m, 5000m, 5000m);
            _minBalanceToSell = CreateParameter("Minimum balance to sell Fund", -4950m, 5000m, 5000m, 5000m);

            _mFundName = CreateParameter("Фонд для ребалансировки:", "TMON@", new[] { "TMON@", "LQDT", "AKMM", "BCSD+TQTF","SBMMSR" });

            _icebergCount = CreateParameter("Iceberg count", 1, 1, 50, 1);
            _allowedSpreadSize = CreateParameter("Allowed spread size(%)", 0.01m, 0.01m, 0.01m, 0.01m);

            _tradeMonday = CreateParameterCheckBox("Trading on Monday", true);
            _timeToBuyMonday = CreateParameterTimeOfDay("Time to Rebalance on Monday", 23, 0, 0, 0);
            _tradeTuesday = CreateParameterCheckBox("Trading on Tuesday", true);
            _timeToBuyTuesday = CreateParameterTimeOfDay("Time to Rebalance on Tuesday", 23, 0, 0, 0);
            _tradeWednesday = CreateParameterCheckBox("Trading on Wednesday", true);
            _timeToBuyWednesday = CreateParameterTimeOfDay("Time to Rebalance on Wednesday", 23, 0, 0, 0);
            _tradeThursday = CreateParameterCheckBox("Trading on Thursday", true);
            _timeToBuyThursday = CreateParameterTimeOfDay("Time to Rebalance on Thursday", 23, 0, 0, 0);
            _tradeFriday = CreateParameterCheckBox("Trading on Friday", true);
            _timeToBuyFriday = CreateParameterTimeOfDay("Time to Rebalance on Friday", 23, 0, 0, 0);
            _tradeSaturday = CreateParameterCheckBox("Trading on Saturday", false);
            _timeToBuySaturday = CreateParameterTimeOfDay("Time to Rebalance on Saturday", 23, 0, 0, 0);
            _tradeSunday = CreateParameterCheckBox("Trading on Sunday", false);
            _timeToBuySunday = CreateParameterTimeOfDay("Time to Rebalance on Sunday", 23, 0, 0, 0);

            _rebalanceNowButton = CreateParameterButton("Rebalance now");
            _rebalanceNowButton.UserClickOnButtonEvent += _rebalanceNowButton_UserClickOnButtonEvent;

            _logging = CreateParameter("Записывать лог", "Off", new[] { "Off", "On" });

            _timeToBuyMonday.ValueChange += _timeToBuy_ValueChange;
            _timeToBuyTuesday.ValueChange += _timeToBuy_ValueChange;
            _timeToBuyWednesday.ValueChange += _timeToBuy_ValueChange;
            _timeToBuyThursday.ValueChange += _timeToBuy_ValueChange;
            _timeToBuyFriday.ValueChange += _timeToBuy_ValueChange;
            _timeToBuySaturday.ValueChange += _timeToBuy_ValueChange;
            _timeToBuySunday.ValueChange += _timeToBuy_ValueChange;

            _mFundName.ValueChange += _mFundName_ValueChange;
            
            if (startProgram == StartProgram.IsOsTrader)
            {
                Thread thread = new Thread(StartThread);
                thread.IsBackground = true;
                thread.Start();
            }
            else
            {
                _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            }

            Description = OsLocalization.ConvertToLocString(
                "En:The robot is designed to purchase money market funds in the evening with available funds._" +
                "Ru:Робот предназначен для покупки фондов денежного рынка вечером на свободные средства._");
        }

        private void _mFundName_ValueChange()
        {
            Task.Run(ClosePositions);
        }

        private void _timeToBuy_ValueChange()
        {
            _timeLast = DateTime.MinValue;
        }


        private void _rebalanceTask()
        {
           RebalanceLogic(true);
        }

        private void _rebalanceNowButton_UserClickOnButtonEvent()
        {
            Task.Run(_rebalanceTask);
        }

        #region Work thread for Real

        private DateTime _timeLast = DateTime.MinValue;

        private void StartThread()
        {
            while (true)
            {
                Thread.Sleep(30000);

                try
                {
                    if (_regimeParameter.ValueString == "Off") { continue; }
                    if (_tab.IsConnected == false || _tab.IsReadyToTrade == false) { continue;}
                    if (_tab.Security == null || _tab.CandlesAll == null) { continue; }
                    if (!CheckSpread()){ continue; }
                    if (_regimeParameter.ValueString == "OnlyClose")
                    {
                        ClosePositions();
                        _regimeParameter.ValueString = "Off";
                        continue;
                    }
                    if (CheckDayOfWeek() == false) { continue; }
                    RebalanceLogic();
                }
                catch (Exception ex)
                {
                    SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                    Thread.Sleep(5000);
                }
            }
        }

        #endregion

        #region Candlestick completion event for Tester

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (_regimeParameter.ValueString == "Off") { return; }
            if (candles == null || candles.Count == 0) { return; }
            if (CheckDayOfWeek() == false) { return;}

            RebalanceLogic();
        }

        #endregion

        #region Logic

        private void RebalanceLogic(bool isForced=false)
        {
            try
            {
                TimeOfDay TimeR;
                //время ребалансировки
                switch (TimeServer.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        TimeR = _timeToBuyMonday.Value; break;
                    case DayOfWeek.Tuesday:
                        TimeR = _timeToBuyTuesday.Value; break;
                    case DayOfWeek.Wednesday:
                        TimeR = _timeToBuyWednesday.Value; break;
                    case DayOfWeek.Thursday:
                        TimeR = _timeToBuyThursday.Value; break;
                    case DayOfWeek.Friday:
                        TimeR = _timeToBuyFriday.Value; break;
                    case DayOfWeek.Saturday:
                        TimeR = _timeToBuySaturday.Value; break;
                    case DayOfWeek.Sunday:
                        TimeR = _timeToBuySunday.Value; break;
                    default: TimeR = _timeToBuyMonday.Value; break;
                }


                //фильтр 1
                if (_tab.Portfolio == null) { return; }
                if (StartProgram == StartProgram.IsOsTrader)
                {
                    if (_tab.Portfolio.PositionOnBoard == null) { return; }
                    
                    if (_tab.Security.Name != _mFundName.ValueString)
                    {
                        SendNewLogMessage(OsLocalization.ConvertToLocString(
                                   "En:The robot is designed only for rebalancing money market funds and for running in a tester. The traded instrument does not match the one selected in the settings. Check the fees for the selected fund with your broker._" +
                                   "Ru:Робот предназначен только для ребалансировки фондами денежного рынка и для запуска в тестере. Торгуемый инструмент не совпадает с выбранным в настройках. Проверьте комиссии на выбираемый фонд у вашего брокера._")
                                   , Logging.LogMessageType.Error);
                        Thread.Sleep(10000);
                        return;
                    }
                    if (_tab.Security.Name == "TMON@" && _tab.Connector.MyServer.ServerType != ServerType.TInvest)
                    {
                        SendNewLogMessage(OsLocalization.ConvertToLocString(
                                   "En:The robot is only intended for rebalancing TMON with the T-Investments broker and for testing._" +
                                   "Ru:Робот предназначен только для ребалансировки TMON у брокера Т-Инвестиции и для запуска в тестере_")
                                   , Logging.LogMessageType.Error);
                        Thread.Sleep(10000);
                        return;
                    }
                }
                //должно возвращаться положительное или отрицательное число относительно MinBalance
                decimal balance = GetPortfolioValue();

                
                if ( (TimeR < TimeServer
                     && _timeLast < TimeServer.Date) || isForced
                   )
                {
                    //покупаем или продаем в зависимости от знака баланса
                    BuyLogic(balance);
                    SellLogic(balance);
                    _timeLast = TimeServer.Date;
                }
                else if(_regimeParameter.ValueString == "RebalancingContinously")
                {
                    SellLogic(balance);
                }


            }
            catch(Exception ex)
            {
                _tab.SetNewLogMessage(ex.ToString(),Logging.LogMessageType.Error);
            }
        }

        private void BuyLogic(decimal balance)
        {
            //определяем сколько не хватает
            decimal freeBalance = balance - _minBalanceToBuy.ValueDecimal;

            //если деньги ниже допустимого лимита, то ничего не делаем
            if (freeBalance <= 0) { return; }

            //получаем объем на покупку
            decimal volume = GetVolume(freeBalance);

            if (volume <= 0) { return; }

            if (_icebergCount.ValueInt <= 1)
            {
                if (_tab.PositionOpenLong.Count > 0)
                {
                    _tab.BuyAtLimitToPosition(_tab.PositionOpenLong[0], _tab.PriceBestAsk, volume);
                }
                else
                {
                    _tab.BuyAtLimit(volume, _tab.PriceBestAsk);
                }
            }
            else
            {
                if (_tab.PositionOpenLong.Count > 0)
                {
                    _tab.BuyAtIcebergToPosition(_tab.PositionOpenLong[0], _tab.PriceBestAsk, volume, _icebergCount.ValueInt);
                }
                else
                {
                    _tab.BuyAtIceberg(volume, _tab.PriceBestAsk, _icebergCount.ValueInt);
                }
            }
            if (_logging == "On") { SendNewLogMessage("Rebalancer: Куплено " + volume + " лотов фонда.", Logging.LogMessageType.NoName); }
        }

        private void SellLogic(decimal balance)
        {
            //определяем сколько не хватает
            decimal freeBalance = balance - _minBalanceToSell.ValueDecimal;

            //если деньги выше допустимого лимита, то ничего не делаем
            if (freeBalance>=0) { return; }
           
           //получаем объем на продажу
           decimal volume = GetVolume(freeBalance);

           if (volume <= 0) { return; }


           if (_tab.PositionOpenLong.Count > 0)//тут продаем если есть, что продавать
           {
              if (volume > _tab.PositionOpenLong[0].OpenVolume)
              {
                volume = _tab.PositionOpenLong[0].OpenVolume;
              }
              //TODO сделать продажу по айсбергу
              _tab.CloseAtMarket(_tab.PositionOpenLong[0], volume);
           }
            if (_logging == "On") { SendNewLogMessage("Rebalancer: Продано " + volume + " лотов фонда.", Logging.LogMessageType.NoName); }
        }

        private void ClosePositions()
        {
            if (_tab.PositionsOpenAll.Count > 0)
            {
                for (int i = 0; i < _tab.PositionsOpenAll.Count; i++)
                {
                    _tab.CloseAtMarket(_tab.PositionsOpenAll[i], _tab.PositionsOpenAll[i].OpenVolume);
                }
            }
        }

        private decimal GetPortfolioValue()
        {
            try
            {
                decimal volume = 0;
                
                if (_tab.Connector.MyServer.ServerType == ServerType.TInvest)
                {  //боевой
                    List<PositionOnBoard> positions = _tab.Portfolio.GetPositionOnBoard();

                    if (positions.Count > 0)
                    {
                        for (int i = 0; i < positions.Count; i++)
                        {
                            if (positions[i].SecurityNameCode == "rub")
                            {
                                return positions[i].ValueCurrent - positions[i].ValueBlocked;
                            }
                        }
                    }
                }
                else if (_tab.Connector.MyServer.ServerType == ServerType.Tester)
                {  //тестер
                    List<PositionOnBoard> positions = _tab.Portfolio.GetPositionOnBoard();

                    decimal portfolioValue = _tab.Portfolio.ValueCurrent;
                    decimal volumToPosition = GetVolumeToPositions();
                    volume = portfolioValue - volumToPosition;
                }

                return volume;
            }
            catch (Exception ex)
            {
                SendNewLogMessage("GetPortfolioValue: " + ex.Message, Logging.LogMessageType.Error);
            }

            return 0;
        }

        private decimal GetVolumeToPositions() //метод только для тестера
        {
            decimal volumToPosition = 0;

            if (_tab.Connector.MyServer.ServerType != ServerType.Tester)
                return 0;

            for (int i = 0; i < OsTraderMaster.Master.PanelsArray.Count; i++)
            {
                List<Position> positionsBot = OsTraderMaster.Master.PanelsArray[i].OpenPositions;
                for (int j = 0; j < positionsBot.Count; j++)
                {
                   volumToPosition += positionsBot[j].OpenVolume * positionsBot[j].EntryPrice * positionsBot[j].Lots;
                }
            }

            return volumToPosition;
        }

        private bool CheckDayOfWeek()
        {
            DayOfWeek currentdDayOfWeek = TimeServer.DayOfWeek;

            if (currentdDayOfWeek == DayOfWeek.Monday && _tradeMonday == false) return false;
            else if (currentdDayOfWeek == DayOfWeek.Tuesday && _tradeTuesday == false) return false;
            else if (currentdDayOfWeek == DayOfWeek.Wednesday && _tradeWednesday == false) return false;
            else if (currentdDayOfWeek == DayOfWeek.Thursday && _tradeThursday == false) return false;
            else if (currentdDayOfWeek == DayOfWeek.Friday && _tradeFriday == false) return false;
            else if (currentdDayOfWeek == DayOfWeek.Saturday && _tradeSaturday == false) return false;
            else if (currentdDayOfWeek == DayOfWeek.Sunday && _tradeSunday == false) return false;
            else return true;
        }

        private bool CheckSpread()
        {
            try
            {
                decimal bestAsk = _tab.PriceBestAsk;
                decimal bestBid = _tab.PriceBestBid;

                decimal absoluteSpread = bestAsk - bestBid;
                decimal midPrice = (bestAsk + bestBid) / 2;

                if (midPrice == 0)
                {
                    return false;
                }

                decimal spreadPercent = Math.Round(absoluteSpread / midPrice * 100, 4);

                if (spreadPercent > _allowedSpreadSize.ValueDecimal && _allowedSpreadSize != 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SendNewLogMessage(ex.ToString(), Logging.LogMessageType.Error);
                return false;
            }
        }

        private decimal GetVolume(decimal balance)
        {
            decimal contractPrice = 0;

            if (balance > 0) { contractPrice = _tab.PriceBestAsk; }
            else { contractPrice = _tab.PriceBestBid; }
            
            if (contractPrice == 0) { return 0; }

            decimal volume = balance / contractPrice;

            if (StartProgram == StartProgram.IsOsTrader)
            {
                IServerPermission serverPermission = ServerMaster.GetServerPermission(_tab.Connector.ServerType);

                if (serverPermission != null &&
                    serverPermission.IsUseLotToCalculateProfit &&
                _tab.Security.Lot != 0 &&
                    _tab.Security.Lot > 1)
                {
                    volume = balance / (contractPrice * _tab.Security.Lot);
                }

                //volume = Math.Round(volume, _tab.Security.DecimalsVolume);
                //в данном случае имеется конкретный инструмент, знаем, что лоты целые
                //при отрицательном балансе будет продано больше, при положительном куплено меньше, чтобы не перепрыгнуть границу
                volume = Math.Floor(volume);

            }
            else // Tester or Optimizer
            {
                volume = Math.Floor(volume);
            }

            return Math.Abs(volume);
        }

        #endregion
    }
}
