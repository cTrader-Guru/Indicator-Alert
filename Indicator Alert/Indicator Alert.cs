/*  CTRADER GURU --> Template 1.0.3

    Homepage    : https://ctrader.guru/
    Telegram    : https://t.me/ctraderguru
    Twitter     : https://twitter.com/cTraderGURU/
    Facebook    : https://www.facebook.com/ctrader.guru/
    YouTube     : https://www.youtube.com/channel/UCKkgbw09Fifj65W5t5lHeCQ
    GitHub      : https://github.com/cTraderGURU/
    TOS         : https://ctrader.guru/termini-del-servizio/

*/

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using cAlgo.API;

// --> Microsoft Visual Studio 2017 --> Strumenti --> Gestione pacchetti NuGet --> Gestisci pacchetti NuGet per la soluzione... --> Installa 
using Newtonsoft.Json;
using System.Threading;
using System.Windows.Forms;

namespace cAlgo
{

    // --> AccessRights = AccessRights.FullAccess se si vuole controllare gli aggiornamenti
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
        /// ID prodotto, identificativo, viene fornito da ctrader.guru, 74909 è il riferimento del template in uso
        /// </summary>
        public const int ID = 76227;

        /// <summary>
        /// Nome del prodotto, identificativo, da modificare con il nome della propria creazione
        /// </summary>
        public const string NAME = "Indicator Alert";

        /// <summary>
        /// La versione del prodotto, progressivo, utilie per controllare gli aggiornamenti se viene reso disponibile sul sito ctrader.guru
        /// </summary>
        public const string VERSION = "1.0.0";

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

            // --> Se viene settato l'ID effettua un controllo per verificare eventuali aggiornamenti
            _checkProductUpdate();

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
        /// Effettua un controllo sul sito ctrader.guru per mezzo delle API per verificare la presenza di aggiornamenti, solo in realtime
        /// </summary>
        private void _checkProductUpdate()
        {

            // --> Controllo solo se solo in realtime, evito le chiamate in backtest
            if (RunningMode != RunningMode.RealTime)
                return;

            // --> Organizzo i dati per la richiesta degli aggiornamenti
            Guru.API.RequestProductInfo Request = new Guru.API.RequestProductInfo 
            {

                MyProduct = new Guru.Product 
                {

                    ID = ID,
                    Name = NAME,
                    Version = VERSION

                },
                AccountBroker = Account.BrokerName,
                AccountNumber = Account.Number

            };

            // --> Effettuo la richiesta
            Guru.API Response = new Guru.API(Request);

            // --> Controllo per prima cosa la presenza di errori di comunicazioni
            if (Response.ProductInfo.Exception != "")
            {

                Print("{0} Exception : {1}", NAME, Response.ProductInfo.Exception);

            }
            // --> Chiedo conferma della presenza di nuovi aggiornamenti
            else if (Response.HaveNewUpdate())
            {

                string updatemex = string.Format("{0} : Updates available {1} ( {2} )", NAME, Response.ProductInfo.LastProduct.Version, Response.ProductInfo.LastProduct.Updated);

                // --> Informo l'utente con un messaggio sul grafico e nei log del cbot
                Chart.DrawStaticText(NAME + "Updates", updatemex, API.VerticalAlignment.Top, API.HorizontalAlignment.Left, Color.Red);

            }

        }

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

/// <summary>
/// NameSpace che racchiude tutte le feature ctrader.guru
/// </summary>
namespace Guru
{
    /// <summary>
    /// Classe che definisce lo standard identificativo del prodotto nel marketplace ctrader.guru
    /// </summary>
    public class Product
    {

        public int ID = 0;
        public string Name = "";
        public string Version = "";
        public string Updated = "";

    }

    /// <summary>
    /// Offre la possibilità di utilizzare le API messe a disposizione da ctrader.guru per verificare gli aggiornamenti del prodotto.
    /// Permessi utente "AccessRights = AccessRights.FullAccess" per accedere a internet ed utilizzare JSON
    /// </summary>
    public class API
    {
        /// <summary>
        /// Costante da non modificare, corrisponde alla pagina dei servizi API
        /// </summary>
        private const string Service = "https://ctrader.guru/api/product_info/";

        /// <summary>
        /// Costante da non modificare, utilizzata per filtrare le richieste
        /// </summary>
        private const string UserAgent = "cTrader Guru";

        /// <summary>
        /// Variabile dove verranno inserite le direttive per la richiesta
        /// </summary>
        private RequestProductInfo RequestProduct = new RequestProductInfo();

        /// <summary>
        /// Variabile dove verranno inserite le informazioni identificative dal server dopo l'inizializzazione della classe API
        /// </summary>
        public ResponseProductInfo ProductInfo = new ResponseProductInfo();

        /// <summary>
        /// Classe che formalizza i parametri di richiesta, vengono inviate le informazioni del prodotto e di profilazione a fini statistici
        /// </summary>
        public class RequestProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale richiediamo le informazioni
            /// </summary>
            public Product MyProduct = new Product();

            /// <summary>
            /// Broker con il quale effettiamo la richiesta
            /// </summary>
            public string AccountBroker = "";

            /// <summary>
            /// Il numero di conto con il quale chiediamo le informazioni
            /// </summary>
            public int AccountNumber = 0;

        }

        /// <summary>
        /// Classe che formalizza lo standard per identificare le informazioni del prodotto
        /// </summary>
        public class ResponseProductInfo
        {

            /// <summary>
            /// Il prodotto corrente per il quale vengono fornite le informazioni
            /// </summary>
            public Product LastProduct = new Product();

            /// <summary>
            /// Eccezioni in fase di richiesta al server, da utilizzare per controllare l'esito della comunicazione
            /// </summary>
            public string Exception = "";

            /// <summary>
            /// La risposta del server
            /// </summary>
            public string Source = "";

        }

        /// <summary>
        /// Richiede le informazioni del prodotto richiesto
        /// </summary>
        /// <param name="Request"></param>
        public API(RequestProductInfo Request)
        {

            RequestProduct = Request;

            // --> Non controllo se non ho l'ID del prodotto
            if (Request.MyProduct.ID <= 0)
                return;

            // --> Dobbiamo supervisionare la chiamata per registrare l'eccexione
            try
            {

                // --> Strutturo le informazioni per la richiesta POST
                NameValueCollection data = new NameValueCollection 
                {
                    {
                        "account_broker",
                        Request.AccountBroker
                    },
                    {
                        "account_number",
                        Request.AccountNumber.ToString()
                    },
                    {
                        "my_version",
                        Request.MyProduct.Version
                    },
                    {
                        "productid",
                        Request.MyProduct.ID.ToString()
                    }
                };

                // --> Autorizzo tutte le pagine di questo dominio
                Uri myuri = new Uri(Service);
                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                // -->> Richiedo le informazioni al server
                using (var wb = new WebClient())
                {

                    wb.Headers.Add("User-Agent", UserAgent);

                    var response = wb.UploadValues(myuri, "POST", data);
                    ProductInfo.Source = Encoding.UTF8.GetString(response);

                }

                // -->>> Nel cBot necessita l'attivazione di "AccessRights = AccessRights.FullAccess"
                ProductInfo.LastProduct = JsonConvert.DeserializeObject<Product>(ProductInfo.Source);

            } catch (Exception Exp)
            {

                // --> Qualcosa è andato storto, registro l'eccezione
                ProductInfo.Exception = Exp.Message;

            }

        }

        /// <summary>
        /// Esegue un confronto tra le versioni per determinare la presenza di aggiornamenti
        /// </summary>
        /// <returns></returns>
        public bool HaveNewUpdate()
        {

            // --> Voglio essere sicuro che stiamo lavorando con le informazioni giuste
            return (ProductInfo.LastProduct.ID == RequestProduct.MyProduct.ID && ProductInfo.LastProduct.Version != "" && RequestProduct.MyProduct.Version != "" && new Version(RequestProduct.MyProduct.Version).CompareTo(new Version(ProductInfo.LastProduct.Version)) < 0);

        }

    }

}
