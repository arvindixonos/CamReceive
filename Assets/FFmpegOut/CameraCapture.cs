using UnityEngine;
using System.Diagnostics;
using System.IO;
using System;


namespace FFmpegOut
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("FFmpegOut/Camera Capture")]
    public class CameraCapture : MonoBehaviour
    {
        #region Editable properties

        [SerializeField] bool _setResolution = true;
        [SerializeField] int _width = 1280;
        [SerializeField] int _height = 720;
        [SerializeField] int _frameRate = 30;
        [SerializeField] bool _allowSlowDown = true;
        [SerializeField] FFmpegPipe.Preset _preset;
        [SerializeField] float _startTime = 0;
        [SerializeField] float _recordLength = 5;

        #endregion

        #region Private members

        [SerializeField, HideInInspector] Shader _shader;
        Material _material;

        FFmpegPipe _pipe;
        float _elapsed;

        RenderTexture _tempTarget;
        GameObject _tempBlitter;

        static int _activePipeCount;

        #endregion

        #region MonoBehavior functions

        void OnValidate()
        {
            _startTime = Mathf.Max(_startTime, 0);
            _recordLength = Mathf.Max(_recordLength, 0.01f);
        }

        void OnEnable()
        {
            if (!FFmpegConfig.CheckAvailable)
            {
                print(
                    "ffmpeg.exe is missing. " +
                    "Please refer to the installation instruction. " +
                    "https://github.com/keijiro/FFmpegOut"
                );
                enabled = false;
            }
        }

        void OnDisable()
        {
            if (_pipe != null) ClosePipe();
        }

        void OnDestroy()
        {
            if (_pipe != null) ClosePipe();
        }

        void Start()
        {
            _material = new Material(_shader);

            print(WebCamTexture.devices[0].name + " " + Microphone.devices[0]);
        }

        void Update()
        {
            _elapsed += Time.deltaTime;

            if (_startTime <= _elapsed && _elapsed < _startTime + _recordLength)
            {
                if (_pipe == null) OpenPipe();
            }
            else
            {
                if (_pipe != null) ClosePipe();
            }
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_pipe != null)
            {
                var tempRT = RenderTexture.GetTemporary(source.width, source.height);
                Graphics.Blit(source, tempRT, _material, 0);

                var tempTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                tempTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                tempTex.Apply();

                _pipe.Write(tempTex.GetRawTextureData());



                Destroy(tempTex);
                RenderTexture.ReleaseTemporary(tempRT);
            }

            Graphics.Blit(source, destination);
        }

        #endregion

        #region Private methods

        // // ffmpeg -re -i thelegend.mp4 -c copy -f mpegts udp://192.168.0.5:1234

        // void StreamOutput()
        // {
        //     var opt = "-re -i output.mp4 -c copy -f mpegts udp://192.168.0.5:1234";

        //     var info = new ProcessStartInfo(FFmpegConfig.BinaryPath, opt);

        //     info.UseShellExecute = false;
        //     info.CreateNoWindow = true;
        //     info.RedirectStandardInput = true;
        //     info.RedirectStandardOutput = true;
        //     info.RedirectStandardError = true;

        //     Process _subprocess = Process.Start(info);
        // }

        void OpenPipe()
        {
            if (_pipe != null) return;

            var camera = GetComponent<Camera>();
            var width = _width;
            var height = _height;

            // Apply the screen resolution settings.
            if (_setResolution)
            {
                _tempTarget = RenderTexture.GetTemporary(width, height, 24);
                camera.targetTexture = _tempTarget;
                _tempBlitter = Blitter.CreateGameObject(camera);
            }
            else
            {
                width = camera.pixelWidth;
                height = camera.pixelHeight;
            }

            // Open an output stream.
            _pipe = new FFmpegPipe(name, width, height, _frameRate, _preset);
            _activePipeCount++;

            // Change the application frame rate on the first pipe.
            if (_activePipeCount == 1)
            {
                if (_allowSlowDown)
                    Time.captureFramerate = _frameRate;
                else
                    Application.targetFrameRate = _frameRate;
            }

            print("Capture started (" + _pipe.Filename + ")");
        }

        void ClosePipe()
        {
            var camera = GetComponent<Camera>();

            // Destroy the blitter object.
            if (_tempBlitter != null)
            {
                Destroy(_tempBlitter);
                _tempBlitter = null;
            }

            // Release the temporary render target.
            if (_tempTarget != null && _tempTarget == camera.targetTexture)
            {
                camera.targetTexture = null;
                RenderTexture.ReleaseTemporary(_tempTarget);
                _tempTarget = null;
            }

            // Close the output stream.
            if (_pipe != null)
            {
                print("Capture ended (" + _pipe.Filename + ")");

                _pipe.Close();
                _activePipeCount--;

                if (!string.IsNullOrEmpty(_pipe.Error))
                {
                    print(
                        "ffmpeg returned with a warning or an error message. " +
                        "See the following lines for details:\n" + _pipe.Error
                    );
                }

                _pipe = null;

                // Reset the application frame rate on the last pipe.
                if (_activePipeCount == 0)
                {
                    if (_allowSlowDown)
                        Time.captureFramerate = 0;
                    else
                        Application.targetFrameRate = -1;
                }
            }
        }

        #endregion
    }
}
