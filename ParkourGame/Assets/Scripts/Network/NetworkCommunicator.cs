using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using UnityEngine.UIElements;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static UnityEngine.Networking.UnityWebRequest;
using static UnityEngine.GraphicsBuffer;



public class NetworkCommunicator : MonoBehaviour
{
    // SERVER'S CONNECTION ATTRIBUTE

    // server address
    public int tcpPort = 10000;  // destination port tcp để kết nối với server
    public int udpPort = 10100;  // destination port udp để kết nối với server
    public TMP_InputField InputIP;
    public TMP_InputField InputName;
    private string ip = string.Empty;
    public IPAddress serverIP;
    // Server IP, change as needed
                                // connection attribute                                                // public IPAddress machineIP= GetLocalIPAddress();
    public IPEndPoint TCPendPoint;
    public IPEndPoint UDPendPoint;
    public int timeout = 5000;  // Connection timeout in milliseconds


    // CLIENT'S CONNECTION ATTRIBUTE

    // connections
    public TcpClient tcpClient;         // dùng để thiết lập kết nối tcp
    public UdpClient udpClient_Send;    // dùng để gửi và nhận cái gói tin udp giữa server và người chơi
    //public UdpClient udpClient_Recv;
    public IPEndPoint IPEP;
             
    private int udpListenPort = 10100; // port dùng để nghe những gói tin udp 
                                   //Các Stream hỗ trợ(optional)
    public StreamReader sr;
    public StreamWriter sw;

    public BinaryWriter bw;
    public BinaryReader br;

    private NetworkStream stream;

    // variables
    string server_message;
    private bool isconnect = false;
    private bool isActive = false;
    private bool loadCurrent = false;
    // sub header for connection
    enum networkSubheader // các header phụ để xác định loại gói tin cần xử lý
    {
        PASS,
        INITIAL,
        CURRENT,
        JOIN,
        SPEAK,
        WIN,
        EXIT
    }
    // các attribute dùng để render object người chơi
    private string username; 
    private int playerid;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    //public Transform lastTransform;
    [SerializeField] private Transform playerTransform;  
    [SerializeField] private Transform orientationTransform; 
    //public spawn instance
    [SerializeField] public GameObject Player;
    [SerializeField] public GameObject Camera;
    //public NotificationCanvas instance
    [SerializeField] public GameObject NotificationCanvas;
    [SerializeField] public GameObject NotificationDisconnected;
    [SerializeField] public GameObject NotificationKicked;
    [SerializeField] public GameObject NotificationInvalidName;
    //public ChatBox instance
    private GameObject ChatBox; 
    public ChatBox chatBox;
    private string[] gotMessage = null ;
    // spawn list to spawn
    private List<string> playersToSpawn = new List<string>();
    public Spawning spawner;
    public int indexPlayer;
    public int indexWinner=-1;
    private bool disconnectflag = false;
    string del_target = string.Empty;
    //public game event
    public event EventHandler<OnReceiveServerMessageEventArgs> OnReceiveServerMessage;
    public class OnReceiveServerMessageEventArgs : EventArgs
    {
        public string message;
    }
    /////////////////////////////////////////////////////////////////////////////////////////

    private void Awake()
    {
        serverIP = IPAddress.Parse(InputIP.text);
        username = InputName.text;
        Player.name = "You";
    }
    private async void Start()
    {
        await ConnectServer();
        if (isconnect)
        {
            Instantiate(Camera);
            Instantiate(Player);
            Camera.SetActive(true);
            Player.SetActive(true);
            Debug.Log("Spawn player");
        }
        if (playerTransform != null)
        {
            lastPosition = playerTransform.position;
            lastRotation = orientationTransform.rotation;
        }
        Debug.Log("done start");
        // Start listening for server messages in a separate task
        _ = Task.Run(() => ListenTcpFromServer());
        ListeningSyncData();
    }

    void DemoSpawn(string Pname)
    {
        if (Pname != username)
        {
            int index = spawner.SpawnPrefab(Pname) - 1;
            TextMeshProUGUI TMP = spawner.spawnedObjects[index].GetComponentInChildren<Canvas>().GetComponentInChildren<TextMeshProUGUI>();
            TMP.text = Pname;
            Debug.Log("Demo spawn: " + spawner.spawnedObjects[index] + " index = " + index);
        }
        else
        {
            indexPlayer = spawner.AddPlayer() - 1;
            Debug.Log("Demo spawn: " + spawner.spawnedObjects[indexPlayer] + " index = " + indexPlayer);
        }

    } // hàm spawn ra các object người chơi khi có người kết nối vào server

    private Vector3 Move(string pos)
    {
        string[] parts = pos.Split(',');

        if (parts.Length == 3)
        {
            float x = float.Parse(parts[0].Trim());
            float y = float.Parse(parts[1].Trim());
            float z = float.Parse(parts[2].Trim());
            Vector3 newPosition = new Vector3(x, y, z);
            return newPosition;
        }
        else
        {
            Debug.LogError("Invalid position string format!");
            return Vector3.zero;
        }

    } // hàm để hiển thị ví trí mới của các người chơi khác trong server

    async Task ConnectServer()
    {
        tcpClient = new TcpClient();
        udpClient_Send = new UdpClient();
        TCPendPoint = new IPEndPoint(serverIP, tcpPort);
        UDPendPoint = new IPEndPoint(serverIP, udpPort);
        // Set a connection timeout
        tcpClient.SendTimeout = timeout;
        tcpClient.ReceiveTimeout = timeout;
        try
        {
            // Attempt to connect
            //await tcpClient.ConnectAsync(serverIP, tcpPort);
            tcpClient.Connect(serverIP, tcpPort);
            // Enable auto flush for immediate sending
            sr = new StreamReader(tcpClient.GetStream());
            sw = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
            Debug.Log("Connected to server!");
            // Send authorization message
            AuthorizedProcess();
        }
        catch (SocketException e)
        {
            Debug.LogError("SocketException: " + e.Message);
        }
        catch (IOException e)
        {
            Debug.LogError("IOException: " + e.Message);
        }
    }// hàm thiết lập kết nối và tạo những 

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
    // Start is called before the first frame update
    public string StripControlChars(string s)
    {
        return Regex.Replace(s, @"[^\x20-\x7F]", "");
    }// hàm lọc lại các ký tự đặc biệt trong string

    private async Task ListenTcpFromServer()
    {
        try
        {
            while (isconnect)
            {
                server_message = await sr.ReadLineAsync();
                if (server_message != null)
                {
                    server_message = StripControlChars(server_message);
                    ProcessServerMessage(server_message);
                }
            }
        }
        catch (IOException e)
        {
            Debug.LogError("IOException: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
    }// hàm lắng nghe các thông báo từ server gửi đến (TCP)

    private void ProcessServerMessage(string message)
    {
        string[] parts = message.Split('|');
        if (parts.Length > 0)
        {
            string commandStr = parts[0].Trim();
            if (Enum.TryParse(commandStr, true, out networkSubheader command)) // using true to ignore case
            {
                switch (command)
                {
                    case networkSubheader.CURRENT:
                        Debug.Log("handle current : "+ parts[1]);
                        HandleCurrentClient(parts);
                        break;

                    case networkSubheader.JOIN:
                        Debug.Log("handle joined : "+ parts[1]);
                        HandleClientJoined(parts);
                        break;

                    case networkSubheader.WIN:
                        Debug.Log("handle win : " + parts[1]);
                        HandleClientWin(parts);
                        break;

                    case networkSubheader.SPEAK:
                        Debug.Log("handle speak : " + parts[1]);
                        HandleClientSpeak(parts);
                        break;

                    case networkSubheader.EXIT:
                        Debug.Log("player index is : " + indexPlayer);
                        Debug.Log("handle exit : " + parts[1]);
                        HandleExitClient(parts);
                        break;
                 
                    default:
                        Debug.LogError("Unhandled command: " + command);
                        break;
                }
            }
            else
            {
                Debug.LogError("Invalid command received: " + commandStr);
            }
        }
        else
        {
            Debug.LogError("Message format incorrect: " + message);
        }
    }// hàm xử lý các gói tin thông báo (TCP)

    private void HandleCurrentClient(string[] parts)
    {
        lock (playersToSpawn)
        {
            for (int i = 1; i < parts.Length; i++)
            {
                playersToSpawn.Add(parts[i]);
            }
        }
    }// hàm xử lý các gói tin thông báo có flag = (CURRENT)

    private void HandleClientJoined(string[] parts)
    {
        lock (playersToSpawn)
        {
            playersToSpawn.Add(parts[1]);
        }

    }// hàm xử lý các gói tin thông báo có flag = (JOIN)

    private void HandleClientWin(string[] parts)
    {

         indexWinner = int.Parse(parts[1]);
        

    }

    private void HandleExitClient(string[] parts)
    {
        del_target = parts[1];

    }// hàm xử lý các gói tin thông báo có flag = (EXIT)

    private void HandleClientSpeak(string[] parts)
    {
        Debug.Log($"{parts[1]} has said {parts[2]}");
        gotMessage = parts;
        // hien thi len chat box trong  thread chinh
    }// hàm xử lý các gói tin thông báo có flag = (SPEAK)
    private async void Update()
    {

        if (isconnect)
        {
            if (playersToSpawn.Count > 0)
            {
                foreach (var pname in playersToSpawn)
                {
                    DemoSpawn(pname);
                    chatBox.ShowMessage($"{pname} is in the room");
                }
                playersToSpawn.Clear();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Disconnect();
                // return to menu game!!!
            }
            if (del_target != string.Empty) // xóa object người chơi khi người chơi đó ngắt kết nối
            {
                if(del_target == username)
                {
                    ShowKicked();
                }
                spawner.DeleteObjectByName(del_target);
                indexPlayer = spawner.GetPlayerIndex();
                Debug.Log("player index after del is : " + indexPlayer.ToString());
                chatBox.ShowMessage($"{del_target} is disconnected");
                del_target = string.Empty;
            }
            if (gotMessage != null)
            {
                chatBox.ShowMessage($"{gotMessage[1]} : {gotMessage[2]}");
                gotMessage = null;
            }
            if (!tcpClient.Connected)
            {
                Debug.Log("Disconnected from server.");
                isconnect = false;
               // Debug.Log("notice canvas");
                //NotificationCanvas.SetActive(true);
                //Text txt = NotificationCanvas.GetComponent<Text>();
            }
            if (indexWinner >= 0)
            {
                spawner.SetWinner(indexWinner);
                Debug.Log("Winner is " + indexWinner.ToString());
                chatBox.ShowMessage($"{spawner.spawnedObjects[indexWinner].name} WINN!!!");
                indexWinner = -1;
            }
        }
        else
        {
            if (!disconnectflag)
            {
                ShowDisconnected();
            }
        }

    }// hàm kiểm tra logic game 60 lần lặp mỗi giây 

    private async void FixedUpdate() 
    {
        if (playerTransform != null&& isconnect)
        {
            if (playerTransform.position != lastPosition)
            {
                lastPosition = playerTransform.position;
                SendTransformData(); // GOING TO MAKE
            }
        }
    }//hàm gừi các gói tin vị trí của người chơi với tốc độ 30 lần lặp mỗi giây
    void AuthorizedProcess()
    {
        string hello_message = $"{username}|{null}";  // Replace with actual authorization info if needed
        try
        {
            sw.WriteLine(hello_message);
            InitialUdpMess();
            Debug.Log("Authorization message sent.");
            server_message = sr.ReadLine();
            server_message = StripControlChars(server_message);
            string[] part = server_message.Split('|');
            if (Enum.TryParse(part[0], true, out networkSubheader command)&& command==networkSubheader.PASS)
            {
                Debug.Log("Authorizatize passed with player id is" + part[1]);
                indexPlayer = int.Parse(part[1]);
                isconnect = true;
                OnReceiveServerMessage?.Invoke(this, new OnReceiveServerMessageEventArgs { message = "PASS" });
            }
            else
            {
                Debug.Log("This name already been chosen!");
                OnReceiveServerMessage?.Invoke(this, new OnReceiveServerMessageEventArgs { message = "ALREADY" });
                ShowInvalid();
            }

        }
        catch (IOException e)
        {
            Debug.LogError("Failed to send authorization message: " + e.Message);
        }
    }// hàm xử lý xác thực tên hợp hay không(không được trùng với những người chơi đã có trong server)

    private void InitialUdpMess()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(networkSubheader.INITIAL.ToString());
        for (int i = 0; i < 20; i++)
        {
            udpClient_Send.Send(bytes, bytes.Length, UDPendPoint);
        }
        for(int i=0; i<5000000;i++)
        {
            i++;
            i--;
        }
    }// gửi các gói tin udp punch hole dùng để kết nối những người dùng behind NAT

    void SendTransformData()
    {
        string message = $"{indexPlayer}|{playerTransform.position.x},{playerTransform.position.y},{playerTransform.position.z}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);// need to be configured
        udpClient_Send.Send(bytes, bytes.Length, UDPendPoint);
    }// hàm gửi các gói tin (udp) để cập nhật ví trí người chơi hiện tại cho server

    private async void ListeningSyncData()
    {
        while (isconnect)
        { 
            try
            {
                UdpReceiveResult result = await udpClient_Send.ReceiveAsync();
                string transformData = Encoding.UTF8.GetString(result.Buffer);
                SyncTransformData(result.Buffer);

            }
            catch (SocketException e)
            {
                Debug.LogError("UDP Socket Exception: " + e.Message);
                break;
            }
        }
    }// hàm lắng nghe các gói tin (udp) cập nhật vị trí của người chơi khác

    private async void SyncTransformData(byte[] buffer)
    {
        string message = Encoding.UTF8.GetString(buffer);
        Debug.Log(message);
        string[] part = message.Split('|');
        int index;
        //Debug.Log(message);
        if(int.TryParse(part[0].Trim(),out index))
        {
            if (index != indexPlayer)
            {
                Vector3 targetPosition = Move(part[1]);
                StartSmoothMovement(spawner.spawnedObjects[index], targetPosition);
            }
        }
    }// hàm xử lý các gói tin (udp) vị trí của các người chơi khác 

    private void StartSmoothMovement(GameObject playerObject, Vector3 targetPosition)
    {
        StartCoroutine(SmoothMoveCoroutine(playerObject, targetPosition, 0.075f)); // Adjust duration as needed
    } // hàm làm mượt độ di chuyển của các nhân vật

    private IEnumerator SmoothMoveCoroutine(GameObject playerObject, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = playerObject.transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            playerObject.transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            yield return null;
        }
        // Ensure exact final position
        playerObject.transform.position = targetPosition;
    } // hàm thực thi làm mượt di chuyển

    public async void ClientSpeak(string message)
    {
        if (isconnect)
        {
            string format = $"{networkSubheader.SPEAK}|{username}|{message}";
            await sw.WriteLineAsync(format);
        }
        
    }//hàm gửi tin nhắn của người chơi đến server

    private void OnApplicationQuit()
    {
        Disconnect();
        if (tcpClient != null)
        {
            sr?.Close();
            sw?.Close();
            stream?.Close();
            tcpClient?.Close();
            Debug.Log("Disconnected from server.");
        }
    }// hàm sẽ được gọi khi người chơi thoát game
    private void ShowDisconnected()
    {
        NotificationCanvas.SetActive(true);
        NotificationDisconnected.SetActive(true);
        disconnectflag = true;
        DisconnectLocal();

    }
    private void ShowKicked()
    {
        NotificationCanvas.SetActive(true);
        NotificationKicked.SetActive(true);
        disconnectflag = true;
        DisconnectLocal();

    }
    private void ShowInvalid()
    {
        NotificationCanvas.SetActive(true);
        NotificationInvalidName.SetActive(true);
        disconnectflag = true;
        DisconnectLocal();

    }
    private void Disconnect()// hàm ngắt kết nối 
    {
        sw.WriteLine($"{networkSubheader.EXIT}|{username}");
        isconnect = false;
        tcpClient.Close();
        tcpClient.Dispose();
        udpClient_Send.Close();
        udpClient_Send.Dispose();
        Debug.Log("Disconnected from server.");
    }
    private void DisconnectLocal()// hàm ngắt kết nối 
    {
        isconnect = false;
        tcpClient.Close();
        tcpClient.Dispose();
        udpClient_Send.Close();
        udpClient_Send.Dispose();
        Debug.Log("Disconnected from server.");
    }
}