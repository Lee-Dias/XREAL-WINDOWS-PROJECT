using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class OpenTrackManager : MonoBehaviour
{
    [Header("Executável Embutido (StreamingAssets)")]
    [SerializeField] private bool autoStartOpenTrack = true;
    [SerializeField] private string profileFileName = "default.ini";

    [Header("Configurações da Rede UDP")]
    [SerializeField] private int port = 4242;

    [Header("Alvo e Calibração")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private KeyCode centerKey = KeyCode.C;
    [SerializeField] private float manualPitchOffset = 0f;

    private Process openTrackProcess;
    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    private Vector3 rawRotation;
    private readonly object lockObject = new object();

    private Quaternion centerRotation = Quaternion.identity;
    private bool isCalibrated = false;

    void Start()
    {
        if (targetTransform == null)
        {
            targetTransform = transform;
        }

        if (autoStartOpenTrack)
        {
            StartOpenTrackProcess();
        }

        receiveThread = new Thread(ReceiveUDPData)
        {
            IsBackground = true
        };
        receiveThread.Start();
    }

    private void StartOpenTrackProcess()
    {
        try
        {
            string folderPath = Path.Combine(Application.streamingAssetsPath, "OpenTrack");
            string exePath = Path.Combine(folderPath, "opentrack.exe");
            string profilePath = Path.Combine(folderPath, profileFileName);

            if (File.Exists(exePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--profile \"{profilePath}\" --starttrack",
                    WorkingDirectory = folderPath
                };

                openTrackProcess = Process.Start(startInfo);
                UnityEngine.Debug.Log("[OpenTrackManager] OpenTrack iniciado automaticamente!");
            }
            else
            {
                UnityEngine.Debug.LogError($"[OpenTrackManager] Ficheiro opentrack.exe não encontrado em: {exePath}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[OpenTrackManager] Erro ao arrancar o processo: {ex.Message}");
        }
    }

    private bool hasReceivedFirstPacket = false;

    private void ReceiveUDPData()
    {
        try
        {
            udpClient = new UdpClient(port);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                if (data != null && data.Length >= 48)
                {
                    double yaw = BitConverter.ToDouble(data, 24);
                    double pitch = BitConverter.ToDouble(data, 32);
                    double roll = BitConverter.ToDouble(data, 40);

                    lock (lockObject)
                    {
                        rawRotation = new Vector3((float)pitch, (float)yaw, (float)roll);
                    }

                    if (!hasReceivedFirstPacket)
                    {
                        hasReceivedFirstPacket = true;
                        UnityEngine.Debug.Log("[OpenTrackManager] Dados UDP a ser recebidos com sucesso!");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (isRunning)
            {
                UnityEngine.Debug.LogWarning($"[OpenTrackManager UDP] {ex.Message}");
            }
        }
    }

    void Update()
    {
        if (!hasReceivedFirstPacket) return;

        Vector3 currentRaw;
        lock (lockObject)
        {
            currentRaw = rawRotation;
        }

        float pitch = -currentRaw.x + manualPitchOffset;
        float yaw = currentRaw.y;
        float roll = -currentRaw.z;

        Quaternion currentQuat = Quaternion.Euler(pitch, yaw, roll);

        if (!isCalibrated || Input.GetKeyDown(centerKey))
        {
            centerRotation = currentQuat;
            isCalibrated = true;
            UnityEngine.Debug.Log("[OpenTrackManager] Visão centrada com dados reais!");
        }

        Quaternion calibratedRotation = Quaternion.Inverse(centerRotation) * currentQuat;
        targetTransform.localRotation = calibratedRotation;
    }

    void OnApplicationQuit()
    {

        isRunning = false;

        if (udpClient != null)
        {
            udpClient.Close();
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(100);
        }

        if (openTrackProcess != null && !openTrackProcess.HasExited)
        {
            try
            {
                openTrackProcess.Kill();
                openTrackProcess.Dispose();
                UnityEngine.Debug.Log("[OpenTrackManager] OpenTrack encerrado.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[OpenTrackManager] Falha ao encerrar o OpenTrack: {ex.Message}");
            }
        }
    }
}