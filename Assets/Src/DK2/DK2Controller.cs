using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using Ovr;

public class DK2Controller : MonoBehaviour {

	private static Hmd _capiHmd;
	private static bool ovrIsInitialized;
	
	private Vector3 DisplayedPosition;

	public Vector3 Position;
	public Quaternion Orientation;

	public static Hmd capiHmd
	{
		get {
			if (_capiHmd == null)
			{
				IntPtr hmdPtr = IntPtr.Zero;
				OVR_GetHMD(ref hmdPtr);
				_capiHmd = (hmdPtr != IntPtr.Zero) ? new Hmd(hmdPtr) : null;
			}
			return _capiHmd;
		}
	}

	void Start () {
		if (!ovrIsInitialized)
		{
			DisplayedPosition = new Vector3(0.0f, 1.0f, 0.0f);
			OVR_Initialize();
			ovrIsInitialized = true;
		}
	}

	void Update () {
		if (ovrIsInitialized)
		{
			OVRPose pose = capiHmd.GetTrackingState().HeadPose.ThePose.ToPose();

			Position = pose.position;
			Orientation = pose.orientation;

			transform.rotation = pose.orientation;
			transform.position = pose.position + DisplayedPosition;
		}
	}

	private void OnDisable()
	{
		if (ovrIsInitialized)
		{
			OVR_Destroy();
			_capiHmd = null;
			ovrIsInitialized = false;
		}
	}

    private const string LibOVR = "OculusPlugin";

	[DllImport(LibOVR, CallingConvention = CallingConvention.Cdecl)]
	private static extern void OVR_GetHMD(ref IntPtr hmdPtr);

	[DllImport(LibOVR, CallingConvention = CallingConvention.Cdecl)]
	private static extern void OVR_Initialize();

	[DllImport(LibOVR, CallingConvention = CallingConvention.Cdecl)]
	private static extern void OVR_Destroy();
}