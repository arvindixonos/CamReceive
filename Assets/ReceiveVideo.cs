using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine.Video;

public class ReceiveVideo : MonoBehaviour {

	private TcpListener server = null;
	private Thread		listenerThread;

	void Start () 
	{
		Application.runInBackground = true;

		//StopCoroutine("StartServerAndReadData");
		//StartCoroutine("StartServerAndReadData");
		// listenerThread = new Thread(new ThreadStart(StartServer));
		// listenerThread.Start();

	
	}

	void OnApplicationQuit()
	{
		// if(listenerThread != null)
		// 	listenerThread.Abort();
	}

	void StartServer ()
	{		
		while(true)
		{
			Thread.Sleep(200);

			try
			{
				UdpClient udpClient = new UdpClient(1234);

				IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.136"), 1234);
				byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint); 

				print(receiveBytes.Length);

				udpClient.Close();
			}
			catch(SocketException exp)
			{
				print(exp.Message);
			}
		}
	}

	void OnDestroy()
	{
		
	}
}
