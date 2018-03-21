using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebCam : MonoBehaviour {

	private	RawImage	webcamOutput;

	private	WebCamTexture webCamTexture;

	void Start () 
	{
		webcamOutput = GetComponent<RawImage>();

		webCamTexture = new WebCamTexture();

		webcamOutput.texture = webCamTexture;
		webCamTexture.Play();

		webcamOutput.SetNativeSize();
	}
}
