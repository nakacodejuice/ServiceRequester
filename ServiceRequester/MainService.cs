using System.Net;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Xml;
using System.Web.Services.Description;
using Service.RNG;

namespace ServiceRequester
{
    public partial class MainService : ServiceBase
    {

        public static StConfig st;
        public struct StConfig
        {
            public string WSRNG, WSTrain, frequencyMiliSeconds, loginRNG, passRNG, loginTrain, passTrain;

            public StConfig(string p1, string p2, string IP2, string lRNG,string pRNG,
                                string logTrain,string pTrain)
            {
                WSRNG = p1;
                WSTrain = p2;
                frequencyMiliSeconds = IP2;
                loginRNG = lRNG;
                passRNG = pRNG;
                loginTrain = logTrain;
                passTrain = pTrain;
            }
        }

        public MainService(string[] args)
        {
            st = readconfig(args[0], "configRest", "WSRNG");
            InitializeComponent();


        }

        public MainService()
        {
            st = readconfig("config.xml", "configRest", "WSRNG");
            InitializeComponent();


        }

        protected override void OnStart(string[] args)
        {
            string result;
            Service.RNG.DataExchange n = new DataExchange();
            n.UseDefaultCredentials = false;
            n.Credentials = new NetworkCredential(st.loginRNG, st.passRNG);
            result = n.ВыполнитьАлгоритмИПолучитьРезультат("Тест", "{}", false, false, true);


        }

        protected override void OnStop()
        {

        }

        protected override void OnPause()
        {
            //stopSerialRead = true;
        }

        protected override void OnContinue()
        {
            string[] args = { "", "" };
            OnStart(args);
        }

        public static StConfig readconfig(string pathtoXML, string RootElement, string Node)
        {
            StConfig cf = new StConfig();
            // XElement el = new XElement();
            using (XmlReader reader = XmlReader.Create(pathtoXML))
            {
                while (reader.Read())
                {
                    if (reader.Name=="WSRNG")
                    {
                        reader.Read();
                        if(reader.Value.IndexOf('\n') ==-1)
                            cf.WSRNG = reader.Value;
                    }
                      else if (reader.Name=="WSTrain")
                        {
                            reader.Read();
                            if (reader.Value.IndexOf('\n') == -1)
                                cf.WSTrain = reader.Value;
                        }
                    else if (reader.Name == "frequencyMiliSeconds")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.frequencyMiliSeconds = reader.Value;
                    }
                    else if (reader.Name == "loginRNG")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.loginRNG = reader.Value;
                    }
                    else if (reader.Name == "passRNG")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.passRNG = reader.Value;
                    }
                    else if (reader.Name == "loginTrain")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.loginTrain = reader.Value;
                    }
                    else if (reader.Name == "passTrain")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.passTrain = reader.Value;
                    }
                }



            }
            return cf;
        }
    
    }

}
