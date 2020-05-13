/*  CTRADER GURU --> Template 1.0.6

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/termini-del-servizio/

*/

using System;
using cAlgo.API;
using System.Threading;
using System.Windows.Forms;

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
        public const string VERSION = "1.0.1";

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

            new Thread(new ThreadStart(delegate { MessageBox.Show(mex, "BreakOut", MessageBoxButtons.OK, MessageBoxIcon.Information); })).Start();
            Print(mex);

        }

        #endregion

    }

}
