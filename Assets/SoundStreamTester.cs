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

public class SoundStreamTester : MonoBehaviour
{
    SoundPlayer soundPlayer = new SoundPlayer();

    Process senderProcess;

    private string ffmpegPath = "";

    private StreamReceiver streamReceiver;

    void Start()
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

        string opt = "-y -i http://13.126.154.86:8090/callerAudio.wav -f s16le -acodec pcm_s16le -";

        // + (SkypeManager.Instance.isCaller ? "feed1.ffm" : "feed2.ffm") + " -f rawvideo -vcodec rawvideo -pix_fmt rgb24 -";

        var info = new ProcessStartInfo(ffmpegPath, opt);

        UnityEngine.Debug.Log(opt);

        info.UseShellExecute = false;
        info.CreateNoWindow = false;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        // info.RedirectStandardError = false;

        senderProcess = new Process();
        senderProcess.StartInfo = info;
        senderProcess.EnableRaisingEvents = true;
        senderProcess.Exited += new EventHandler(ProcessExited);
        senderProcess.Disposed += new EventHandler(ProcessDisposed);
        senderProcess.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
        senderProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

        senderProcess.Start();

        // soundPlayer.Stream = senderProcess.StandardOutput.BaseStream;
		// soundPlayer.PlaySync();
    }

    void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        print("error " + e.Data);
    }

    void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        print(e.Data);
    }
    void ProcessExited(object sender, EventArgs e)
    {
        print("exited");
    }

    void ProcessDisposed(object sender, EventArgs e)
    {
        print("disposed");
    }

    void OnDestroy()
    {
        if (senderProcess != null)
            senderProcess.Kill();

        if (streamReceiver != null)
            streamReceiver.AbortThread();
    }

    void Update()
    {
        if (streamReceiver != null)
        {
            streamReceiver.DrawFrame();
        }
    }
}
