using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace Pipes
{
    public partial class frmMain : Form
    {
        private Int32 PipeHandle = -1;   // дескриптор канала
        private Thread t;
        private bool _continue = true;

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            this.Text += "     " + Dns.GetHostName();   // выводим имя текущей машины в заголовок формы
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage(tbLogin.Text + ": " + tbMessage.Text);
            /*uint BytesWritten = 0;  // количество реально записанных в канал байт
            byte[] buff = Encoding.Unicode.GetBytes(tbLogin.Text + ": " + tbMessage.Text);
            Int32 tempPipeHandle = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(tempPipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(tempPipeHandle);*/
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            t = new Thread(ReadMessages);
            t.Start();

            /*uint BytesWritten = 0;  // количество реально записанных в канал байт
            byte[] buff = Encoding.Unicode.GetBytes(tbLogin.Text + @"<LOGIN>");

            Int32 tempPipeHandle = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(tempPipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);
            DIS.Import.CloseHandle(tempPipeHandle);*/
            SendMessage(tbLogin.Text + @"<LOGIN>");

            // открываем именованный канал, имя которого указано в поле tbPipe
            PipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe" + tbLogin.Text, DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);

            btnLogin.Enabled = false;
        }

        private void SendMessage(string message)
        {
            uint BytesWritten = 0;  // количество реально записанных в канал байт
            byte[] buff = Encoding.Unicode.GetBytes(message);
            Int32 tempPipeHandle = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(tempPipeHandle, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(tempPipeHandle);
        }

        private void ReadMessages(){
            while (_continue)
            {
                if (DIS.Import.ConnectNamedPipe(PipeHandle, 0))
                {
                    byte[] buff = new byte[1024];
                    uint realBytesReaded = 0;
                    DIS.Import.FlushFileBuffers(PipeHandle);
                    DIS.Import.ReadFile(PipeHandle, buff, Convert.ToUInt32(buff.Length), ref realBytesReaded, 0);
                    string msg = Encoding.Unicode.GetString(buff);
                    rtbMessages.Invoke((MethodInvoker)delegate
                    {
                        if (msg != "")
                            rtbMessages.Text += "\n" + msg;
                    });
                    DIS.Import.DisconnectNamedPipe(PipeHandle);
                }
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;

            if (t != null)
                t.Abort();
            
            if (PipeHandle != -1)
            {
                SendMessage(tbLogin.Text + "<DISCONNECT>");
                DIS.Import.CloseHandle(PipeHandle);
            }
        }
    }
}
