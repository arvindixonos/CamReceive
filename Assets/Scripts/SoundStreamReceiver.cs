﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;
using FFmpegOut;
using UnityEngine.UI;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Video;
using UnityEngine.Profiling;
using System.Media;

public class SoundStreamReceiver
{
    int numDataPerRead = 5000;
    byte[] newData;

    Process audioProcess;

    private string ffmpegPath = "";

    private BinaryReader stdout;

    Thread audioFetchThread;
    Thread audioPlayThread;

    private bool firstTime = true;

    woLib WaveOut = new woLib();


    private bool audioPresent = false;

    private int bytesRead = 0;


    public void StartReceivingAudio()
    {
        Application.runInBackground = true;

        ffmpegPath = FFmpegConfig.BinaryPath;

        //         string path = "InputVideo/input%03d.mp4";

        // #if UNITY_EDITOR
        //         path = Application.dataPath.Replace("/Assets", "") + "/" + path;
        // #elif UNITY_STANDALONE
        // 		path = Application.streamingAssetsPath + "/" + path;
        // #endif

        // var opt = "-y -i http://123.176.34.172:8090/test1.mpg -f segment -reset_timestamps 1 " + path;

        // string opt = "-y -re -rtbufsize 100M -f dshow -i video=\"" + UnityEngine.WebCamTexture.devices[0].name + "\":audio=\"" + UnityEngine.Microphone.devices[0]
        //  + "\" http://13.126.154.86:8090/"
        //  + (SkypeManager.Instance.isCaller ? "feed1.ffm" : "feed2.ffm") + " -f segment -segment_time 2 -reset_timestamps 1 -vcodec libvpx -b 465k -pix_fmt yuv420p -profile:v baseline -preset ultrafast  " + path;

        string opt = "-y -i rtsp://13.126.154.86:5454/" + (SkypeManager.Instance.isCaller ? "callerAudio.mp3" : "callerAudio.mp3") + " -f wav -fflags +bitexact -flags:v +bitexact -flags:a +bitexact -map_metadata -1 -";
        // string opt = "-y -f dshow -i audio=\"" + UnityEngine.Microphone.devices[0] + "\"" + " -vn -f wav -fflags +bitexact -flags:v +bitexact -flags:a +bitexact -map_metadata -1 -";

        ProcessStartInfo info = new ProcessStartInfo(Application.streamingAssetsPath + "/FFmpegOut/Windows/WaveOutTestCSharp.exe", opt);

        UnityEngine.Debug.Log(opt);

        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardInput = false;
        info.RedirectStandardOutput = false;
        info.RedirectStandardError = false;

        audioProcess = new Process();
        audioProcess.StartInfo = info;
        audioProcess.EnableRaisingEvents = false;
        audioProcess.Start();

        // stdout = new BinaryReader(audioProcess.StandardOutput.BaseStream);

        // audioFetchThread = new Thread(new ThreadStart(AudioFetchUpdate));
        // audioFetchThread.Priority = System.Threading.ThreadPriority.Highest;
        // audioFetchThread.Start();

        // audioPlayThread = new Thread(new ThreadStart(AudioPlayUpdate));
        // audioPlayThread.Priority = System.Threading.ThreadPriority.Highest;
        // audioPlayThread.Start();

        // WaveOut.InitWODevice(44100, 2, 16, false);
    }

    public void AudioFetchUpdate()
    {
        newData = new byte[numDataPerRead];

        while (true)
        {
            if (audioPresent)
            {
                Thread.Sleep(10);
                continue;
            }

            bytesRead = stdout.Read(newData, 0, numDataPerRead);

            if (firstTime)
            {
                firstTime = false;
                continue;
            }

            if (bytesRead > 0)
                audioPresent = true;
        }
    }

    public unsafe void AudioPlayUpdate()
    {
        newData = new byte[numDataPerRead];

        while (true)
        {
            if (!audioPresent)
            {
                Thread.Sleep(10);
                continue;
            }

            fixed (byte* p = newData)
            {
                IntPtr pPCM = (IntPtr)p;
                WaveOut.SendWODevice(pPCM, (uint)bytesRead);
            }

            audioPresent = false;
        }
    }

    void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityEngine.Debug.Log("error " + e.Data);
    }

    void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityEngine.Debug.Log(e.Data);
    }
    void ProcessExited(object sender, EventArgs e)
    {
        UnityEngine.Debug.Log("exited");
    }

    void ProcessDisposed(object sender, EventArgs e)
    {
        UnityEngine.Debug.Log("disposed");
    }

    public void Destroy()
    {
        WaveOut.Dispose();

        if (audioFetchThread != null)
            audioFetchThread.Abort();

        if (audioPlayThread != null)
            audioPlayThread.Abort();

        if (audioProcess != null)
            audioProcess.Kill();
    }
}
