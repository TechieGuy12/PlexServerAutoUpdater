using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TE.Plex
{
    public class Api
    {
        #region Event Delegates
        /// <summary>
        /// The delegate for the Message event handler.
        /// </summary>
        /// <param name="sender">
        /// The object that triggered the event.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        public delegate void MessageChangedEventHandler(object sender, string messagee);
        #endregion

        #region Events
        /// <summary>
        /// The MessageChanged event member.
        /// </summary>
        public event MessageChangedEventHandler MessageChanged;

        /// <summary>
        /// Triggered when the message has changed.
        /// </summary>
        protected virtual void OnMessageChanged(string message)
        {
            MessageChanged?.Invoke(this, message);
        }
        #endregion

        #region Public Constants
        /// <summary>
        /// A constant representing an unknown value.
        /// </summary>
        public const int Unknown = -1;
        #endregion

        #region Private Variables
        /// <summary>
        /// The Plex server.
        /// </summary>
        private string _server;

        /// <summary>
        /// The Plex user token.
        /// </summary>
        private string _token;

        /// <summary>
        /// The HTTP client used to connect to the Plex website.
        /// </summary>
        private HttpClient _client = new HttpClient();
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the <see cref="Api"/> class when provided
        /// with the server name or IP address, and the Plex token.
        /// </summary>
        /// <param name="server">
        /// The name or IP address of the Plex server.
        /// </param>
        /// <param name="token">
        /// The user's Plex token.
        /// </param>
        public Api(string server, string token)
        {
            _server = server;
            _token = token;
        }
        #endregion

        #region Public Functions
        /// <summary>
        /// Gets the number of media currently being played on the Plex server.
        /// </summary>
        /// <returns>
        /// The number of items being played.
        /// </returns>
        public int GetPlayCount()
        {
            int playCount = Unknown;
            if (string.IsNullOrWhiteSpace(_server))
            {
                OnMessageChanged("The Plex server was not provided so the play count could not be retrieved.");
                return playCount;
            }

            if (string.IsNullOrWhiteSpace(_token))
            {
                OnMessageChanged("The Plex token was not provided so the play count could not be retrieved.");
                return playCount;
            }

            string url = $"http://{_server}:32400/status/sessions?X-Plex-Token={_token}";
            string content = null;
            try
            {
                using (HttpResponseMessage response = _client.GetAsync(url).Result)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        content = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        OnMessageChanged($"The connection to the Plex server wasn't successful. Status: {response.StatusCode.ToString()}.");
                    }
                }
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is HttpRequestException)
            {
                OnMessageChanged($"There was an issue sending the request to the Plex server. Reason: {ex.Message}");
                return playCount;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    OnMessageChanged($"Could not process the response result. Message: {e.Message}.");
                }
                return playCount;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                OnMessageChanged("No content was returned from the Plex server.");
                return playCount;
            }

            using (StringReader sr = new StringReader(content))
            {
                XmlSerializer serializer =
                    new XmlSerializer(typeof(MediaContainer));
                try
                {
                    MediaContainer mediaContainer =
                        (MediaContainer)serializer.Deserialize(sr);
                    playCount = Convert.ToInt32(mediaContainer.Size);
                }
                catch (Exception ex)
                    when (ex is InvalidOperationException || ex is FormatException || ex is OverflowException)
                {
                    OnMessageChanged($"The content could not be parsed. Reason: {ex.Message}");
                    return playCount;
                }
            }

            return playCount;
        }

        /// <summary>
        /// Gets the number of in progress recordings (i.e. by the DVR) on the Plex server.
        /// </summary>
        /// /// <returns>
        /// The number of items being currently recorded.
        /// </returns>
        public int GetInProgressRecordingCount()
        {
            int inProgressRecordingCount = Unknown;
            if (string.IsNullOrWhiteSpace(_server))
            {
                OnMessageChanged("The Plex server was not provided so the in progress recording count could not be retrieved.");
                return inProgressRecordingCount;
            }

            if (string.IsNullOrWhiteSpace(_token))
            {
                OnMessageChanged("The Plex token was not provided so the in progress recording count could not be retrieved.");
                return inProgressRecordingCount;
            }

            string url = $"http://{_server}:32400/media/subscriptions/scheduled?X-Plex-Token={_token}";
            string content = null;
            try
            {
                using (HttpResponseMessage response = _client.GetAsync(url).Result)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        content = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        OnMessageChanged($"The connection to the Plex server wasn't successful. Status: {response.StatusCode.ToString()}.");
                    }
                }
            }
            catch (Exception ex)
                when (ex is ArgumentNullException || ex is HttpRequestException)
            {
                OnMessageChanged($"There was an issue sending the request to the Plex server. Reason: {ex.Message}");
                return inProgressRecordingCount;
            }
            catch (AggregateException ae)
            {
                foreach (var e in ae.Flatten().InnerExceptions)
                {
                    OnMessageChanged($"Could not process the response result. Message: {e.Message}.");
                }
                return inProgressRecordingCount;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                OnMessageChanged("No content was returned from the Plex server.");
                return inProgressRecordingCount;
            }

            try
            {
                var xml = XDocument.Parse(content);
                inProgressRecordingCount = xml.Descendants("MediaGrabOperation").Count(x => (string)x.Attribute("status") == "inprogress");
            }
            catch (Exception ex)
                when (ex is XmlException)
            {
                OnMessageChanged($"The content could not be parsed. Reason: {ex.Message}");
                return inProgressRecordingCount;
            }

            return inProgressRecordingCount;
        }
        #endregion
    }
}
