/*  CTRADER GURU 

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/cTraderGuru
    GitHub      : https://github.com/cTraderGURU/

*/

using System;
using cAlgo.API;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Net;

namespace cAlgo
{

    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class IndicatorAlert : Indicator
    {

        #region Enums

        enum WaitingLevel
        {

            Up,
            Down,
            Neutral

        }

        #endregion

        #region Identity

        public const string NAME = "Indicator Alert";

        public const string VERSION = "1.0.3";

        #endregion

        #region Params

        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://www.google.com/search?q=ctrader+guru+indicator+alert")]
        public string ProductInfo { get; set; }

        [Parameter("Alert Source", Group = "Params")]
        public DataSeries Source { get; set; }

        [Parameter("Level Over", DefaultValue = 80, Group = "Params")]
        public double LevelOver { get; set; }

        [Parameter("Level Under", DefaultValue = 20, Group = "Params")]
        public double LevelUnder { get; set; }

        [Parameter("Flag Reset", Group = "Params", DefaultValue = 10, MinValue = 0)]
        public double FlagReset { get; set; }

        [Parameter("Alert PopUp?", Group = "Params", DefaultValue = true)]
        public bool PopUpEnabled { get; set; }

        [Parameter("Enabled?", Group = "Webhook", DefaultValue = false)]
        public bool WebhookEnabled { get; set; }

        [Parameter("API", Group = "Webhook", DefaultValue = "https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage")]
        public string Webhook { get; set; }

        [Parameter("POST params", Group = "Webhook", DefaultValue = "chat_id=[ @CHATID ]&text={0}")]
        public string PostParams { get; set; }

        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        #endregion

        #region Property

        int LastIndex = -1;
        bool AlertInThisBar = true;
        WaitingLevel MonitoringFor = WaitingLevel.Neutral;

        #endregion

        #region Indicator Events

        protected override void Initialize()
        {

            Print("{0} : {1}", NAME, VERSION);

        }

        public override void Calculate(int index)
        {

            if (!IsLastBar)
                return;

            if (index != LastIndex)
            {

                if (LastIndex != -1)
                    AlertInThisBar = false;
                LastIndex = index;

            }

            if ((MonitoringFor == WaitingLevel.Up && Source.LastValue > (LevelUnder + FlagReset)) || (MonitoringFor == WaitingLevel.Down && Source.LastValue < (LevelOver - FlagReset)))
                MonitoringFor = WaitingLevel.Neutral;

            if ((MonitoringFor != WaitingLevel.Down && Source.LastValue > LevelOver) || (MonitoringFor != WaitingLevel.Up && Source.LastValue < LevelUnder))
                Alert();

            MonitoringFor = (Source.LastValue > LevelOver) ? WaitingLevel.Down : (Source.LastValue < LevelUnder) ? WaitingLevel.Up : MonitoringFor;

        }

        #endregion

        #region Private Methods

        private void Alert()
        {

            if (RunningMode != RunningMode.RealTime || AlertInThisBar)
                return;

            string mex = string.Format("{0} : {1} ( {2} ) breakout {3}", NAME, SymbolName, TimeFrame.ToString(), Math.Round(Source.LastValue, Symbol.Digits));

            AlertInThisBar = true;

            if (PopUpEnabled)
                new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();

            ToWebHook(mex);
            Print(mex);

        }


        public void ToWebHook(string custom)
        {

            if (!WebhookEnabled || custom == null || custom.Trim().Length < 1)
                return;

            string messageformat = custom.Trim();

            try
            {

                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.UploadString(myuri, string.Format(PostParams, messageformat));
                }

            } catch (Exception exc)
            {

                MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        #endregion

    }

}
