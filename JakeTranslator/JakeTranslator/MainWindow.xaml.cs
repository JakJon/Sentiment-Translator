using System;
using System.Windows;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.IO;
using System.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Translator.Samples;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;



namespace JakeTranslator
{
    /// <summary>
    /// The goal of this WPF app is to demonstrate code for getting a security token, and translating a word or phrase into another langauge.
    /// The target langauge is selected from a combobox. The text of the translation is displayed and the translation is heard as speech.
    /// </summary>
   
    public partial class MainWindow : Window
    {
        // Before running the application, input the secret key for your subscription to Translator Text Translation API.
        private const string TEXT_TRANSLATION_API_SUBSCRIPTION_KEY = "b3f34a2885464192a5c99a2dba2c301c";

        // Object to get an authentication token
        private AzureAuthToken tokenProvider;
        // Cache language friendly names
        private string[] friendlyName = { " " };
        // Cache list of languages for speech synthesis
        private List<string> speakLanguages;
        // Dictionary to map language code from friendly name
        private Dictionary<string, string> languageCodesAndTitles = new Dictionary<string, string>();

        private class results
        {
            public string theResults;
        }
        
        private class TextDocument
        {
            public TextDocument(string text, string language)
            {
                Id = Guid.NewGuid().ToString();
                Language = language;
                Text = text;
            }
            [JsonProperty("language")]
            public string Language { get; private set; }
            [JsonProperty("id")]
            public string Id { get; private set; }
            [JsonProperty("text")]
            public string Text { get; private set; }
        }

        private class TextRequest
        {
            public TextRequest()
            {
                Documents = new List<TextDocument>();
            }
            [JsonProperty("documents")]
            public List<TextDocument> Documents { get; set; }
        }
        public class Scoreclass
        {
            public double score { get; set; }
            
        }
        
        public class sentimentScore
        {
            public List<Scoreclass> documents { get; set; }
            public List<object> errors { get; set; }
        }

        

        private void CatchAndThrow(JObject response)
        {
            if (response["errors"] != null && response["errors"].Children().Any())
            {
                throw new Exception(response["errors"].Children().First().Value<string>("message"));
            }
            if (response["code"] != null && response["message"] != null)
            {
                throw new Exception(response["message"].Value<string>());
            }
        }

        

        public MainWindow()
        {
            InitializeComponent();
            tokenProvider = new AzureAuthToken(TEXT_TRANSLATION_API_SUBSCRIPTION_KEY);
            GetLanguagesForTranslate(); //List of languages that can be translated
            GetLanguageNamesMethod(tokenProvider.GetAccessToken(), friendlyName); //Friendly name of languages that can be translated
            GetLanguagesForSpeakMethod(tokenProvider.GetAccessToken()); //List of languages that have a synthetic voice for text to speech
            enumLanguages(); //Create the drop down list of langauges
        }

        //*****POPULATE COMBOBOX*****
        private void enumLanguages()
        {
            //run a loop to load the combobox from the dictionary
            var count = languageCodesAndTitles.Count;

            for (int i = 0; i < count; i++)
            {
                LanguageComboBox.Items.Add(languageCodesAndTitles.ElementAt(i).Key);
            }
        }

        //*****BUTTON TO START TRANSLATION PROCESS
        private void translateButton_Click(object sender, EventArgs e)
        {
                                 
            //TRANSLATION PROCESS
            string languageCode;
            languageCodesAndTitles.TryGetValue(LanguageComboBox.Text, out languageCode); //get the language code from the dictionary based on the selection in the combobox

            if (languageCode == null)  //in case no language is selected.
            {
                languageCode = "en";

            }

            //*****BEGIN CODE TO MAKE THE CALL TO THE TRANSLATOR SERVICE TO PERFORM A TRANSLATION FROM THE USER TEXT ENTERED INCLUDES A CALL TO A SPEECH METHOD*****

            string txtToTranslate = textToTranslate.Text;

            string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + WebUtility.UrlEncode(txtToTranslate) + "&to={0}", languageCode);

            WebRequest translationWebRequest = WebRequest.Create(uri);

            translationWebRequest.Headers.Add("Authorization", tokenProvider.GetAccessToken()); //header value is the "Bearer plus the token from ADM

            WebResponse response = null;

            response = translationWebRequest.GetResponse();

            Stream stream = response.GetResponseStream();

            Encoding encode = Encoding.GetEncoding("utf-8");

            StreamReader translatedStream = new StreamReader(stream, encode);

            System.Xml.XmlDocument xTranslation = new System.Xml.XmlDocument();

            xTranslation.LoadXml(translatedStream.ReadToEnd());

            Translationtb.Text = "Translation -->   " + xTranslation.InnerText;

            if (speakLanguages.Contains(languageCode) && txtToTranslate != "")
            {
                //call the method to speak the translated text
                SpeakMethod(tokenProvider.GetAccessToken(), xTranslation.InnerText, languageCode);
            }
        }

        //*****SPEECH CODE*****
        private void SpeakMethod(string authToken, string textToVoice, String languageCode)
        {
            string translatedString = textToVoice;

            string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Speak?text={0}&language={1}&format=" + WebUtility.UrlEncode("audio/wav") + "&options=MaxQuality", translatedString, languageCode);

            WebRequest webRequest = WebRequest.Create(uri);
            webRequest.Headers.Add("Authorization", authToken);
            WebResponse response = null;
            try
            {
                response = webRequest.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    using (SoundPlayer player = new SoundPlayer(stream))
                    {
                        player.PlaySync();
                    }
                }
            }
            catch
            {

                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        results Thefinalone = new results();
        //*****CODE TO GET TRANSLATABLE LANGAUGE CODES*****
        private void GetLanguagesForTranslate()
        {

            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForTranslate";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers.Add("Authorization", tokenProvider.GetAccessToken());

            WebResponse response = null;

            try
            {
                response = WebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {

                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>)); List<string> languagesForTranslate = (List<string>)dcs.ReadObject(stream);
                    friendlyName = languagesForTranslate.ToArray(); //put the list of language codes into an array to pass to the method to get the friendly name.

                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        public string TheSentimentScore;

        private async Task getScore()
        {
            if (SentimentText.Content != "")
            {
                SentimentText.Content = "loading";
            }
            string score = "Sentiment rating:\n" + $"{await Sentiment("en", textToTranslate.Text)}";
            
            SentimentText.Content = score;
            
        }

        /// <summary>
        /// Sentiment analysis
        /// </summary>
        /// <param name="language"></param>
        /// <param name="text"></param>
        /// <returns>From 0 to 1 (1 being totally positive sentiment)</returns>
        public async Task<double> Sentiment(string language, string text)
        {
            string textToAnalyze = textToTranslate.Text;
            HttpClient _httpClient;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "8cad92d2a95549e3a67ef23240641ce5");
            var serviceEndpoint = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?";


            if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException();
            }
            var request = new TextRequest();
            request.Documents.Add(new TextDocument(text, language));
            var content = new StringContent(JsonConvert.SerializeObject(request), System.Text.Encoding.UTF8, "application/json");
            var result = await _httpClient.PostAsync($"{serviceEndpoint}sentiment", content).ConfigureAwait(false);
           
            
                      
            var response = JObject.Parse(await result.Content.ReadAsStringAsync());
            
            var test1 = Convert.ToString(response.Children().First());
            TheSentimentScore = test1;
            
                        
            return response["documents"].Children().First().Value<double>("score");
            
        }
      

        //*****CODE TO GET TRANSLATABLE LANGAUGE FRIENDLY NAMES FROM THE TWO CHARACTER CODES*****
        private void GetLanguageNamesMethod(string authToken, string[] languageCodes)
        {
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguageNames?locale=en";
            // create the request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers.Add("Authorization", tokenProvider.GetAccessToken());
            request.ContentType = "text/xml";
            request.Method = "POST";
            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String[]"));
            using (System.IO.Stream stream = request.GetRequestStream())
            {
                dcs.WriteObject(stream, languageCodes);
            }
            WebResponse response = null;
            try
            {
                response = request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    string[] languageNames = (string[])dcs.ReadObject(stream);

                    for (int i = 0; i < languageNames.Length; i++)
                    {

                        languageCodesAndTitles.Add(languageNames[i], languageCodes[i]); //load the dictionary for the combo box

                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        private void GetLanguagesForSpeakMethod(string authToken)
        {

            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForSpeak";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authToken);
            WebResponse response = null;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {

                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>));
                    speakLanguages = (List<string>)dcs.ReadObject(stream);

                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        private void SentimentButton_Click(object sender, RoutedEventArgs e)
        {
            if (textToTranslate.Text == null)
            {
                SentimentText.Content = "There must be english text in the text" +
                                        " box \nin order to return a sentiment score.";
            }
            else
            {
                getScore();
            }
            
            
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Translationtb.Text = "This Translator was made by Jake Jones using Microsofts Cognitive services APIS" +
                                   "\n\nThe sentiment button will give your entry a percantage value for the messages \"Positivity\" " +
                                   "0.0 being completely negative and 1 being completely positive. Scroll Down for more VVV\n\nAlso some translations will be spoken" +
                                   " aloud if it is compatible.";
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }
    }
}