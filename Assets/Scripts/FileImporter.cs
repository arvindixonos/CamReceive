using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FileImporter : MonoBehaviour
{

    public FileSystemWatcher fileSystemWatcher;

    public delegate void NewFileAdded(string fileName);
    public event NewFileAdded onNewFileAdded;

    private FileSystemEventHandler eventHandler;

    public string path = "OutputVideo/";

    public string filter = "*.mp4";

    void Awake()
    {

#if UNITY_STANDALONE
        if (!Directory.Exists(Application.streamingAssetsPath + "/" + path))
            Directory.CreateDirectory(Application.streamingAssetsPath + "/" + path);
#endif

#if UNITY_EDITOR
        path = Application.dataPath.Replace("/Assets", "") + "/" + path;
#elif UNITY_STANDALONE
        path = Application.streamingAssetsPath + "/" + path;
#endif
        eventHandler = new FileSystemEventHandler(OnCreated);
        fileSystemWatcher = new FileSystemWatcher(path);
        fileSystemWatcher.Created += eventHandler;
        fileSystemWatcher.Filter = filter;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    void DeleteOldFiles(string path)
    {
        string[] allFiles = Directory.GetFiles(path, filter, SearchOption.TopDirectoryOnly);
        foreach (string filePath in allFiles)
        {
            File.Delete(filePath);
        }
    }

    void Start()
    {
        DeleteOldFiles(path);
    }

    void OnDestroy()
    {
        fileSystemWatcher.Created -= eventHandler;
        fileSystemWatcher.EnableRaisingEvents = false;
        //Destroy(fileSystemWatcher);
        print("Destroyed");
    }

    void OnApplicationQuit()
    {
        if (fileSystemWatcher != null)
        {
            fileSystemWatcher.Created -= eventHandler;
            fileSystemWatcher.Changed -= eventHandler;
            fileSystemWatcher.EnableRaisingEvents = false;
        }
        print("App Quit");
    }

    void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (onNewFileAdded != null)
        {
            onNewFileAdded(e.FullPath);
        }
    }
}
