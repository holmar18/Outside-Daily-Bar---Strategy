using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;


using System.Globalization;

/*

    STRATEGY
    First strategy in this PDF documnet : https://www.youtube.com/watch?v=lrKWUeBc14s
    Name: How to Trade EUR/USD Using a Simple Outside Daily Bar Setup
    
    STRATEGY RULES:
    IMPORTANT: You should set your timezone on the charts to EST / New York time, with 5 daily bars per week.
    
    
    LONG / BUY TRADE: - (Also see days of the week below)
    1. Look at the Daily chart at 17:00 EST when the bar is completed.
    2. Looking at Daily charts we want to see an Outside Bar (the high is greater than the high
    of the previous day and the low is less than the low of the previous day).
    3. We also need to see the close of the Outside bar less than the low of the previous day.
    4. If the above conditions are met we enter a Long trade at the open of the next bar.
        Depending on your broker, this will be immediately or up to 15 minutes later (17:15).
        However if the setup bar was on a Friday, the Buy order would be placed on the opening
        bar Sunday.
    5. Place a Stop Loss 200 pips away from your entry price.

    
    
    EXIT RULES:
    Simple exit. Simple often works best!
    You will exit the trade in 1 of 2 ways.
    The most common exit will be in profit.
    
    1. Once in a trade check the close of the next daily bar (17:00 EST). If you are in profit
        then exit at the open of the next bar. Depending on your broker, this will be immediately
        or up to 15 minutes later (17:15). However if the close was on a Friday, then take profit
        at the opening bar on Sunday. But if you are not in profit then leave the trade open.
    2. Or your Stop Loss is hit, you take a loss and you wait for a new trade.
    

*/

namespace cAlgo
{

    /// <summary>
    /// <para><b>cTrader Guru | Extensios</b></para>
    /// <para>A group of generic extensions that make the developer's life easier</para>
    /// </summary>
    public static class Extensions
    {
    
        #region DateTime

        /// <param name="Culture">Localization of double value</param>
        /// <returns>double : Time representation in double format (example : 10:34:07 = 10,34)</returns>
        public static double ToDouble(this DateTime thisDateTime, string Culture = "en-EN")
        {

            string nowHour = (thisDateTime.Hour < 10) ? string.Format("0{0}", thisDateTime.Hour) : string.Format("{0}", thisDateTime.Hour);
            string nowMinute = (thisDateTime.Minute < 10) ? string.Format("0{0}", thisDateTime.Minute) : string.Format("{0}", thisDateTime.Minute);

            return string.Format("{0}.{1}", nowHour, nowMinute).ToDouble(Culture);

        }

        #endregion
    
    
        #region String

        /// <param name="Culture">Localization of double value</param>
        /// <returns>double : Time representation in double format (example : "10:34:07" = 10,34)</returns>
        public static double ToDouble(this string thisString, string Culture = "en-EN")
        {
            var culture = CultureInfo.GetCultureInfo(Culture);
            return double.Parse(thisString.Replace(',', '.').ToString(CultureInfo.InvariantCulture), culture);

        }

        #endregion

        #region Spread
        /// <param name="MaxSpread">Settings value for Maximum allowed spread</param>
        /// <returns>bool : if the spread is to large will return <b>true</b></returns>
        public static bool SpreadFilter(this Symbol thisSymbol, double MaxSpread) {
            var spread = thisSymbol.TickSize == 0.01 ? thisSymbol.Spread : thisSymbol.Spread * 10000;
            if(spread.CompareTo(MaxSpread) < 0) {
                return true;
            }
            return false;
        }


        #endregion



        #region Positions
        
        /// <param name="label">Settings label value (filters open positions made only by this bot)</param>
        /// <returns>bool : if there is an open position from this bot returns <b>true</b></returns>
        public static bool Count(this Positions thisPos, string label) {
            var pos = thisPos.Find(label);
            if(pos != null) {
                return true;
            }
            return false;
        }
        
        
        /// <summary>
        /// Closes a trading position with the specified label if the position's gross profit is positive.
        /// </summary>
        /// <param name="positions">The collection of trading positions.</param>
        /// <param name="label">The label of the position to close.</param>
        public static void ClosePositivePosition(this Positions positions, string label)
        {
            Position position = positions.Find(label);
        
            // Check if the position's gross profit is positive before closing
            if (position != null && position.GrossProfit > 0)
            {
                position.Close();
            }
        }
        #endregion

        #region Symbol
        
        /// <param name="AccountBalance">Account.balance value</param>
        /// <param name="RiskPercentage">% of capital to risk per trade</param>
        /// <param name="_StopLoss">SL difference from Stoploss & Entry</param>
        /// <returns>double : Lot size for Forex/Stocks</returns>
        public static double CalculateLotSize(this Symbol thisSymbol, double AccountBalance, double RiskPercentage, double _StopLoss) {
            var amount_to_risk_per_trade = AccountBalance * (RiskPercentage / 100);
            var  PipScale = thisSymbol.PipValue;
            var trade_volume   = amount_to_risk_per_trade / (_StopLoss * PipScale);
            var truncation_factor   = thisSymbol.LotSize * PipScale * 100;
            var trade_volume_truncated = ( (int) (trade_volume / truncation_factor)) * truncation_factor;
            
            return thisSymbol.TickSize == 0.01 ? thisSymbol.NormalizeVolumeInUnits(trade_volume) : thisSymbol.NormalizeVolumeInUnits(trade_volume_truncated); 
        }
        
        /// <param name="tradeSize">SL difference from Stoploss,Entry Or Atr value or Any simular</param>
        /// <param name="StopLossMultiplier">Value to multiply the stoploss with default in settings = 1</param>
        /// <returns>double : stoploss size</returns>
        public static double CalculateStopLoss(this Symbol thisSymbol, double tradeSize, double StopLossMultiplier) {
            return (tradeSize * (thisSymbol.TickSize / thisSymbol.PipSize * Math.Pow(10, thisSymbol.Digits))) * StopLossMultiplier;
        }
        
        /// <param name="tradeSize">SL difference from Stoploss,Entry Or Atr value or Any simular</param>
        /// <param name="StopLossMultiplier">Value to multiply the stoploss with default in settings = 1<</param>
        /// <param name="TakeProfit">TP 2 = 2 tp 1 sl</param>
        /// <returns>double : takeprofit size</returns>
        public static double CalcTakeProfit(this Symbol thisSymbol, double tradeSize, double StopLossMultiplier, double TakeProfit) {
            var atrInPips = tradeSize * (thisSymbol.TickSize / thisSymbol.PipSize * Math.Pow(10, thisSymbol.Digits));
            return (atrInPips * StopLossMultiplier) * TakeProfit;
        }

        #endregion


        #region Bars
        /// <summary>
        /// Determines whether the current bar is an outer bar compared to the previous bar.
        /// </summary>
        /// <param name="bars">The collection of bars to analyze.</param>
        /// <returns>
        /// Returns true if the current bar is an outer bar (higher high, lower low, and lower close) compared to the previous bar.
        /// Returns false otherwise.
        /// </returns>
        public static bool IsOuterBar(this Bars bars)
        {
            int i = bars.Count - 2;
        
            double bar1High = bars.HighPrices[i];
            double bar1Low = bars.LowPrices[i];
            double bar1Close = bars.ClosePrices[i];
        
            double bar2High = bars.HighPrices[i - 1];
            double bar2Low = bars.LowPrices[i - 1];
            double bar2Close = bars.ClosePrices[i - 1];
        
            bool isOutsideBar = bar1High > bar2High && bar1Low < bar2Low && bar1Close < bar2Close;
            return isOutsideBar;
        }

        #endregion

    }
    
}


/// <summary>
/// Represents the possible trading states in a state machine.
/// </summary>
public enum TradingState
{
    /// <summary>
    /// The state when waiting for an outer bar.
    /// </summary>
    WaitingForOuterBar,

    /// <summary>
    /// The state when waiting for an exit condition.
    /// </summary>
    WaitingForExit
}


public enum FridayBlocker
{
    IsFriday,
    IsSunday,
    IsNotFriday
}


namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.None, TimeZone = TimeZones.EasternStandardTime)]
    public class NewBotTemplateStocksForexv11 : Robot
    {
        #region Settings

        #region Trade Settings
        [Parameter("Position Label", DefaultValue = "BOT", Group = "Trade Settings")]
        public string PosLabel { get; set; }


        #endregion

        #region Risk Settings
        [Parameter("Trade size % of account", DefaultValue = 1, MinValue = 0.1, Step = 0.1, Group = "Risk Settings")]
        public double Risk_Percentage { get; set; }
        
        [Parameter("TakeProfit (Multiplied with ATR)", DefaultValue = 20, MinValue = 1, Step = 1, Group = "Risk Settings")]
        public double TakeProfit { get; set; }
        
        
        [Parameter("Max spread (No trades if spread above)", DefaultValue = 0.5, MinValue = 0.1, Step = 0.1, Group = "Risk Settings")]
        public double MaxSpread { get; set; }
        #endregion


        #endregion

        #region Built in Functions
        protected override void OnStart()
        {
            Positions.Closed += ToggleStateWhenPositionClose;
        }

        protected override void OnTick()
        {
            FridayTradeblocker();
        }
        
        protected override void OnBar() {
            RunStrategy();
        }
        #endregion

        #region Variables
        public TradingState currentState = TradingState.WaitingForOuterBar;
        public FridayBlocker fridayblock;
        #endregion


        #region Strategy
        public void RunStrategy()
        {
            //Print("1: ", currentState == TradingState.WaitingForOuterBar);
            //Print("2: ", fridayblock != FridayBlocker.IsFriday);
            //Print("3: ", Bars.IsOuterBar() );
            if(Bars.IsOuterBar() 
                && currentState == TradingState.WaitingForOuterBar
                && fridayblock != FridayBlocker.IsFriday
                || fridayblock == FridayBlocker.IsSunday
               )
            {
                Print("Have An outer bar");
                ExecuteTrade();
            }
            else if (currentState == TradingState.WaitingForExit)
            {
                Positions.ClosePositivePosition(PosLabel);
            }
            
        }


        #endregion



        #region Trade 
        public void ExecuteTrade() 
        {
            double SIZE = Symbol.CalculateLotSize(Account.Balance, Risk_Percentage, 200);
            
            ExecuteMarketOrder(TradeType.Buy, Symbol.Name, SIZE, PosLabel, 200, 200 * TakeProfit);
            currentState = TradingState.WaitingForExit;
        }


        public void FridayTradeblocker()
        {
            if(Server.Time.DayOfWeek == DayOfWeek.Friday)
            {
                fridayblock = FridayBlocker.IsFriday;
            }
            else if(Server.Time.DayOfWeek == DayOfWeek.Sunday && fridayblock == FridayBlocker.IsFriday)
            {
                fridayblock = FridayBlocker.IsFriday;
            }
            else
            {
                fridayblock = FridayBlocker.IsNotFriday;
            }
        }
        #endregion


        #region Other
        public void DrawHorizantalLine(string name, double y, Color color)
        {
            Chart.DrawHorizontalLine(name, y, color);
        }
        
        
        public void ToggleStateWhenPositionClose(PositionClosedEventArgs closedPosition)
        {
            currentState = TradingState.WaitingForOuterBar;
        }
        
        #endregion

    }
}