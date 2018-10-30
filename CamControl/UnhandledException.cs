using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

public class UnhandledException
{
    static public void WriteEvent(object sender, Exception ex)
    {
        WriteEvent(sender, ex, true);
    }


    static public String WriteEvent(object sender, Exception ex, Boolean endApplication)
    {
        StringBuilder texto = new StringBuilder();
        try
        {
            Process myProc = Process.GetCurrentProcess();
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();


            texto.AppendLine("----------------------------------------------");
            texto.AppendLine("Exe: " + asm.Location);

            try
            {
                texto.Append("Arguments: ");
                foreach (String arg in Environment.GetCommandLineArgs())
                {
                    texto.Append(arg + " ");
                }
                texto.AppendLine("");
            }
            catch { }

            texto.AppendLine("PID: " + myProc.Id);
            texto.AppendLine("Data: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"));
            texto.AppendLine("Message: " + ex.Message);
            texto.AppendLine("StackTrace: " + ex.StackTrace);

            if (ex.InnerException != null)
            {
                texto.AppendLine("InnerException Message: " + ex.InnerException.Message);
                texto.AppendLine("InnerException StackTrace: " + ex.InnerException.StackTrace);
            }

            texto.AppendLine(" ");

            BinaryWriter writer = new BinaryWriter(File.Open(Environment.CurrentDirectory + "\\Exception_" + DateTime.Now.ToString("yyyyMMdd") + ".txt", FileMode.Append));
            writer.Write(Encoding.UTF8.GetBytes(texto.ToString()));
            writer.Flush();
            writer.Close();
            //texto = null;

        }
        catch { }
        finally
        {
            if (endApplication)
                Process.GetCurrentProcess().Kill();
        }
        return texto.ToString();
    }


    static public String WriteEvent(object sender, UnhandledExceptionEventArgs e)
    {
        return WriteEvent(sender, e, true);
    }

    static public String WriteEvent(object sender, UnhandledExceptionEventArgs e, Boolean endApplication)
    {

        try
        {
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine("UnhandledException: " + ex.Message + ex.StackTrace);

            return WriteEvent(sender, ex, endApplication);

        }
        catch { return ""; }
        finally
        {
            if (endApplication)
                Process.GetCurrentProcess().Kill();
        }
    }
}