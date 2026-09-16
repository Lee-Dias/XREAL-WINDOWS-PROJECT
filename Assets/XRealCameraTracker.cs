using UnityEngine;
using HidLibrary;
using System.Linq;
using System.Collections.Generic;
using System;

public class XRealCameraTracker : MonoBehaviour
{
    [Header("Configurações USB")]
    public int vendorId = 0x3318;  // VID (13080)
    public int productId = 0x0436; // PID (1078)

    [Header("Sensibilidade")]
    public float sensitivity = 0.05f;

    private List<HidDevice> openDevices = new List<HidDevice>();

    // Rotação
    private Vector3 rawGyroDelta = Vector3.zero;
    private Vector3 targetRotation = Vector3.zero;
    private readonly object lockObject = new object();

    // Diagnóstico
    private int packetCount = 0;
    private int lastReportSize = 0;

    void Start()
    {
        Camera mainCam = GetComponent<Camera>();
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
        }

        // Procura todas as sub-interfaces dos óculos
        var devList = HidDevices.Enumerate(vendorId, productId).ToList();
        Debug.Log($"[USB Setup] Encontradas {devList.Count} interfaces nos óculos.");

        int index = 0;
        foreach (var dev in devList)
        {
            dev.OpenDevice();

            if (dev.IsOpen)
            {
                openDevices.Add(dev);

                // Usamos Attributes para confirmação de conexão (compatível com todas as versões)
                Debug.Log($"[Interface #{index}] Dispositivo aberto com sucesso! VID: 0x{dev.Attributes.VendorHexId} | PID: 0x{dev.Attributes.ProductHexId}");

                // Envia o sinal de ativação do giroscópio
                byte[] enableCommand = new byte[64];
                enableCommand[0] = 0x00; // Report ID
                enableCommand[1] = 0x1D; // Comando IMU
                dev.Write(enableCommand);

                // Inicia a escuta contínua
                IniciarEscuta(dev);
            }
            index++;
        }
    }

    private void IniciarEscuta(HidDevice dev)
    {
        if (dev != null && dev.IsOpen)
        {
            dev.ReadReport(report => OnReportReceived(dev, report));
        }
    }

    private void OnReportReceived(HidDevice dev, HidReport report)
    {
        try
        {
            if (report != null && report.Data != null && report.Data.Length > 0)
            {
                packetCount++;
                lastReportSize = report.Data.Length;

                // Lemos os dados do giroscópio se o pacote tiver tamanho suficiente
                if (report.Data.Length >= 30)
                {
                    short gyroX = BitConverter.ToInt16(report.Data, 25);
                    short gyroY = BitConverter.ToInt16(report.Data, 27);
                    short gyroZ = BitConverter.ToInt16(report.Data, 29);

                    lock (lockObject)
                    {
                        rawGyroDelta = new Vector3(-gyroX, -gyroY, gyroZ) * sensitivity;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro ao processar pacote USB: " + ex.Message);
        }
        finally
        {
            // Pede o próximo pacote de forma contínua
            IniciarEscuta(dev);
        }
    }

    void Update()
    {
        Vector3 deltaToApply = Vector3.zero;

        lock (lockObject)
        {
            deltaToApply = rawGyroDelta;
            rawGyroDelta = Vector3.zero;
        }

        if (deltaToApply != Vector3.zero)
        {
            targetRotation += deltaToApply * Time.deltaTime;
            transform.localRotation = Quaternion.Euler(targetRotation.x, targetRotation.y, targetRotation.z);
        }
    }

    void OnEnable()
    {
        InvokeRepeating(nameof(LogStatus), 2.0f, 2.0f);
    }

    void LogStatus()
    {
        Debug.Log($"[Status USB] Pacotes recebidos: {packetCount} | Último pacote: {lastReportSize} bytes");
    }

    void OnDisable()
    {
        CancelInvoke(nameof(LogStatus));
    }

    void OnApplicationQuit()
    {
        foreach (var dev in openDevices)
        {
            if (dev != null && dev.IsOpen)
            {
                dev.CloseDevice();
            }
        }
    }
}