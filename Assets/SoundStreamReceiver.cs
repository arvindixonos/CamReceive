using System.Collections;
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

    private StreamReceiver streamReceiver;

    private BinaryReader stdout;

    Thread audioFetchThread;

    private bool firstTime = true;

    woLib WaveOut = new woLib();

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

        // + (SkypeManager.Instance.isCaller ? "feed1.ffm" : "feed2.ffm") + " -f rawvideo -vcodec rawvideo -pix_fmt rgb24 -";

        var info = new ProcessStartInfo(ffmpegPath, opt);

        UnityEngine.Debug.Log(opt);

        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardInput = false;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = false;

        audioProcess = new Process();
        audioProcess.StartInfo = info;
        audioProcess.EnableRaisingEvents = true;
        audioProcess.Exited += new EventHandler(ProcessExited);
        audioProcess.Disposed += new EventHandler(ProcessDisposed);
        audioProcess.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
        audioProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

        audioProcess.Start();

        stdout = new BinaryReader(audioProcess.StandardOutput.BaseStream);

        audioFetchThread = new Thread(new ThreadStart(AudioFetchUpdate));
        audioFetchThread.Priority = System.Threading.ThreadPriority.Highest;
        audioFetchThread.Start();

        WaveOut.InitWODevice(44100, 2, 16, false);
    }

    public unsafe void AudioFetchUpdate()
    {
        newData = new byte[numDataPerRead];

        while (true)
        {
            int bytesRead = stdout.Read(newData, 0, numDataPerRead);

            if (firstTime)
            {
                firstTime = false;
                continue;
            }

            fixed (byte* p = newData)
            {
                IntPtr pPCM = (IntPtr)p;
                WaveOut.SendWODevice(pPCM, (uint)bytesRead);
            }
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

    void OnDestroy()
    {
        if (audioProcess != null)
            audioProcess.Kill();

        if (audioFetchThread != null)
            audioFetchThread.Abort();

        WaveOut.CloseWODevice();
        WaveOut.Dispose();
    }

    void OnDisable()
    {
        OnDestroy();
    }
}
