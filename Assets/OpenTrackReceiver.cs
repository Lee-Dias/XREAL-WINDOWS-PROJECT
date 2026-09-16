using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class OpenTrackReceiver : MonoBehaviour
{
    [Header("Configurações de Rede")]
    public int port = 4242;

    [Header("Objeto Alvo")]
    public Transform targetTransform;

    [Header("Tecla para Centrar")]
    public KeyCode centerKey = KeyCode.C;

    [Header("Ajuste Manual de Inclinação (Pitch Offset)")]
    [Tooltip("Soma graus à vista vertical (ex: 0, 15, 30) para ajustar a altura do olhar padrão.")]
    public float manualPitchOffset = 0f;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    private Vector3 rawRotation;
    private readonly object lockObject = new object();

    // Guardar os valores brutos capturados no momento do 'C'
    private float pitchCenter = 0f;
    private float yawCenter = 0f;
    private bool isCalibrated = false;

    void Start()
    {
        if (targetTransform == null)
            targetTransform = transform;

        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveData()
    {
        try
        {
            udpClient = new UdpClient(port);
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (isRunning)
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                if (data.Length >= 48)
                {
                    // ADICIONA ESTA LINHA PARA TESTAR NA CONSOLA DO UNITY
                    Debug.Log($"[REDE OK] Pacote recebido de: {remoteEndPoint.Address}");

                    double yaw = BitConverter.ToDouble(data, 24);
                    double pitch = BitConverter.ToDouble(data, 32);

                    lock (lockObject)
                    {
                        rawRotation = new Vector3((float)pitch, (float)yaw, 0f);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("UDP: " + e.Message);
        }
    }

    void Update()
    {
        Vector3 rot;
        lock (lockObject)
        {
            rot = rawRotation;
        }

        float currentPitch = -rot.x;
        float currentYaw = rot.y;

        // Se for a primeira vez ou se carregares no 'C', guarda o centro exato
        if (!isCalibrated || Input.GetKeyDown(centerKey))
        {
            pitchCenter = currentPitch;
            yawCenter = currentYaw;
            isCalibrated = true;
            Debug.Log("Centro calibrado!");
        }

        // Calcula os ângulos relativos ao ponto em que carregaste no 'C'
        float finalPitch = (currentPitch - pitchCenter) + manualPitchOffset;
        float finalYaw = currentYaw - yawCenter;

        // --- GARANTIA CONTRA DIAGONAL ---
        // 1. O Yaw roda SEMPRE em torno do vetor global do mundo (Vector3.up).
        // Isso impede fisicamente a câmara de inclinar de lado ao virar a cabeça.
        Quaternion yawRotation = Quaternion.AngleAxis(finalYaw, Vector3.up);

        Quaternion pitchRotation = Quaternion.AngleAxis(finalPitch, Vector3.right);

        targetTransform.localRotation = yawRotation * pitchRotation;
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (udpClient != null) udpClient.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}