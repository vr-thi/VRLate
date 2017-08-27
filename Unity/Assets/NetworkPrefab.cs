using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace VRLate
{
	public class NetworkPrefab : NetworkBehaviour
	{
		private GameObject cam;
		private VRLate vrLate;

		void Awake ()
		{
			cam = GameObject.FindGameObjectWithTag ("MainCamera");
			vrLate = cam.GetComponent<VRLate> ();
		}

		void Start ()
		{
			if (isLocalPlayer) {
				this.name = "Synced Object (Local User)";
			} else {
				this.name = "Synced Object (Remote User)";
				vrLate.trackedObject = this.gameObject;
			}
		}

		void FixedUpdate ()
		{
			if (isLocalPlayer) {
				this.transform.position = cam.transform.position;
				this.transform.rotation = cam.transform.rotation;
			}
		}
	}
}