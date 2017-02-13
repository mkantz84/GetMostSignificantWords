using GetUrlText.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace GetUrlText.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult GetUrlText(string URL)
        {
            List<Data> modelData = new List<Data>();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            string data = GetDocumentContents(res);
            var wordsList = GetCleanData(data);
            foreach (var key in wordsList)
            {
                modelData.Add(new Data() { Name = key.Key, Amount = key.Value });
            }
            //modelData.dict = wordsList.ToDictionary(t => t.Key, t => t.Value);
            return View(modelData);
        }

        private IEnumerable<KeyValuePair<string,int>> GetCleanData(string data)
        {
            Dictionary<string, int> words = new Dictionary<string, int>();
            int findBody = data.IndexOf("body");
            data = CleanData(data, findBody);
            int startText = data.IndexOf(">", findBody);
            int endText = data.IndexOf("<", startText);
            //string text = "";
            while (endText >= 0)
            {
                while (endText - startText == 1)
                {
                    startText = data.IndexOf(">", endText);
                    endText = data.IndexOf("<", startText);
                }
                if (endText >= 0)
                {
                    UpdateWords(data, words, startText, endText);
                    startText = data.IndexOf(">", endText);
                    endText = data.IndexOf("<", startText);
                }
            }
            var sortWords = from pair in words
                            orderby pair.Value descending
                            select pair;
            var wordsList = sortWords.ToList().Take(20);

            return wordsList;
        }

        private void UpdateWords(string data, Dictionary<string, int> words, int startText, int endText)
        {
            string text = data.Substring(startText + 1, endText - startText - 1);
            string[] textArray = text.Split(' ');
            for (int i = 0; i < textArray.Length; i++)
            {
                byte[] asciiBytes = Encoding.ASCII.GetBytes(textArray[i]);
                bool isLegal = CheckBytes(asciiBytes);
                if (isLegal)
                {
                    if (words.ContainsKey(textArray[i]))
                    {
                        words[textArray[i]]++;
                    }
                    else
                    {
                        words[textArray[i]] = 1;
                    }
                }
            }
        }

        private string CleanData(string data, int findBody)
        {
            data = Clean(data, "<script>", "</script>", findBody);
            data = Clean(data, "<style>", "</style>", findBody);
            data = data.Replace("\n", string.Empty);
            data = data.Replace("\t", string.Empty);
            data = data.Replace("\r", string.Empty);
            return data;
        }

        private bool CheckBytes(byte[] asciiBytes)
        {
            if (asciiBytes.Length == 0)
            {
                return false;
            }
            foreach (byte b in asciiBytes)
            {
                //*** the ascii encoding encoding turns hebrew letters to '?', and the byte is '63' 
                if (b != 63)
                {
                    return false;
                }
            }
            return true;
        }

        private string Clean(string data, string toFindFirst, string toFindSecond, int start)
        {
            int firstTrimIndex = data.IndexOf(toFindFirst, start);
            while (firstTrimIndex >= 0)
            {
                int secondTrimIndex = data.IndexOf(toFindSecond, firstTrimIndex);
                data = Trim(data, firstTrimIndex, secondTrimIndex, toFindSecond.Length);
                firstTrimIndex = data.IndexOf(toFindFirst, start);
            }

            return data;
        }

        private string Trim(string data, int firstTrimIndex, int secondTrimIndex, int length)
        {
            return data.Remove(firstTrimIndex, secondTrimIndex - firstTrimIndex + length);
        }

        private string GetDocumentContents(HttpWebResponse Request)
        {
            string documentContents;
            using (Stream receiveStream = Request.GetResponseStream())
            {
                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                {
                    documentContents = readStream.ReadToEnd();
                }
            }
            return documentContents;
        }
    }
}