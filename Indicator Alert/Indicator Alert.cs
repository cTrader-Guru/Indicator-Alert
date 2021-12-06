/*  CTRADER GURU 

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/cTraderGuru
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/terms-of-service/

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

        /// <summary>
        /// Nome del prodotto, identificativo, da modificare con il nome della propria creazione
        /// </summary>
        public const string NAME = "Indicator Alert";

        /// <summary>
        /// La versione del prodotto, progressivo, utilie per controllare gli aggiornamenti se viene reso disponibile sul sito ctrader.guru
        /// </summary>
        public const string VERSION = "1.0.3";

        #endregion

        #region Params

        /// <summary>
        /// Identità del prodotto nel contesto di ctrader.guru
        /// </summary>
        [Parameter(NAME + " " + VERSION, Group = "Identity", DefaultValue = "https://ctrader.guru/product/indicator-alert/")]
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

        /// <summary>
        /// L'output primario, può essere modificato a seconda delle esigenze
        /// </summary>
        [Output("Main")]
        public IndicatorDataSeries Result { get; set; }

        #endregion

        #region Property

        int LastIndex = -1;
        bool AlertInThisBar = true;
        WaitingLevel MonitoringFor = WaitingLevel.Neutral;

        #endregion

        #region Indicator Events

        /// <summary>
        /// Viene generato all'avvio dell'indicatore, si inizializza l'indicatore
        /// </summary>
        protected override void Initialize()
        {

            // --> Stampo nei log la versione corrente
            Print("{0} : {1}", NAME, VERSION);

        }

        /// <summary>
        /// Generato ad ogni tick, vengono effettuati i calcoli dell'indicatore
        /// </summary>
        /// <param name="index">L'indice della candela in elaborazione</param>
        public override void Calculate(int index)
        {

            if (!IsLastBar)
                return;

            // --> Ad ogni cambio candela resetto l'alert flag, è una semplice sicurezza ulteriore
            if (index != LastIndex)
            {

                if (LastIndex != -1)
                    AlertInThisBar = false;
                LastIndex = index;

            }

            // --> Tento di resettare l'alert per eventuali nuovi segnali nella stessa direzione
            if ((MonitoringFor == WaitingLevel.Up && Source.LastValue > (LevelUnder + FlagReset)) || (MonitoringFor == WaitingLevel.Down && Source.LastValue < (LevelOver - FlagReset)))
                MonitoringFor = WaitingLevel.Neutral;

            // --> Controllo se devo mettere in alert
            if ((MonitoringFor != WaitingLevel.Down && Source.LastValue > LevelOver) || (MonitoringFor != WaitingLevel.Up && Source.LastValue < LevelUnder))
                _alert();

            // --> Aggiorno il flag
            MonitoringFor = (Source.LastValue > LevelOver) ? WaitingLevel.Down : (Source.LastValue < LevelUnder) ? WaitingLevel.Up : MonitoringFor;

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gestisce le popup per gli alert
        /// </summary>
        private void _alert()
        {

            if (RunningMode != RunningMode.RealTime || AlertInThisBar)
                return;

            string mex = string.Format("{0} : {1} ( {2} ) breakout {3}", NAME, SymbolName, TimeFrame.ToString(), Math.Round(Source.LastValue, Symbol.Digits));

            AlertInThisBar = true;

            // --> La popup non deve interrompere la logica delle API, apertura e chiusura

            if(PopUpEnabled) new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();
            
            _toWebHook(mex);
            Print(mex);

        }


        public void _toWebHook(string custom)
        {

            if (!WebhookEnabled || custom == null || custom.Trim().Length < 1)
                return;

            string messageformat = custom.Trim();

            try
            {
                // --> Mi servono i permessi di sicurezza per il dominio, compreso i redirect
                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                // --> Autorizzo tutte le pagine di questo dominio
                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
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
