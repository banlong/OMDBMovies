using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace omdbCommon {

    //PROVIDE WEB COMMUNICATION SERVICE
    public class HttpServices {

        private Dictionary<string, IMovieParser> parsers;

        public HttpServices() {
            parsers = new Dictionary<string, IMovieParser>();
            parsers.Add("xml", new XmlMovieParser());
            parsers.Add("json", new JSONMovieParser());
        }

        //GET MOVIES INFORMATION FROM REMOTE SITE
        public List<Movie> GetMoviesInfo(string title, string protocol){
            string imdbApiStr = "http://www.omdbapi.com/?s={0}&r={1}";
            string searchTitle = title.ToLower().Replace(" ", "%20");

            string queryURL = String.Format(imdbApiStr, searchTitle, protocol.ToLower());


            //Get response either in XML/JSON
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(queryURL);
            httpRequest.Timeout = 10000;     // 10 secs
            HttpWebResponse webResponse = (HttpWebResponse)httpRequest.GetResponse();

            //Parse data
            return parsers[protocol.ToLower()].Parse(webResponse);

        }

        //SAVE REMOTE IMAGE LOCALLY
        //In this project: omdbMovies\csx\Debug\roles\omdbWorker\approot
        public void SaveImage(string file_name, string url) {
            byte[] content;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse response = request.GetResponse();

            Stream stream = response.GetResponseStream();

            using (BinaryReader br = new BinaryReader(stream)){
                content = br.ReadBytes(500000);
                br.Close();
            }
            response.Close();

            FileStream fs = new FileStream(file_name, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            try {
                bw.Write(content);
            }finally {
                fs.Close();
                bw.Close();
            }
        }

        //CREATE MEMORYSTREAM OF THE REMOTE IMAGE(base on URL)
        public MemoryStream GetImageStream(string url) {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            MemoryStream memStream = new MemoryStream();
            //Read Response Stream into a Memory Stream
            try {
                byte[] block = new byte[0x1000]; // blocks of 4K.
                while (true){
                    int bytesRead = stream.Read(block, 0, block.Length);
                    if (bytesRead == 0) return memStream;
                    memStream.Write(block, 0, bytesRead);
                }
            } finally {
                stream.Close();
            }
        }

    }
}
