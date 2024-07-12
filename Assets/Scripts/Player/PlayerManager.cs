using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    public static event Action<SongMetadata> OnMetadataChanged;
    public static event Action<float> OnDurationChanged;

    private const string SOCKET = "/tmp/mpvsocket";

    private Process process;
    private StreamWriter processInputWriter;

    private float duration = -1;

    private void Awake()
    {
        Instance = this;
    }

    public void StartPlayerInstance(string filePath)
    {
        // Check if the system is running on Linux
        if (System.Environment.OSVersion.Platform != PlatformID.Unix)
        {
            UnityEngine.Debug.Log("Unsupported platform: This function is intended for Linux systems only.");
            return;
        }
        // Create process start info
        ProcessStartInfo psi = new ProcessStartInfo("mpv");
        psi.Arguments = $"--input-ipc-server={SOCKET} --no-video \"{filePath}\"";
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        // Start the process
        process = new Process();
        process.StartInfo = psi;
        process.Start();

        // Read the output
        process.OutputDataReceived += OutputDataReceived;
        process.ErrorDataReceived += ErrorDataReceived;

        processInputWriter = process.StandardInput;

        process.BeginOutputReadLine();

        Invoke(nameof(RequestMetadata), 0.2f);
        Invoke(nameof(RequestDuration), 0.2f);
        
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if(e.Data.Contains("Exiting") && e.Data.Contains("Errors when loading file")){
            UnityEngine.Debug.LogError("Errors while loading file");
        }

        UnityEngine.Debug.Log(e.Data);
    }

    private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityEngine.Debug.LogError(e.Data);
    }

    public void Pause(){
       LinuxCommand.Run("echo '{ \\\"command\\\": [\\\"set_property\\\", \\\"pause\\\", true] }' | socat - " + SOCKET);
    }

    public void Play(){
        LinuxCommand.Run("echo '{ \\\"command\\\": [\\\"set_property\\\", \\\"pause\\\", false] }' | socat - " + SOCKET);
    }

    public void StopPlayerInstance()
    {
        if (process != null)
        {
            process.Kill();
        }
    }

    public void SkipTo(float time)
    {
        if (process != null)
        {
            string output = LinuxCommand.Run("echo '{ \\\"command\\\": [\\\"seek\\\", \\\"" + time.ToString().Replace(",", ".") + "\\\", \\\"absolute\\\"] }' | socat - " + SOCKET);
            UnityEngine.Debug.Log(output);
        }
    }

    public float GetDuration()
    {


        return duration;
    }

    private void RequestDuration()
    {
        if (process != null)
        {
            string output = LinuxCommand.Run("echo '{ \\\"command\\\": [\\\"get_property\\\", \\\"duration\\\"] }' | socat - " + SOCKET);
            UnityEngine.Debug.Log(output);
            dynamic data = JsonConvert.DeserializeObject<dynamic>(output);
            if (data != null)
            {
                duration = data.data;
            }
        }
    }

    private void RequestMetadata()
    {
        if (process != null)
        {
            string output = LinuxCommand.Run("echo '{ \\\"command\\\": [\\\"get_property\\\", \\\"metadata\\\"] }' | socat - " + SOCKET);
            UnityEngine.Debug.Log(output);
            dynamic data = JsonConvert.DeserializeObject<dynamic>(output);
            SongMetadata metadata = new SongMetadata
            {
                title = data.data.title,
                artist = data.data.artist,
                album = data.data.album,
                year = data.data.date
            };
            UnityEngine.Debug.Log(metadata);
            OnMetadataChanged?.Invoke(metadata);
        }

    }

    void OnDestroy()
    {
        StopPlayerInstance();
    }

    public struct SongMetadata
    {
        public string title;
        public string artist;
        public string album;
        public string year;
    }
}
