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
	int numDataPerRead = 500;
    byte[] data;
    byte[] tempData = new byte[20000];
    int count = 0;
    byte[] pattern;

    byte[] newData;

    SoundPlayer soundPlayer = new SoundPlayer();

    Process senderProcess;

    private string ffmpegPath = "";

    private StreamReceiver streamReceiver;

	private BinaryReader stdout;

	Thread audioThread;
	
	[DllImport("msvcrt.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int memcmp(byte[] b1, byte[] b2, long count);


    public void StartReceivingAudio()
    {
        Application.runInBackground = true;

		newData = new byte[numDataPerRead];

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

        string opt = "-nostdin -y -i rtsp://13.126.154.86:5454/callerAudio.mp3 -f s16le -acodec pcm_s16le -";

        // + (SkypeManager.Instance.isCaller ? "feed1.ffm" : "feed2.ffm") + " -f rawvideo -vcodec rawvideo -pix_fmt rgb24 -";

        var info = new ProcessStartInfo(ffmpegPath, opt);

        UnityEngine.Debug.Log(opt);

        info.UseShellExecute = false;
        info.CreateNoWindow = true;
        info.RedirectStandardInput = true;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        senderProcess = new Process();
        senderProcess.StartInfo = info;
        senderProcess.EnableRaisingEvents = true;
        senderProcess.Exited += new EventHandler(ProcessExited);
        senderProcess.Disposed += new EventHandler(ProcessDisposed);
        senderProcess.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
        senderProcess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

        senderProcess.Start();

		stdout = new BinaryReader(senderProcess.StandardError.BaseStream);

		pattern = new byte[4];
        pattern[0] = 52;
        pattern[1] = 49;
		pattern[2] = 46;
		pattern[3] = 46;

		audioThread = new Thread(new ThreadStart(ThreadUpdate));
        audioThread.Priority = System.Threading.ThreadPriority.Highest;
        audioThread.Start();

        // soundPlayer.Stream = senderProcess.StandardOutput.BaseStream;
		// soundPlayer.PlaySync();
    }

	public void ThreadUpdate()
    {
        // Profiler.BeginThreadProfiling("TLSKYPE_THREADS", "Sender Thread");

        while (true)
        {
            int bytesRead = numDataPerRead;

            newData = stdout.ReadBytes(numDataPerRead); 

            // bytesRead = stdout.Read(newData, 0, numDataPerRead);

            // bytesRead = streamReader.Read(newData, 0, numDataPerRead);

            print(bytesRead);

            int index = SearchBytePattern();

            if (index != -1)
            {
                Buffer.BlockCopy(newData, 0, tempData, count, index);
                count += index;

                data = new byte[count];
                Buffer.BlockCopy(tempData, 0, data, 0, count);

                index += 2;

                Buffer.BlockCopy(newData, index, tempData, 0, bytesRead - index);
                count = bytesRead - index;

				print("GOT ONE");
            }
            else
            {
                Buffer.BlockCopy(newData, 0, tempData, count, bytesRead);
                count += bytesRead;
            }
        }

        // Profiler.EndThreadProfiling();
    }

	 public int SearchBytePattern()
    {
        int patternLength = pattern.Length;
        int totalLength = newData.Length;
        byte firstMatchByte = pattern[0];

		// Debug.Log(newData[0] + " " + newData[1]);

        for (int i = 0; i < totalLength; i++)
        {
            if (firstMatchByte == newData[i] && totalLength - i >= patternLength)
            {
                byte[] match = new byte[patternLength];
                Buffer.BlockCopy(newData, i, match, 0, patternLength);
                if (memcmp(pattern, match, patternLength) == 0)
                {
                    return i;
                }
            }
        }

        return -1;
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

         if (audioThread != null)
            audioThread.Abort();
    }

	void Update()
	{
		if(Input.GetKeyUp(KeyCode.A))
		{
			StartReceivingAudio();
		}
	}
}
