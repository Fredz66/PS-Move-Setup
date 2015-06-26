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

	public Vector3 Accelerometer;
	public Vector3 Gyro;
	public Vector3 Magnetometer;
	public Quaternion MARGOrientation;

	private AHRS.MadgwickAHRS AHRS;

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
			//AHRS = new AHRS.MadgwickAHRS(1f / 75f, 0.1f);
			AHRS = new AHRS.MadgwickAHRS(1f / 75f, 0.033f);
		}
	}

	void Update () {
		if (ovrIsInitialized)
		{
			var raw = capiHmd.GetTrackingState().RawSensorData;
			Accelerometer.Set(raw.Accelerometer.x, raw.Accelerometer.y, raw.Accelerometer.z);
			Gyro.Set(raw.Gyro.x, raw.Gyro.y, raw.Gyro.z);
			Magnetometer.Set(raw.Magnetometer.x, raw.Magnetometer.y, raw.Magnetometer.z);
		
			// For Unity 5.1 see : http://docs.unity3d.com/ScriptReference/VR.InputTracking.html
			OVRPose pose = capiHmd.GetTrackingState().HeadPose.ThePose.ToPose();

			Position = pose.position;
			Orientation = pose.orientation;

			AHRS.Update(
				Gyro.x, Gyro.y, Gyro.z,
				Accelerometer.x, Accelerometer.y, Accelerometer.z,
				Magnetometer.x, Magnetometer.y, Magnetometer.z);

			MARGOrientation.Set(AHRS.Quaternion[1], AHRS.Quaternion[2], AHRS.Quaternion[3], AHRS.Quaternion[0]);
			//transform.rotation = MARGOrientation;

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