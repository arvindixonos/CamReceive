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
    int numDataPerRead = 5000;
    byte[] pattern;
    byte[] newData;

    SoundPlayer soundPlayer = new SoundPlayer();

    Process senderProcess;

    private string ffmpegPath = "";

    private StreamReceiver streamReceiver;

    private BinaryReader stdout;

    Thread audioFetchThread;
    Thread audioPlayThread;

    [DllImport("msvcrt.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int memcmp(byte[] b1, byte[] b2, long count);

    MemoryStream memoryStream = new MemoryStream();

    private bool firstTime = true;
    private byte[] headerBytes;

    private List<byte[]> audioBuffers = new List<byte[]>();

    void Start()
    {
        // FileStream fileStream = File.Open("ourout1.wav", FileMode.Open, FileAccess.Read);

        // print(fileStream.Length);

        // byte[] inbyte = new byte[(int)fileStream.Length];

        // fileStream.Read(inbyte, 0, inbyte.Length);

        // // // // fileStream.Close();

        // byte[] sizeByte = new byte[4];
        // int j = 0;
        // for (int i = 8; i < 12; i++)
        // {
        //     // sizeByte[j] = inbyte[i];
        //     print(i + " " + inbyte[i]);

        //     j += 1;
        // }

        // print(BitConverter.ToInt16(inbyte, 34));

        // // // fileStream.CopyTo(memoryStream);

        // memoryStream = new MemoryStream(inbyte);

        // soundPlayer.Stream = memoryStream;
        // soundPlayer.Play();

        // fileStream.Close();

        // print("NEW");

        StartReceivingAudio();
    }

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

        string opt = "-y -i rtsp://13.126.154.86:5454/callerAudio.mp3 -f wav -fflags +bitexact -flags:v +bitexact -flags:a +bitexact -map_metadata -1 -";

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

        stdout = new BinaryReader(senderProcess.StandardOutput.BaseStream);

        pattern = new byte[4];
        pattern[0] = 82;
        pattern[1] = 73;
        pattern[2] = 70;
        pattern[3] = 70;

        headerBytes = new byte[numHeaderBytes];

        audioFetchThread = new Thread(new ThreadStart(AudioFetchUpdate));
        audioFetchThread.Priority = System.Threading.ThreadPriority.Highest;
        audioFetchThread.Start();

        audioPlayThread = new Thread(new ThreadStart(AudioPlayUpdate));
        audioPlayThread.Priority = System.Threading.ThreadPriority.Highest;
        audioPlayThread.Start();
    }

    int numHeaderBytes = 44;

    public void AudioPlayUpdate()
    {
        int outID = 0;
        while (true)
        {
            if (audioBuffers.Count > 0)
            {
                // print("A");

                byte[] currentAudioBuffer = audioBuffers[0];
                audioBuffers.RemoveAt(0);

                byte[] audioBufferWithHeader = new byte[currentAudioBuffer.Length + numHeaderBytes];
                Buffer.BlockCopy(headerBytes, 0, audioBufferWithHeader, 0, numHeaderBytes);
                Buffer.BlockCopy(currentAudioBuffer, 0, audioBufferWithHeader, numHeaderBytes, currentAudioBuffer.Length);

                byte[] chunkSize = BitConverter.GetBytes(audioBufferWithHeader.Length - 8);
                Buffer.BlockCopy(chunkSize, 0, audioBufferWithHeader, 4, 4);

                chunkSize = BitConverter.GetBytes(audioBufferWithHeader.Length - 44);
                Buffer.BlockCopy(chunkSize, 0, audioBufferWithHeader, 40, 4);

                // FileStream ourFileStream = File.Create("ourout" + outID + ".wav");
                // ourFileStream.Write(audioBufferWithHeader, 0, audioBufferWithHeader.Length);
                // ourFileStream.Close();

                print(audioBufferWithHeader.Length);

                outID += 1;

                memoryStream = new MemoryStream(audioBufferWithHeader, 0, audioBufferWithHeader.Length);

                soundPlayer = new SoundPlayer();
                soundPlayer.Stream = memoryStream;
                soundPlayer.Play();

                // print("Finished");

                soundPlayer = null;
            }

            Thread.Sleep(10);
        }
    }

    int totalBufferAdd = 200000;
    public void AudioFetchUpdate()
    {
        int bytesAdded = 0;
        newData = new byte[numDataPerRead];

        while (true)
        {
            // FileStream fileStream = File.Open("1.wav", FileMode.Open, FileAccess.Read);
            // stdout = new BinaryReader(fileStream);

            //chunk 0
            // int chunkID       = stdout.ReadInt32();
            // int fileSize      = stdout.ReadInt32();
            // int riffType      = stdout.ReadInt32();

            // // chunk 1
            // int fmtID         = stdout.ReadInt32();
            // int fmtSize       = stdout.ReadInt32(); // bytes for this chunk
            // int fmtCode       = stdout.ReadInt16();
            // int channels      = stdout.ReadInt16();
            // int sampleRate    = stdout.ReadInt32();
            // int byteRate      = stdout.ReadInt32();
            // int fmtBlockAlign = stdout.ReadInt16();
            // int bitDepth      = stdout.ReadInt16();

            // // chunk 2
            // int dataID = stdout.ReadInt32();
            // int bytes = stdout.ReadInt32();

            // print(chunkID + " " + bytes + " " + fileSize);

            // // byte[] byteArray = stdout.ReadBytes(bytes);

            // print(BitConverter.GetBytes(dataID)[0] + " " + BitConverter.GetBytes(dataID)[1] + " " + BitConverter.GetBytes(dataID)[2] + " " + BitConverter.GetBytes(dataID)[3]);

            // break;

            if(!firstTime)
            {
                Array.Resize(ref newData, bytesAdded + numDataPerRead);
            }

            int bytesRead = stdout.Read(newData, bytesAdded, numDataPerRead);

            print(bytesRead);

            if (firstTime)
            {
                Buffer.BlockCopy(newData, 0, headerBytes, 0, numHeaderBytes);

                firstTime = false;

                continue;
            }

            bytesAdded += bytesRead;
            
            Array.Resize(ref newData, bytesAdded);

            if(bytesAdded >= totalBufferAdd)
            {
                audioBuffers.Add(newData);
                print("Adding " + audioBuffers.Count);
                bytesAdded = 0;
            }
        }
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

        if (audioFetchThread != null)
            audioFetchThread.Abort();

        if (audioPlayThread != null)
            audioPlayThread.Abort();

        if (soundPlayer != null)
            soundPlayer.Stop();
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            StartReceivingAudio();
        }
    }
}
