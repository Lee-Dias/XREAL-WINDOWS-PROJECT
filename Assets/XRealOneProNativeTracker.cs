using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;

public class XRealOneProNativeTracker : MonoBehaviour
{
    private const string GLASSES_IP = "169.254.2.1";
    private const int GLASSES_PORT = 52998;

    private TcpClient _tcpClient;
    private NetworkStream _networkStream;
    private Thread _connectionThread;
    private bool _isRunning = false;

    private Quaternion _targetRotation = Quaternion.identity;
    private readonly object _lockObject = new object();

    void Start()
    {
        _isRunning = true;
        _connectionThread = new Thread(ConnectToGlasses);
        _connectionThread.IsBackground = true;
        _connectionThread.Start();
    }

    void Update()
    {
        lock (_lockObject)
        {
            // Valida se o Quaternion gerado contém números reais válidos
            if (!float.IsNaN(_targetRotation.x) && !float.IsNaN(_targetRotation.y) &&
                !float.IsNaN(_targetRotation.z) && !float.IsNaN(_targetRotation.w))
            {
                // Interpolação (Lerp) para suavizar o ruído eletrônico natural do sensor
                transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRotation, Time.deltaTime * 12f);
            }
        }
    }

    private void ConnectToGlasses()
    {
        while (_isRunning)
        {
            try
            {
                if (_tcpClient == null || !_tcpClient.Connected)
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.ReceiveTimeout = 1000;
                    _tcpClient.Connect(GLASSES_IP, GLASSES_PORT);
                    _networkStream = _tcpClient.GetStream();
                    Debug.Log("[XREAL Nativo] Conectado diretamente ao fluxo de hardware do chip X1!");
                }

                byte[] buffer = new byte[1024];

                while (_isRunning && _tcpClient.Connected)
                {
                    if (_networkStream.DataAvailable)
                    {
                        int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead >= 32)
                        {
                            ParseX1Data(buffer, bytesRead);
                        }
                    }
                    Thread.Sleep(2);
                }
            }
            catch (Exception)
            {
                CleanupConnection();
                Thread.Sleep(2000);
            }
        }
    }

    private void ParseX1Data(byte[] packet, int length)
    {
        for (int i = 0; i < length - 28; i++)
        {
            if ((packet[i] == 0x28 || packet[i] == 0x27) && packet[i + 1] == 0x36)
            {
                int offset = i + 16;

                // Lê os floats diretamente do ponto dinâmico do seu firmware
                float val1 = BitConverter.ToSingle(packet, offset);
                float val2 = BitConverter.ToSingle(packet, offset + 4);
                float val3 = BitConverter.ToSingle(packet, offset + 8);

                if (!float.IsNaN(val1) && !float.IsNaN(val2) && !float.IsNaN(val3))
                {
                    // Converte radianos para graus de forma direta
                    float degX = val1 * Mathf.Rad2Deg;
                    float degY = val2 * Mathf.Rad2Deg;
                    float degZ = val3 * Mathf.Rad2Deg;

                    lock (_lockObject)
                    {
                        // Atribuição direta sem filtros para vermos o movimento acontecer
                        _targetRotation = Quaternion.Euler(degX, degY, degZ);
                    }
                }
                break;
            }
        }
    }


    private void CleanupConnection()
    {
        if (_networkStream != null) _networkStream.Close();
        if (_tcpClient != null) _tcpClient.Close();
        _networkStream = null;
        _tcpClient = null;
    }

    void OnDestroy()
    {
        _isRunning = false;
        CleanupConnection();
        if (_connectionThread != null && _connectionThread.IsAlive)
        {
            _connectionThread.Join(500);
        }
    }
}
