using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class OpenTrackLauncher : MonoBehaviour
{
    [Header("Configurações do Perfil")]
    [Tooltip("Nome do ficheiro .ini dentro de StreamingAssets/OpenTrack/")]
    public string profileFileName = "default.ini";

    [Header("Opções de Janela")]
    [Tooltip("Executa o OpenTrack oculto sem abrir janela no ecrã.")]
    public bool hideWindow = true;

    private Process openTrackProcess;

    void Start()
    {
        StartOpenTrack();
    }

    private void StartOpenTrack()
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
                    Arguments = $"--profile \"{profilePath}\"",
                    WorkingDirectory = folderPath
                };

                if (hideWindow)
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.CreateNoWindow = true;
                }

                openTrackProcess = Process.Start(startInfo);
                UnityEngine.Debug.Log("[OpenTrackLauncher] OpenTrack iniciado em segundo plano com sucesso!");
            }
            else
            {
                UnityEngine.Debug.LogError($"[OpenTrackLauncher] Não foi encontrado o executável em: {exePath}");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[OpenTrackLauncher] Erro ao arrancar o processo: {ex.Message}");
        }
    }

    void OnApplicationQuit()
    {
        if (openTrackProcess != null && !openTrackProcess.HasExited)
        {
            try
            {
                openTrackProcess.Kill();
                openTrackProcess.Dispose();
                UnityEngine.Debug.Log("[OpenTrackLauncher] OpenTrack encerrado.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[OpenTrackLauncher] Falha ao fechar o OpenTrack: {ex.Message}");
            }
        }
    }
}