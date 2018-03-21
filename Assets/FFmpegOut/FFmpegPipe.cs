using System.Diagnostics;
using System.IO;
using System;

namespace FFmpegOut
{
    // A stream pipe class that invokes ffmpeg and connect to it.
    class FFmpegPipe
    {
        #region Public properties

        public enum Preset
        {
            ProRes422,
            ProRes4444,
            H264Default,
            H264Lossless420,
            H264Lossless444,
            VP8Default
        }

        public string Filename { get; private set; }
        public string Error { get; private set; }

        #endregion

        #region Public methods

        public FFmpegPipe(string name, int width, int height, int framerate, Preset preset)
        {
            // name += DateTime.Now.ToString(" yyyy MMdd HHmmss");
            Filename = "output.mp4";//name.Replace(" ", "_") + GetSuffix(preset);

            var opt = "-y -f rawvideo -vcodec rawvideo -pix_fmt rgba";
            opt += " -colorspace bt709";
            opt += " -video_size " + width + "x" + height;
            opt += " -framerate " + framerate;
            opt += " -i - " + GetOptions(preset);
            opt += " " + Filename;

            // http://123.176.34.172:8090/feed1.ffm 

            // opt = "-y -rtbufsize 100M -f dshow -i video=\"Logitech HD Webcam C310\":audio=\"Microphone (HD Webcam C310)\" -f mpegts udp://192.168.0.101:1234  sample.avi";

            opt = "-y -re -rtbufsize 100M -f dshow -i video=\"" + UnityEngine.WebCamTexture.devices[0].name + "\":audio=\"" + UnityEngine.Microphone.devices[0] + "\" http://123.176.34.172:8090/feed1.ffm sample.avi";

            // opt = "-y -re -i testvideo.mp4 -f mpegts udp://192.168.0.101:1234 sample.avi";

            var info = new ProcessStartInfo(FFmpegConfig.BinaryPath, opt);

            UnityEngine.Debug.Log(opt);

            info.UseShellExecute = false;
            info.CreateNoWindow = false;
            info.RedirectStandardInput = false;
            info.RedirectStandardOutput = false;
            info.RedirectStandardError = false;

            _subprocess = Process.Start(info);

            // _subprocess.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputDataReceived);
            // _subprocess.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);
            
            // _stdin = new BinaryWriter(_subprocess.StandardInput.BaseStream);
        }

        void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            UnityEngine.Debug.Log("error " + e.Data);
        }

        void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            UnityEngine.Debug.Log(e.Data);
        }

        public void Write(byte[] data)
        {
            return;

            if (_subprocess == null) return;

            _stdin.Write(data);
            _stdin.Flush();
        }

        public void Close()
        {
            if (_subprocess == null) return;

            // _subprocess.StandardInput.Close();
            // _subprocess.WaitForExit();

            // var outputReader = _subprocess.StandardError;
            // Error = outputReader.ReadToEnd();

            _subprocess.Close();
            _subprocess.Dispose();

            // outputReader.Close();
            // outputReader.Dispose();

            _subprocess = null;
            _stdin = null;
        }

        #endregion

        #region Private members

        Process _subprocess;
        BinaryWriter _stdin;

        static string[] _suffixes = {
            ".mp4",
            ".mp4",
            ".mp4",
            ".mov",
            ".mov",
            ".webm"
        };

        static string[] _options = {
            "-pix_fmt yuv420p",
            "-pix_fmt yuv420p -preset ultrafast -crf 0",
            "-pix_fmt yuv444p -preset ultrafast -crf 0",
            "-c:v prores_ks -pix_fmt yuv422p10le",
            "-c:v prores_ks -pix_fmt yuva444p10le",
            "-c:v libvpx -pix_fmt yuv420p"
        };

        static string GetSuffix(Preset preset)
        {
            return _suffixes[(int)preset];
        }

        static string GetOptions(Preset preset)
        {
            return _options[(int)preset];
        }

        #endregion
    }
}
