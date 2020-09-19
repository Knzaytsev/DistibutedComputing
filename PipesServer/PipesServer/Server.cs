using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using PipesServer;

namespace Pipes
{
    public partial class frmMain : Form
    {
        private Int32 PipeHandle;                                                       // дескриптор канала
        private string PipeName = "\\\\" + Dns.GetHostName() + "\\pipe\\ServerPipe";    // имя канала, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t;                                                               // поток для обслуживания канала
        private bool _continue = true;                                                  // флаг, указывающий продолжается ли работа с каналом
        private Dictionary<string, Client> clients = new Dictionary<string, Client>();

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();

            // создание именованного канала
            PipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe", DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);

            // вывод имени канала в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + PipeName;

            // создание потока, отвечающего за работу с каналом
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        private void ReceiveMessage()
        {
            string msg = "";            // прочитанное сообщение
            uint realBytesReaded = 0;   // количество реально прочитанных из канала байтов

            // входим в бесконечный цикл работы с каналом
            while (_continue)
            {
                if (DIS.Import.ConnectNamedPipe(PipeHandle, 0))
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(PipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff
                    msg = Encoding.Unicode.GetString(buff);                                 // выполняем преобразование байтов в последовательность символов
                    msg = msg.Trim('\0');
                    if (msg.Contains("<LOGIN>"))
                    {
                        msg = msg.Replace("<LOGIN>", "");
                        var client = new Client();
                        client.PipeHandle = PipeHandle;
                        if (!clients.ContainsKey(msg))
                            clients.Add(msg, client);
                        msg = "User " + msg + " has joined!";
                    }

                    if (msg.Contains("<DISCONNECT>"))
                    {
                        //uint realBytesWritten = 0;
                        msg = msg.Replace("<DISCONNECT>", "");
                        /*Int32 clientPipeHandle = DIS.Import.CreateFile("\\\\.\\pipe\\ServerPipe" + msg, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
                        //Int32 clientPipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe" + client.Key, DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);
                        DIS.Import.WriteFile(clientPipeHandle, buff, Convert.ToUInt32(buff.Length), ref realBytesWritten, 0);
                        DIS.Import.DisconnectNamedPipe(clientPipeHandle);
                        DIS.Import.CloseHandle(clientPipeHandle);*/
                        SendMessage(msg, "");
                        if (clients.ContainsKey(msg))
                            clients.Remove(msg);
                        msg = "User " + msg + " has disconnected!";
                    }

                    rtbMessages.Invoke((MethodInvoker)delegate
                    {
                        if (msg != "")
                            rtbMessages.Text += "\n >> " + msg;                             // выводим полученное сообщение на форму
                    });

                    SendMessages(msg);

                    DIS.Import.DisconnectNamedPipe(PipeHandle);                             // отключаемся от канала клиента 
                    Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                }
            }
        }

        private void SendMessages(string message)
        {
            /*byte[] buff = new byte[1024];
            uint realBytesWritten = 0;
            buff = Encoding.Unicode.GetBytes(message);*/
            foreach(var client in clients)
            {
                SendMessage(client.Key, message);
                /*Int32 clientPipeHandle = DIS.Import.CreateFile("\\\\.\\pipe\\ServerPipe" + client.Key, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
                //Int32 clientPipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe" + client.Key, DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);
                DIS.Import.WriteFile(clientPipeHandle, buff, Convert.ToUInt32(buff.Length), ref realBytesWritten, 0);
                DIS.Import.DisconnectNamedPipe(clientPipeHandle);
                DIS.Import.CloseHandle(clientPipeHandle);*/
            }
        }

        private void SendMessage(string username, string message)
        {
            uint realBytesWritten = 0;
            byte[] buff = Encoding.Unicode.GetBytes(message);
            Int32 clientPipeHandle = DIS.Import.CreateFile("\\\\.\\pipe\\ServerPipe" + username, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(clientPipeHandle, buff, Convert.ToUInt32(buff.Length), ref realBytesWritten, 0);
            DIS.Import.DisconnectNamedPipe(clientPipeHandle);
            DIS.Import.CloseHandle(clientPipeHandle);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с каналом завершена

            if (t != null)
                t.Abort();          // завершаем поток
            
            if (PipeHandle != -1)
                DIS.Import.CloseHandle(PipeHandle);     // закрываем дескриптор канала
        }
    }
}