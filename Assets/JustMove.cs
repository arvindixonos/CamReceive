using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class JustMove : MonoBehaviour {

	// Use this for initialization
	void Start () {
		transform.DOKill();
		transform.DOMoveX(-10, 1f).SetLoops(-1, LoopType.Yoyo);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
