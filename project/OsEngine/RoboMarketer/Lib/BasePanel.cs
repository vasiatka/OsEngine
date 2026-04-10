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

using OsEngine.RoboMarketer.Common.Settings;

namespace OsEngine.RoboMarketer.Common
{
    public abstract class BasePanel : BotPanel 
    {
        protected BotTabScreener _tab;

               
        public BasePanel(string name, StartProgram startProgram) : base(name, startProgram)
        {
            /*Description = OsLocalization.ConvertToLocString(
              "Eng: Testing screener robot" +
              "Ru:Торговый робот основанный на скринере");*/
            Description = "Торговый робот основанный на скринере";

            TabCreate(BotTabType.Screener);
            _tab = TabsScreener[0];






        }


        



    }
}