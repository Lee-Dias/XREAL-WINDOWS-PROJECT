using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class OpenTrackReceiver : MonoBehaviour
{
    [Header("Configurações de Rede")]
    [SerializeField]
    private int port = 4242;

    [Header("Objeto Alvo")]
    [SerializeField]
    private Transform targetTransform;

    [Header("Tecla para Centrar")]
    [SerializeField]
    private KeyCode centerKey = KeyCode.C;

    [Header("Ajuste Manual de Inclinação (Pitch Offset)")]
    [Tooltip("Soma graus à vista vertical (ex: 0, 15, 30) para ajustar a altura do olhar padrão.")]
    [SerializeField]
    private float manualPitchOffset = 0f;

    private UdpClient udpClient;
    private Thread receiveThread;
    private bool isRunning = true;

    private Vector3 rawRotation;
    private readonly object lockObject = new object();

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

        if (!isCalibrated || Input.GetKeyDown(centerKey))
        {
            pitchCenter = currentPitch;
            yawCenter = currentYaw;
            isCalibrated = true;
            Debug.Log("Centro calibrado!");
        }

        float finalPitch = (currentPitch - pitchCenter) + manualPitchOffset;
        float finalYaw = currentYaw - yawCenter;

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