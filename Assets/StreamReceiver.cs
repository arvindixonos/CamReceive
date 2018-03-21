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
using UnityEngine;

public class StreamReceiver
{
    int numDataPerRead = 7372800;
    byte[] data;
    byte[] tempData = new byte[100000];
    int count = 0;
    byte[] pattern;

    byte[] newData;

    private BinaryReader stdout;

    public RawImage targetImage;
    private Texture2D targetTexture;
    Thread renderThread;

    private StreamReader streamReader;

    [DllImport("msvcrt.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int memcmp(byte[] b1, byte[] b2, long count);



    public StreamReceiver(StreamReader stream, RawImage targetImage, Vector2 textureSize)
    {
        this.streamReader = stream;
        this.stdout = new BinaryReader(stream.BaseStream);
        this.targetImage = targetImage;

        targetTexture = new Texture2D((int)textureSize.x, (int)textureSize.y, TextureFormat.RGB24, false);

        newData = new byte[numDataPerRead];
    }

    public void StartReceivingStream()
    {
        pattern = new byte[2];
        pattern[0] = 255;
        pattern[1] = 217;

        renderThread = new Thread(new ThreadStart(ThreadUpdate));

        renderThread.Priority = System.Threading.ThreadPriority.Highest;
        renderThread.Start();
    }

    public void ThreadUpdate()
    {
        // Profiler.BeginThreadProfiling("TLSKYPE_THREADS", "Sender Thread");

        while (true)
        {
            int bytesRead = numDataPerRead;

            // newData = stdout.ReadBytes(numDataPerRead); 
            bytesRead = stdout.Read(newData, 0, numDataPerRead);

            // bytesRead = streamReader.Read(newData, 0, numDataPerRead);

            int index = SearchBytePattern();

            // Debug.Log(index);

            if (index != -1)
            {
                Buffer.BlockCopy(newData, 0, tempData, count, index);
                count += index;

                data = new byte[count];
                Buffer.BlockCopy(tempData, 0, data, 0, count);

                index += 2;

                Buffer.BlockCopy(newData, index, tempData, 0, bytesRead - index);
                count = bytesRead - index;
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

    public void DrawFrame()
    {
        if (data != null)
        {
            targetTexture.LoadImage(data);
            targetTexture.Apply();

            targetImage.texture = targetTexture;

            data = null;
        }
    }

    public void AbortThread()
    {
        if (renderThread != null)
            renderThread.Abort();
    }
}
