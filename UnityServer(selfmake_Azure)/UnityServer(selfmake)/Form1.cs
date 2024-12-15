using Microsoft.VisualBasic.ApplicationServices;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace UnityServer_selfmake_
{

    public partial class Form1 : Form
    {
        //TESTING
        IPEndPoint UDPTest;
        // management attribute
        int playerNum = 0;
        int playerUdpPort = 10100;
        // dictionary dùng để quản lý kết nối bao gồm key(string) là tên người chơi, địa chỉ ip và cặp Value(TcpClient,IPEndPoint) gồm một kết nối tcp và IPEndpoint dung để gửi cái gói tin udp vì udp là giao thức không hướng kết nối
        public Dictionary<string, (TcpClient tcpClient, IPEndPoint udpEP)> users = new Dictionary<string, (TcpClient, IPEndPoint)>();
        public List<string> clients = new List<string>();
        public List<int?> indexWinners = new List<int?>();
        // connection attribuet
        private TcpListener tcpListener;
        private UdpClient udpListener;
        private int tcpPort = 10000;
        private int udpPort = 10100;
        StreamReader sr;
        StreamWriter sw;
        bool isRunning = false;
        enum networkSubheader
        {
            PASS,
            INITIAL,
            CURRENT,
            JOIN,
            SPEAK,
            WIN,
            EXIT
        }
        float goal = 42 + 1;// giá trị đích đến sẽ được quyết định bởi server
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        public string StripControlChars(string s)
        {
            return Regex.Replace(s, @"[^\x20-\x7F]", "");
        }// hàm bỏ ký tự
        private async Task StartServerAsync()
        {
            tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            IPEndPoint udpEndpoint = new IPEndPoint(IPAddress.Any, udpPort);
            udpListener = new UdpClient(udpEndpoint);
            tcpListener.Start();
            richTextBox_notification.Text += $"Server started on port {tcpPort}\n";
            // Start listening for TCP clients
            ListenForTcpClientsAsync();
            // Start listening for UDP data (position updates, etc.)
            await ListenForUdpDataAsync();

        }// hàm chạy server (binding port và cấp phát dữ liệu)
        private async Task ListenForTcpClientsAsync()
        {
            while (true)
            {
                try
                {
                    TcpClient Client = new TcpClient();
                    Client = await tcpListener.AcceptTcpClientAsync();
                    if (Client.Connected)
                    {
                        richTextBox_notification.Text += "a client has connect to sever \n";
                        HandleTcpClientAsync(Client); // ko dc de await vi se ko acept dc client moi
                    }

                }
                catch (Exception exception)
                {
                    richTextBox_notification.Text += $"Error accepting TCP client: {exception.Message}\n";
                }
            }
        }// hàm lắng nghe các kết nối từ các client 
        private async Task HandleTcpClientAsync(TcpClient tcpClient)
        {
            try
            {
                StreamReader reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8);
                StreamWriter writer = new StreamWriter(tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
                {
                    string clientip = tcpClient.Client.RemoteEndPoint.ToString();
                    string[] add = clientip.Split(':');
                    clientip = add[0];
                    string? hello_message = await reader.ReadLineAsync();
                    // Kiểm tra định dạng gói tin hello_message
                    if (hello_message.Contains("|")) //"{username}|{ip}"
                    {
                        string[] parts = hello_message.Split('|');
                        string username = parts[0].Trim();
                        // Xác thực tên người dùng
                        string? clientId = AuthenticateUsername(username, clientip);
                        if (clientId != null)
                        {
                            richTextBox_notification.Text += "Client tcp connect successfully\n";
                            IPEndPoint udp_endPoint = await InitialUdpEP();// gán IPEndPoint cho udp_endPoint sau khi đã nhận  
                            richTextBox_notification.Text += "Client udp connect successfully\n";
                            // Lưu cả TcpClient và IPEndPoint vào dictionary
                            users.Add(clientId, (tcpClient, udp_endPoint));
                            clients.Add(username);
                            await writer.WriteLineAsync($"{networkSubheader.PASS.ToString()}|{CountPlayer() - 1}");
                            richTextBox_notification.Text += ($"Client kết nối thành công với ID: {clientId}:{add[1]}\n");
                            await SendClientListAsync(writer);
                            await NotifyAllClients(username, clientId, networkSubheader.JOIN);
                            textBox_players.Text = CountPlayer().ToString();
                            listBox_players.Items.Add(clientId);
                            while (tcpClient.Connected)
                            {
                                await ClientCheck(clientId, tcpClient);
                            }
                            richTextBox_notification.Text += $"{clientId} has been removed \n";
                            textBox_players.Text = CountPlayer().ToString();
                            listBox_players.Items.Remove(clientId);
                        }
                        else
                        {
                            await writer.WriteLineAsync("ALREADY");
                            tcpClient.Close();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex) { /*MessageBox.Show(ex.ToString());*/ }

        }// hàm xử lý các kết của các tcp client
        private string? AuthenticateUsername(string username, string ipEndpoint)
        {
            username = StripControlChars(username);
            if (string.IsNullOrWhiteSpace(username) ||
                    username.Contains(' ') ||
                    username.Contains('\t') ||
                    username.Contains('\0'))
            {
                return null;
            }
            // Tạo clientId theo định dạng {username}|{IPEndPoint}
            string clientId = $"{username}|{ipEndpoint}";

            // Kiểm tra nếu username đã tồn tại trong dictionary
            foreach (var key in users.Keys)
            {
                if (key.StartsWith(username + "|"))
                {

                    return null; // Trả về null nếu trùng username
                }
            }
            if (username.Contains('|')) return null;

            return clientId;
        }// hàm xác thực tên của client khi họ kết nối tới
        public async Task<IPEndPoint?> InitialUdpEP()
        {
            UdpReceiveResult result = new UdpReceiveResult();
            result = await udpListener.ReceiveAsync();
            string message = Encoding.ASCII.GetString(result.Buffer);
            message = StripControlChars(message);//sua format 
            Enum.TryParse(message, true, out networkSubheader command);// using true to ignore case
            //Debug.Log("enum command: " + command);

            switch (command)
            {
                case networkSubheader.INITIAL:
                    return result.RemoteEndPoint;

                // Add more cases for other message types
                default:
                    richTextBox_notification.Text += "return null\n";
                    return null;

            }

        }// hàm nghe các gói initial udp ( udp punch hole) để có thể lấy IPEndPoint
         // để trả gói tin về cho các client behind the nat 
        private async Task ListenForUdpDataAsync()
        {   // định dạng gói UDP là $"{
            while (isRunning)
            {
                UdpReceiveResult udpResult = await udpListener.ReceiveAsync();

                string message = Encoding.ASCII.GetString(udpResult.Buffer);
                string[] parts = message.Split('|');
                if (message == networkSubheader.INITIAL.ToString())
                {
                    //MessageBox.Show("no udpmessage");
                    // richTextBox_notification.Text += "INITIAL\n";
                }
                else
                {
                    CheckWinner(parts);
                    BroadcastPositionUpdate(message);
                }
            }
        } // lắng nghe các gói tin UDP được gửi đến port đã bind 
        private int? GoalCheck(int PlayerIndex, string pos)
        {
            //this.transform.position=  
            string[] parts = pos.Split(',');
            if (parts.Length == 3)
            {
                float x = float.Parse(parts[0].Trim());
                float y = float.Parse(parts[1].Trim());
                float z = float.Parse(parts[2].Trim());
                if (y >= goal)
                {
                    return PlayerIndex;
                }
            }
            else
            {
            }
            return null;
        }// hàm check xem vị trí của người chơi đã đến được đích hay chưa
        private void BroadcastPositionUpdate(string message)
        {
            string Broadcastmessage = message;
            byte[] data = Encoding.ASCII.GetBytes(Broadcastmessage);

            foreach (var entry in users)
            {
                if (entry.Value.udpEP != null /*&& !entry.Key.Contains(username)*/) // client has to listen to he same port!!
                {
                    udpListener.Send(data, data.Length, /*clientUdpEP*/ entry.Value.udpEP);
                }
            }
        }// hàm phát lại gói tin vị trí cho tất cả client
        private async Task NotifyAllClients(string username, string clientId, networkSubheader subheader)
        {

            string formattedMessage = $"{subheader}|{username}";
            foreach (var user in users)
            {
                if (user.Key != clientId)
                {
                    StreamWriter writer = new StreamWriter(user.Value.tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
                    await writer.WriteLineAsync(formattedMessage);
                }
            }
        } // hàm thông tin các gói tin tcp với các header tương ứng
        private async Task NotifyAllClients(string username, string clientId, networkSubheader subheader, string message)
        {

            string formattedMessage = $"{subheader}|{username}|{message}";
            foreach (var user in users)
            {
                if (user.Key != clientId)
                {
                    StreamWriter writer = new StreamWriter(user.Value.tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
                    await writer.WriteLineAsync(formattedMessage);
                }
            }
        }
        private async Task NotifyAllClients(int? playerIndex, networkSubheader subheader)
        {

            string formattedMessage = $"{subheader}|{playerIndex}";
            foreach (var user in users)
            {
                StreamWriter writer = new StreamWriter(user.Value.tcpClient.GetStream(), Encoding.UTF8) { AutoFlush = true };
                await writer.WriteLineAsync(formattedMessage);
            }
        }
        private async Task SendClientListAsync(StreamWriter writer)
        {
            var clientNames = new List<string>();
            string clientListMessage = $"{networkSubheader.CURRENT}|{string.Join("|", clients)}";
            await writer.WriteLineAsync(clientListMessage);
            // dellete disconnect from the list
        }// hàm gửi danh sách các client hiện có trong server
        private async Task ClientCheck(string clientid, TcpClient tcp)
        {
            StreamReader streamReader = new StreamReader(tcp.GetStream());
            string? mess = await streamReader.ReadLineAsync();
            //richTextBox_notification.Text += mess;
            string[] parts = mess.Split('|');

            if (StripControlChars(parts[0]) == networkSubheader.EXIT.ToString()) //process when client disconnect
            {
                richTextBox_notification.Text += $"{parts[1]} has disconnected\n";
                tcp.Dispose();
                tcp.Close();
                users.Remove(clientid);
                clients.Remove(parts[1]);
                await NotifyAllClients(parts[1], clientid, networkSubheader.EXIT);
            }
            else if (StripControlChars(parts[0]) == networkSubheader.SPEAK.ToString())
            {
                richTextBox_notification.Text += $"{parts[1]} has said: {parts[2]}\n";
                await NotifyAllClients(parts[1], clientid, networkSubheader.SPEAK, parts[2]);

            }


            //Console.WriteLine($"Client removed: {clientEndpoint}");
        }// hàm kiểm tra xem gói tin tcp client gửi là gì
        private async void CheckWinner(string[] parts)
        {
            int? index = GoalCheck(int.Parse(parts[0]), parts[1]);
            if (index != null)
            {
                for (int i = 0; i < indexWinners.Count; i++)
                {
                    if (index == indexWinners[i]) return;
                }
                await NotifyAllClients(index, networkSubheader.WIN);
                indexWinners.Add(index);
                richTextBox_notification.Text += "got winner\n";
            }
        }// hàm xử lý người chơi chiến thắng
        private int CountPlayer()
        {
            lock (users)
            {
                int num = users.Count();
                textBox_players.Text = num.ToString();
                return num;
            }

        }// hàm đếm số người chơi trong phòng
        private async void button_kick_Click(object sender, EventArgs e)
        {
            if (textBox_playertokick.Text == string.Empty)
            {
                return;
            }

            foreach (var client in users)
            {
                if (client.Key == textBox_playertokick.Text)
                {
                    string[] parts = client.Key.Split('|');
                    parts[0] = StripControlChars(parts[0]);
                    //await NotifyAllClients(parts[0], string.Empty, networkSubheader.EXIT);
                    //richTextBox_notification.Text += $"{parts[0]} has disconnected\n";
                    //client.Value.tcpClient.Dispose();
                    //client.Value.tcpClient.Close();
                    //users.Remove(client.Key);
                    await NotifyAllClients(parts[0], string.Empty, networkSubheader.EXIT);
                    richTextBox_notification.Text += $"{parts[0]} has disconnected\n";
                    client.Value.tcpClient.Dispose();
                    client.Value.tcpClient.Close();
                    users.Remove(client.Key);
                    clients.Remove(parts[0]);


                }
            }
            textBox_playertokick.Text = string.Empty;
            listBox_players.Items.Remove(listBox_players.SelectedItem);
            textBox_players.Text = CountPlayer().ToString();
        }//hàm kick người chơi khỏi server
        private void listBox_players_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_players.SelectedItem != null)
            {
                textBox_playertokick.Text = listBox_players.SelectedItem.ToString();
            }

        }
        private async void button_LISTEN_Click(object sender, EventArgs e)
        {
            isRunning = true;
            await StartServerAsync();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isRunning = false;
            tcpListener.Stop();
            udpListener.Close();
        }
    }
}