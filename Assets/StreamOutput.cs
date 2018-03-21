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

public class StreamOutput : MonoBehaviour
{
    BinaryReader stdout;
   
    Process subprocess;

    Texture2D texture;

    private string ffmpegPath = "";

    FileStream opnFile;

    PerformanceCounter performanceCounter;

    private List<string> filesQueue = new List<string>();

    public VideoPlayer targetVideoPlayer;

    void Start()
    {
        Application.runInBackground = true;

        // FileImporter.onNewFileAdded += NewFileAdded;

        ffmpegPath = FFmpegConfig.BinaryPath;

        texture = new Texture2D(640, 480, TextureFormat.RGB24, false);

        StreamOutputFile();

        targetVideoPlayer.loopPointReached += VideoCompleted;
        targetVideoPlayer.prepareCompleted += VideoPrepareCompleted;

        StartCoroutine("OpenFile");
    }

    public void VideoCompleted(VideoPlayer source)
    {
        print("Video Completed " + source.url);

        // targetVideoPlayer.Stop();
        // File.Delete(source.url.Replace("file://", ""));
    }

    public void VideoPrepareCompleted(VideoPlayer source)
    {
        targetVideoPlayer.Play();
    }

    // void FileSystemWatcher()
    // {
    // 	FileSystemWatcher watcher = new FileSystemWatcher();
    //     watcher.Path = "C:\\Intel";

    // 	watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
    //        | NotifyFilters.FileName | NotifyFilters.DirectoryName;
    //     watcher.Filter = "*.txt";

    // 	watcher.Changed += new FileSystemEventHandler(OnChanged);
    //     watcher.Created += new FileSystemEventHandler(OnChanged);
    //     watcher.Deleted += new FileSystemEventHandler(OnChanged);
    //     watcher.Renamed += new RenamedEventHandler(OnRenamed);

    // 	watcher.EnableRaisingEvents = true;
    // }

    private static void OnChanged(object source, FileSystemEventArgs e)
    {
        print("File: " + e.FullPath + " " + e.ChangeType);
    }

    private static void OnRenamed(object source, RenamedEventArgs e)
    {
        print("File: " + e.FullPath + " " + e.ChangeType);
    }

    void OnApplicationQuit()
    {
        // FileImporter.onNewFileAdded -= NewFileAdded;
    }

    void NewFileAdded(string fileName)
    {
        print("New File " + fileName);

        filesQueue.Add(fileName);
    }

    IEnumerator OpenFile()
    {
        while (true)
        {
            while (filesQueue.Count == 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            string fileName = filesQueue[0];
            filesQueue.RemoveAt(0);

            // opnFile = File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            // long bytesRead = 0;
            // long totalBytes = opnFile.Length;
            // int bytesPerRead = 2764800;

            // // print(fileName);
            // // print(opnFile.Length);

            // byte[] datas = new byte[bytesPerRead];
            // for (; bytesRead < totalBytes; bytesRead += bytesPerRead)
            // {
            //     opnFile.Read(datas, 0, bytesPerRead);

            //     texture.LoadRawTextureData(datas);
            //     texture.Apply();

            //     rawImage.texture = texture;

            //     totalBytes = opnFile.Length;

            //     subprocess.Refresh();

            //     yield return new WaitForSeconds(0.01f);
            // }
            // opnFile.Dispose();
            // opnFile.Close();


            while (true)
            {
                try
                {
            		print("Trying " + fileName);

                    FileStream videoFile = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                    videoFile.Close();

                    break;
                }
                catch (IOException exception)
                {
                }

                yield return new WaitForSeconds(0.01f);
            }

            while (true)
            {
                if (!targetVideoPlayer.isPlaying)
                {
                    break;
                }

                yield return new WaitForSeconds(0.01f);
            }

            targetVideoPlayer.url = fileName.Replace("\\", "/");
            print("Playing " + targetVideoPlayer.url);
            targetVideoPlayer.Prepare();
        }
    }

    void FTPTry()
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://kidsgenie.com/testupload.txt");
        request.Method = WebRequestMethods.Ftp.DownloadFile;

        request.Credentials = new NetworkCredential("kidsgenie", "kidTL@domains123ie");

        StreamReader sourceStream = new StreamReader("testupload.txt");
        BinaryReader binaryReader = new BinaryReader(sourceStream.BaseStream);
        byte[] fileContents = Encoding.ASCII.GetBytes(sourceStream.ReadToEnd());
        sourceStream.Close();
        request.ContentLength = fileContents.Length;

        string results = System.Text.Encoding.UTF8.GetString(fileContents);
        results = results.Replace("\r", string.Empty);

        fileContents = Encoding.ASCII.GetBytes(results);

        print(results.Length + " " + results);
        print(fileContents.Length);

        // byte[] data = new byte[256];
        // print(StreamReader.Read(data, 0, 256)); 

        for (int i = 0; i < fileContents.Length; i++)
        {
            print(fileContents[i] + " " + (char)fileContents[i]);
        }

        Stream StreamReader = request.GetResponse().GetResponseStream();

        // Stream requestStream = request.GetRequestStream();  
        // requestStream.Read(fileContents, 0, fileContents.Length);  
        // requestStream.Close();  
    }

    // ffmpeg -re -i thelegend.mp4 -c copy -f mpegts udp://192.168.0.5:1234
    void StreamOutputFile()
    {
        var opt = " -i http://123.176.34.172:8090/test1.mpg -g 60 -map 0 -vcodec rawvideo -f segment -reset_timestamps 1 -segment_format rawvideo -pix_fmt rgb24 " + Application.persistentDataPath
                    + "/out%03d.seg";

        string path = "out%03d.mp4";

#if UNITY_EDITOR
        path = Application.dataPath.Replace("/Assets", "") + "/" + path;
#elif UNITY_STANDALONE
		path = Application.streamingAssetsPath + "/" + path;
#endif

        // print(path);

        // opt = "-y -i udp://192.168.0.101:1234 -g 60 -map 0 -f segment -reset_timestamps 1 " + path;
        opt = "-y -i http://123.176.34.172:8090/test1.mpg -f segment -reset_timestamps 1 " + path;

        var info = new ProcessStartInfo(ffmpegPath, opt);

        info.UseShellExecute = false;
        info.CreateNoWindow = false;
        // info.RedirectStandardInput = false;
        info.RedirectStandardOutput = true;
        // info.RedirectStandardError = true;

        subprocess = new Process();
        subprocess.StartInfo = info;
        subprocess.EnableRaisingEvents = true;
        subprocess.Exited += new EventHandler(ProcessExited);
        subprocess.Disposed += new EventHandler(ProcessDisposed);
        subprocess.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
        subprocess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

        subprocess.Start();

        subprocess.PriorityClass = ProcessPriorityClass.RealTime;

        // print(System.Diagnostics.PerformanceCounterCategory.Exists("Performance Counter"));
        // performanceCounter = new PerformanceCounter();
        // performanceCounter.CategoryName = "Process";
        // performanceCounter.CounterName = "% Processor Time";
        // performanceCounter.InstanceName = subprocess.ProcessName;

        // print(subprocess.ProcessName);

        // subprocess.BeginOutputReadLine();
        // subprocess.BeginErrorReadLine();

        stdout = new BinaryReader(subprocess.StandardOutput.BaseStream);

        // AsyncExtract();

        // subprocess.WaitForExit();
    }


    int numData = 921600;
    byte[] data = new byte[921600];
    int count = 0;

    private void AsyncExtract()
    {
        subprocess.StandardOutput.BaseStream.BeginRead(
                data, 0, numData,
                new AsyncCallback(StandardOutput_AsyncCallBack),
                null
                );
    }

    private void StandardOutput_AsyncCallBack(IAsyncResult asyncResult)
    {
        int stdoutreadlength = subprocess.StandardOutput.BaseStream.EndRead(asyncResult);
        if (stdoutreadlength == 0)
        {
            print("Stream Closed");
            subprocess.StandardOutput.BaseStream.Close();
        }
        else
        {
            AsyncExtract();
        }
    }

    static void stderrReeader_DataReceivedEvent(object sender, DataReceived e)
    {
        print(e.Data);
    }

    static void stdoutReader_DataReceivedEvent(object sender, DataReceived e)
    {
        print(e.Data + "  " + e.Data.Length);
    }

    void OnDestroy()
    {
        // FileImporter.onNewFileAdded -= NewFileAdded;

        subprocess.Kill();

        if (opnFile != null)
            opnFile.Close();
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


    void Update()
    {
        if (Input.GetKeyUp(KeyCode.A))
        {
            StartCoroutine("OpenFile");
            // print(opnFile.Length);
        }

        // string output = subprocess.StandardOutput.ReadToEnd();
        // print(output);

        //Marshal.Copy(subprocess.MainWindowHandle, data, 0, 2123);

        // try
        // {
        //     // data = stdout.ReadInt32();

        //     subprocess.StandardOutput.BaseStream.Read(data, 0, numData);

        //     texture.LoadRawTextureData(data);
        //     texture.Apply();

        //     rawImage.texture = texture;

        //     // print(subprocess.StandardOutput.BaseStream.Read(data, 0, numData));
        //     // subprocess.StandardOutput.BaseStream.Flush();

        //     // print("Output " + stdout.ReadString());

        //     // print(Time.time);
        // }
        // catch (Exception exp)
        // {
        //     print(exp.Message);
        // }
    }
}
