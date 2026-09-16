using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine;

public class NetReciever : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField]
    public int port = 4242;

    [Header("Target Camera / Object")]
    private Transform targetTransform; 

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = false;

    private Vector3 latestRotation = Vector3.zero;
    private readonly object lockObject = new object();

    void Start()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        StartReceiver();
    }

    void StartReceiver()
    {
        try
        {
            IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(localEP);
            isRunning = true;

            receiveThread = new Thread(ReceiveData)
            {
                IsBackground = true
            };
            receiveThread.Start();
            Debug.Log($"<color=green>[UDP] Servidor ativo e a escutar na porta {port}!</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UDP ERROR] Falha ao abrir a porta {port}. Certifica-te que o OpenTrack/PowerShell estão fechados! Erro: {ex.Message}");
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                Debug.Log($"<color=yellow>[UNITY RECEBEU] {data.Length} bytes de {remoteEndPoint.Address}</color>");

                if (data != null && data.Length >= 48)
                {
                    double yaw = BitConverter.ToDouble(data, 24);
                    double pitch = BitConverter.ToDouble(data, 32);
                    double roll = BitConverter.ToDouble(data, 40);

                    lock (lockObject)
                    {
                        latestRotation = new Vector3((float)-pitch, -(float)-yaw, (float)roll);
                    }
                }
            }
            catch (Exception ex)
            {
                if (isRunning) Debug.LogWarning($"[UDP Read Warning] {ex.Message}");
            }
        }
    }

    void Update()
    {
        lock (lockObject)
        {
            targetTransform.localRotation = Quaternion.Euler(latestRotation);
        }
    }

    void OnDisable() => StopReceiver();
    void OnApplicationQuit() => StopReceiver();

    void StopReceiver()
    {
        isRunning = false;
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
    }
}
