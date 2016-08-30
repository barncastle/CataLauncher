using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace CataLauncher
{
    class CookieWebClient : WebClient
    {
        private CookieContainer _cookies = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
                webRequest.CookieContainer = _cookies; //Restore cookies

            return request;
        }

        public void DownloadWebFileAsync(Uri address, string fileName, object userToken = null)
        {
            if (address.Host.ToLower() == "drive.google.com") //Check if it is a file hosted on Google Drive
                FormatGoogleDriveFile(ref address, userToken); //Handles the required cookies and redirects to make Google Drive files work

            if (userToken == null)
                this.DownloadFileAsync(address, fileName);
            else
                this.DownloadFileAsync(address, fileName, userToken);
        }

        #region Google Drive methods

        private void FormatGoogleDriveFile(ref Uri address, object userToken = null)
        {
            //Check if the original page is the default share page
            string _address = address.ToString();
            if(_address.ToLower().Contains("://drive.google.com/file/d"))
            {
                Match fileid = Regex.Match(_address, @":\/\/drive\.google\.com\/file\/d\/(.*)\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                if (fileid.Success)
                    address = new Uri("https://drive.google.com/uc?export=download&id=" + fileid.Groups[1].Value); //This is the force download page
                else
                    return;
            }

            //Send a request to the force download page
            //From this request we get the download link and the cookies required to authenticate
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.Method = "GET";
            request.CookieContainer = _cookies;
            request.AllowAutoRedirect = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string html = WebUtility.HtmlDecode(reader.ReadToEnd()); //Get the page html

                Match url = Regex.Match(html, @"(\/uc\?export=download[^""]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase); //Scrape the confirm button link
                if (url.Success)
                {
                    address = new Uri("https://drive.google.com" + url.Value); //Set the actual download link

                    if (userToken as PatchFileInfo != null)
                        ((PatchFileInfo)userToken).totalbytes = GetBytes(html); //Store a rough filesize to calculate the progress %
                }
            }
        }

        /// <summary>
        /// Caculate an estimated filesize from the html page
        /// <para>Google has blocked the Content-Length header</para>
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private long GetBytes(string html)
        {
            Match size = Regex.Match(html, @"\((\d+)(M|G|MB|GB)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase); //Scrape on-screen file size
            if (!size.Success || size.Groups.Count != 3)
                return -1;

            int amount = -1;
            if(int.TryParse(size.Groups[1].Value, out amount))
            {
                switch (size.Groups[2].Value.ToUpper())
                {
                    case "M":
                    case "MB":
                        return amount * 1024 * 1024; //Mb to Bytes
                    case "G":
                    case "GB":
                        return amount * 1024 * 1024 * 1024; //Gb to Bytes
                }
            }
            
            return -1;
        }

        #endregion
    }
}
