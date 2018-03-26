using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace WaveOutTestCSharp
{
    class Program
    {
        SoundStreamReceiver soundStreamReceiver = new SoundStreamReceiver();

        static Program p;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnProcessExit);

            string fullArgs = System.Environment.CommandLine.Replace("\"" + Environment.GetCommandLineArgs()[0] + "\"", "");

            p = new Program();
            p.StartAudio(fullArgs);
        }

        public void StartAudio(string ffmpegParams)
        {
            soundStreamReceiver.StartReceivingAudio(ffmpegParams);
        }

        public void StopAudio()
        {
            soundStreamReceiver.StopReceivingAudio();
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            p.StopAudio();
        }
    }
}
