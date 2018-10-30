using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text;
using System.Net;

namespace CamLib
{
    public class Camera
    {

        [DllImport("user32", EntryPoint = "SendMessage")]
        public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        [DllImport("avicap32.dll", EntryPoint = "capCreateCaptureWindowA")]
        public static extern int capCreateCaptureWindowA(string lpszWindowName, int dwStyle, int X, int Y, int nWidth, int nHeight, int hwndParent, int nID);

        [DllImport("user32", EntryPoint = "OpenClipboard")]
        public static extern int OpenClipboard(int hWnd);

        [DllImport("user32", EntryPoint = "EmptyClipboard")]
        public static extern int EmptyClipboard();

        [DllImport("user32", EntryPoint = "CloseClipboard")]
        public static extern int CloseClipboard();


        public const int WM_USER = 1024;

        public const int WM_CAP_CONNECT = 1034;
        public const int WM_CAP_DISCONNECT = 1035;
        public const int WM_CAP_GET_FRAME = 1084;
        public const int WM_CAP_COPY = 1054;

        public const int WM_CAP_START = WM_USER;

        public const int WM_CAP_DLG_VIDEOFORMAT = WM_CAP_START + 41;
        public const int WM_CAP_DLG_VIDEOSOURCE = WM_CAP_START + 42;
        public const int WM_CAP_DLG_VIDEODISPLAY = WM_CAP_START + 43;
        public const int WM_CAP_GET_VIDEOFORMAT = WM_CAP_START + 44;
        public const int WM_CAP_SET_VIDEOFORMAT = WM_CAP_START + 45;
        public const int WM_CAP_DLG_VIDEOCOMPRESSION = WM_CAP_START + 46;
        public const int WM_CAP_SET_PREVIEW = WM_CAP_START + 50;

        //
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;

        public const int WM_CAP_SET_SCALE = WM_CAP_START + 53;
        public const int WM_CAP_SET_PREVIEWRATE = WM_CAP_START + 52;

        private int m_Width = 640;
        private int m_Height = 480;

        public void Shot(String uri)
        {
            
            Thread thread = new Thread(new ParameterizedThreadStart(iShot));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(uri);
            thread.Join();
        }

        internal void iShot(Object oUri)
        {

            Uri url = new Uri((String)oUri);

            //Form f = new Form();
            //f.Width = m_Width;
            //f.Height = m_Height;
            //f.Show();
            //f.Hide();


            Int32 h = (Int32)Process.GetCurrentProcess().MainWindowHandle;
            //h = f.Handle.ToInt32();

            Boolean found = false;
            for (Int32 i = 0; i <= 3; i++)
            {
                try
                {


                    Int32 mCapHwnd = capCreateCaptureWindowA("WebCap", 0, 0, 0, m_Width, m_Height, h, 0);
                    Application.DoEvents();

                    // Connect
                    Int32 tst = SendMessage(mCapHwnd, WM_CAP_CONNECT, i, 0);
                    Console.WriteLine("Trying device " + i.ToString());
                    if (tst > 0)
                    {

                        //SendMessage(mCapHwnd, WM_CAP_SET_PREVIEWRATE, 30, 0);
                        SendMessage(mCapHwnd, WM_CAP_SET_PREVIEW, 0, 0);

                        SendMessage(mCapHwnd, WM_CAP_GET_FRAME, 0, 0);

                        // copy the frame to the clipboard
                        SendMessage(mCapHwnd, WM_CAP_COPY, 0, 0);


                        // get from the clipboard
                        //System.Threading.Thread.Sleep(300);
                        IDataObject tempObj = Clipboard.GetDataObject();
                        Image tempImg = (System.Drawing.Bitmap)tempObj.GetData(System.Windows.Forms.DataFormats.Bitmap);

                        try
                        {
                            Clipboard.Clear();
                        }
                        catch { }

                        /*
                        * For some reason, the API is not resizing the video
                        * feed to the width and height provided when the video
                        * feed was started, so we must resize the image here
                        */
                        Image tempImg2 = tempImg.GetThumbnailImage(m_Width, m_Height, null, System.IntPtr.Zero);

                        Console.WriteLine("Sending image from device " + i.ToString());
                        SendPicture(url, tempImg2);
                        found = true;

                        Application.DoEvents();
                        SendMessage(mCapHwnd, WM_CAP_DISCONNECT, i, 0);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro capturando imagem da cam " + i + ": " + ex.Message);
                }

            }

            if (!found)
            {
#if DEBUG
                Image tempImg2 = BlackImage(m_Width, m_Height);

                SendPicture(url, tempImg2);

#else
                Console.WriteLine("No camera device found!");
#endif

            }

            Console.WriteLine("Finishing process...");
            try
            {
                System.Threading.Thread.CurrentThread.Abort();
            }
            catch { }


            Process.GetCurrentProcess().Kill();
        }

        private void SendPicture(Uri uri, Image image)
        {
            
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, ImageFormat.Jpeg);
                byte[] data = m.ToArray();

                if (data.Length > 0)
                {

                    StringBuilder putData = new StringBuilder();

                    putData.AppendLine("--------JPG_INIT------");
                    putData.AppendLine(Convert.ToBase64String(data));
                    putData.AppendLine("------JPG_END--------");

                    try
                    {
                        WebClient client = new WebClient();
                        client.UploadData(uri, "PUT", Encoding.UTF8.GetBytes(putData.ToString()));
                    }
                    catch { }
                }

            }
        }

        private Bitmap BlackImage(int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics graph = Graphics.FromImage(bmp))
            {
                Rectangle ImageSize = new Rectangle(0, 0, width, height);
                graph.FillRectangle(Brushes.Black, ImageSize);
            }
            return bmp;
        }

    }
}
