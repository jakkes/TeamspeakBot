using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using TeamspeakBotv2.Models;
using System.Text.RegularExpressions;

namespace TeamspeakBotv2.Core
{
    public class Channel : IDisposable
    {
        public event EventHandler Disposed;
        private EventWaitHandle ErrorLineReceived = new EventWaitHandle(false, EventResetMode.AutoReset);
        public string Name { get; private set; }
        public int Id { get; set; }
        private string DefaultChannel;
        private IPEndPoint Host;
        private int Port;
        private string Username;
        private string Password;

        private Socket connection;


        public Channel(string channel, string defaultchannel, IPEndPoint host, string username, string password)
        {
            Name = channel; DefaultChannel = defaultchannel;
            Host = host; Username = username; Password = password;
            connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                connection.Connect(Host);
            } catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            Login();
        }
        
        private void Login()
        {
            Send(string.Format("login {0} {1}", Username, Password));
            ErrorLineReceived.WaitOne();
        }

        private WhoAmIModel WhoAmI()
        {
            
        }

        private void Send(string message)
        {
            connection.SendTo(Encoding.ASCII.GetBytes(message), Host);
        }

        private void Read()
        {
            if (!connection.Connected)
            {
                Dispose();
                return;
            }

            byte[] buffer = new byte[4096];
            string msg = string.Empty;
            while(connection.Available != 0)
            {
                int bytes = connection.Receive(buffer);
                msg += Encoding.ASCII.GetString(buffer, 0, bytes);
            }
            string[] msgs = msg.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < msgs.Length; i++)
                HandleReply(msgs[i]);
        }
        private void HandleReply(string line)
        {
            if (line.StartsWith("notifytextmessage"))
                HandleMessage(line);
            else if (line.StartsWith("notifyclientmoved"))
                HandleClientMoved(line);
            else if (line.StartsWith("notifyclientleftview"))
                HandleClientLeftView(line);
            else if (line.StartsWith("notifycliententerview"))
                HandleClientEnterView(line);
            else if (line.StartsWith("error"))
                HandleErrorMessage(line);
        }

        private void HandleErrorMessage(string line)
        {
            var match = RegPatterns.ErrorLine.Match(line);
            if (match.Success)
            {
                var error = new ErrorModel(match);
                if (error.Id != 0)
                    throw new Exception(error.Message);
                ErrorLineReceived.Set();
            }
        }
        private void HandleClientEnterView(string line)
        {
            Match m = RegPatterns.EnterView.Match(line);
            if (m.Success)
            {
                var model = new ClientEnteredViewModel(m);
            }
        }

        private void HandleClientLeftView(string line)
        {
            throw new NotImplementedException();
        }

        private void HandleClientMoved(string line)
        {
            throw new NotImplementedException();
        }

        private void HandleMessage(string line)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            connection.Dispose();
            if (Disposed != null)
                Disposed(this, new EventArgs());
        }
    }
}
