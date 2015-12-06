using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Player
{
    public class customLicenseAcquirer : LicenseAcquirer
    {
        private string challengeString;
        string _mediaElementName;        

        public customLicenseAcquirer(string mediaElementName)
        {
            _mediaElementName = mediaElementName;
        }

        // The default implementation of OnAcquireLicense calls into the MediaElement to acquire a
        //  license. It is called when the Media pipeline is building a topology and will be raised
        // before MediaOpened is raised.
        protected override void OnAcquireLicense(System.IO.Stream licenseChallenge, Uri licenseServerUri)
        {
            StreamReader sr = new StreamReader(licenseChallenge);
            challengeString = sr.ReadToEnd();

            // Need to resolve the URI for the License Server -- make sure it is correct
            // and store that correct URI as resolvedLicenseServerUri.
            Uri resolvedLicenseServerUri;
            if (LicenseServerUriOverride == null)
                resolvedLicenseServerUri = licenseServerUri;
            else
                resolvedLicenseServerUri = LicenseServerUriOverride;

            // Make a HttpWebRequest to the License Server.
            HttpWebRequest request = WebRequest.Create(resolvedLicenseServerUri) as HttpWebRequest;
            request.Method = "POST";

            // Set ContentType through property    
            request.ContentType = "application/xml";

            //  ADD REQUIRED HEADERS.
            // The headers below are necessary so that error handling and redirects are handled 
            // properly via the Silverlight client.
            request.Headers["msprdrm_server_redirect_compat"] = "false";
            request.Headers["msprdrm_server_exception_compat"] = "false";

            //  Initiate getting request stream  
            IAsyncResult asyncResult = request.BeginGetRequestStream(new AsyncCallback(RequestStreamCallback), request);
        }

        // This method is called when the asynchronous operation completes.
        void RequestStreamCallback(IAsyncResult ar)
        {
            HttpWebRequest request = ar.AsyncState as HttpWebRequest;

            // populate request stream  
            request.ContentType = "text/xml";
            Stream requestStream = request.EndGetRequestStream(ar);
            StreamWriter streamWriter = new StreamWriter(requestStream, System.Text.Encoding.UTF8);

            streamWriter.Write(challengeString);
            streamWriter.Close();

            // Make async call for response  
            request.BeginGetResponse(new AsyncCallback(ResponseCallback), request);
        }

        private void ResponseCallback(IAsyncResult ar)
        {
            Stream responseStream = new MemoryStream();
            try {
                
                HttpWebRequest request = ar.AsyncState as HttpWebRequest;
                WebResponse response = request.EndGetResponse(ar);
                responseStream = response.GetResponseStream();
                
            }
            catch (WebException e)
            {
                
            }
            catch (Exception e)
            {
                
            }
            finally
            {
                SetLicenseResponse(responseStream);
            }
        }
    }
}
