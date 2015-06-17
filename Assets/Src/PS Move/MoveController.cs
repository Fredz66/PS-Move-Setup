// TODO: create a more realistic and at scale 3D model for the PS Move
// TODO: separate the elements from the DK2 3D model and give them a specific material (plastic, glass, foam)

using UnityEngine;
using System;
using System.Collections.Generic;

public class MoveController : MonoBehaviour 
{
	public List<MoveManager> moves = new List<MoveManager>();
	
	ControllerRegistration Registration = new ControllerRegistration();
	
	public Vector3 DisplayedPosition;
	public Vector3 CorrectedPosition;

	public Vector3 Accelerometer;
	public Vector3 Gyro;
	public Vector3 Magnetometer;
	public Quaternion MARGOrientation;

	public Matrix4x4 TransformMatrix;
	public Matrix4x4 LocalTransform;
	public Matrix4x4 GlobalTransform;
	
	private DK2Controller dk2;
	
	private AHRS.MadgwickAHRS AHRS;
	
	GUIStyle style;

	void Start() 
	{
		style = new GUIStyle();
		style.normal.textColor = Color.black;

		int count = MoveManager.GetNumConnected();
		
		for (int i = 0; i < count; i++) 
		{
			MoveManager move = gameObject.AddComponent<MoveManager>();
			
			if (!move.Init(i)) 
			{	
				Destroy(move);
				continue;
			}
			
			PSMoveConnectionType conn = move.ConnectionType;

			if (conn == PSMoveConnectionType.Unknown || conn == PSMoveConnectionType.USB) 
			{
				Destroy(move);
			}
			else 
			{
				moves.Add(move);
				move.OnControllerDisconnected += HandleControllerDisconnected;
				move.SetLED(Color.cyan);
				
				DisplayedPosition = new Vector3(0.0f, 1.0f, 0.0f);
				TransformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

				// The proposed algorithm’s adjustable parameter, ß, was set to 0.033 for the MARG 
				// implementation and 0.041 for the IMU implementation.				
				//AHRS = new AHRS.MadgwickAHRS(1f / 75f, 0.1f);
				AHRS = new AHRS.MadgwickAHRS(1f / 75f, 0.033f);
			}
		}
	}
	
	void Update() 
	{	
		dk2 = GameObject.FindWithTag("DK2").GetComponent<DK2Controller>();

		foreach (MoveManager move in moves) 
		{
			if (move.Disconnected) continue;

			CorrectedPosition = TransformMatrix.MultiplyPoint3x4(move.Position);

			/*Accelerometer.Set(move.Acceleration.x, move.Acceleration.y, move.Acceleration.z);
			Gyro.Set(move.Gyro.x, move.Gyro.y, move.Gyro.z);
			Magnetometer.Set(move.Magnetometer.x, move.Magnetometer.y, move.Magnetometer.z);*/

			/*Accelerometer.Set(-move.Acceleration.y * 10, move.Acceleration.z * 10, move.Acceleration.x * 10);
			Gyro.Set(move.Gyro.x, move.Gyro.z, move.Gyro.y);
			Magnetometer.Set(-move.Magnetometer.y / 350, -move.Magnetometer.z / 350, -move.Magnetometer.x / 350);*/
			
			Accelerometer.Set(move.Acceleration.x * 10, move.Acceleration.y * 10, move.Acceleration.z * 10);
			Gyro.Set(move.Gyro.x, move.Gyro.y, move.Gyro.z);
			Magnetometer.Set(move.Magnetometer.x / 350, -move.Magnetometer.y / 350, move.Magnetometer.z / 350);

			AHRS.Update(
				Gyro.x, Gyro.y, Gyro.z,
				Accelerometer.x, Accelerometer.y, Accelerometer.z,
				Magnetometer.x, Magnetometer.y, Magnetometer.z);

			MARGOrientation.Set(AHRS.Quaternion[1], AHRS.Quaternion[2], AHRS.Quaternion[3], AHRS.Quaternion[0]);
			transform.rotation = MARGOrientation;

/*Debug.Log(
	"Move: " + Accelerometer + " - " + Gyro * 10 + " - " + Magnetometer +
	" DK2: " + dk2.Accelerometer + " - " + dk2.Gyro * 10 + " - " + dk2.Magnetometer * 100);

Debug.Log(
	"Move: " + Accelerometer.sqrMagnitude + " - " + Gyro.sqrMagnitude + " - " + Magnetometer.sqrMagnitude +
	" DK2: " + dk2.Accelerometer.sqrMagnitude + " - " + dk2.Gyro.sqrMagnitude + " - " + dk2.Magnetometer.sqrMagnitude);*/

Debug.Log("Move: " + Accelerometer + " - " + Gyro * 10 + " - " + Magnetometer);
Debug.Log("DK2: " + dk2.Accelerometer + " - " + dk2.Gyro * 10 + " - " + dk2.Magnetometer);

Debug.Log(
	(dk2.Accelerometer.sqrMagnitude / Accelerometer.sqrMagnitude) + " - " +
	(dk2.Gyro.sqrMagnitude / Gyro.sqrMagnitude) + " - " +
	(Magnetometer.sqrMagnitude / dk2.Magnetometer.sqrMagnitude));
			
			//transform.rotation = move.Orientation;
			transform.position = CorrectedPosition + DisplayedPosition;

			if (move.GetButtonDown(PSMoveButton.Move)) {
				move.ResetOrientation();
			}
			
			if (Registration.Correlations.Count == 300 && !Registration.IsRegistered) {
				Registration.Compute();
				GlobalTransform = Registration.GetGlobalTransform();
				LocalTransform = Registration.GetLocalTransform();
Debug.Log("GlobalTransform: " + GlobalTransform);
Debug.Log("LocalTransform: " + LocalTransform);
				TransformMatrix = GlobalTransform;
			}

			Registration.AddCorrelation(dk2.Position, dk2.Orientation, move.Position);
		}
	}
	
	void HandleControllerDisconnected (object sender, EventArgs e)
	{
	}

	void OnGUI() 
	{
		foreach (MoveManager move in moves) 
		{
			// FIXME: probably incorrect, the order of multiplication should probably be inversed?
			// Compute AX to show the difference between AX and YB which should be used to compute the RMS error.
			Vector3 ZeroPosition = LocalTransform.MultiplyPoint3x4(dk2.Position);

			string display = string.Format(
				"{0} : " +
				"Move (cm): {1:0.0} {2:0.0} {3:0.0} - " +
				"DK2 (cm): {4:0.0} {5:0.0} {6:0.0} - " +
				"Diff (cm): {7:0.0} {8:0.0} {9:0.0} - " +
				"CorrDiff (cm): {10:0.0} {11:0.0} {12:0.0}",
				Registration.Correlations.Count,
				move.Position.x * 100, move.Position.y * 100, move.Position.z * 100,
				dk2.Position.x * 100, dk2.Position.y * 100, dk2.Position.z * 100,
				(move.Position.x - dk2.Position.x) * 100,
				(move.Position.y - dk2.Position.y) * 100,
				(move.Position.z - dk2.Position.z) * 100,
				(CorrectedPosition.x - ZeroPosition.x) * 100,
				(CorrectedPosition.y - ZeroPosition.y) * 100,
				(CorrectedPosition.z - ZeroPosition.z) * 100
			);
			
			GUI.Label(new Rect(10, Screen.height - 20, 500, 100), display, style);

			/*if (Coregistration.IsRegistered) {
				display = string.Format(
					"Rotation (deg): {0} - Translation (cm): {1}",
					Coregistration.GetRotation().eulerAngles, Coregistration.GetTranslation());

				GUI.Label(new Rect(10, Screen.height - 40, 500, 100), display, style);
			}*/
			/*if (Calibration.IsRegistered) {
				display = string.Format(
					"Rotation (deg): {0} - Translation (cm): {1}",
					Coregistration.GetRotation().eulerAngles, Coregistration.GetTranslation());

				GUI.Label(new Rect(10, Screen.height - 40, 500, 100), display, style);
			}*/
		}
	}
	
	void OnApplicationQuit() {
		//if (Coregistration.Correlations.Count > 0) {
		if (Registration.Correlations.Count > 0) {
			// TODO: Save transform matrix in C:\Users\<User>\AppData\Roaming\.psmoveapi\dk2registration for later use.
		} else {
			Debug.Log("No valid positions tracked, can't compute the transform matrix!");
		}
	}

	public Matrix4x4 rigidMatrix(float tx, float ty, float tz, float rx, float ry, float rz, bool right)
	{
		rx = rx * Mathf.Deg2Rad / 2.0f;
		ry = ry * Mathf.Deg2Rad / 2.0f;
		rz = rz * Mathf.Deg2Rad / 2.0f;

        float C1 = Mathf.Cos(rx);
        float S1 = Mathf.Sin(rx);
        float C2 = Mathf.Cos(ry);
        float S2 = Mathf.Sin(ry);
        float C3 = Mathf.Cos(rz);
        float S3 = Mathf.Sin(rz);

		Quaternion q = Quaternion.identity;

        q.w = C1 * C2 * C3 + S1 * S2 * S3;
        q.x = S1 * C2 * C3 + C1 * S2 * S3;
        q.y = C1 * S2 * C3 - S1 * C2 * S3;
        q.z = C1 * C2 * S3 - S1 * S2 * C3;

		return TRS(new Vector3(tx, ty, tz), q, right);
	}

	public Matrix4x4 TRS(Vector3 p, Quaternion r, bool right)
	{
		Matrix4x4 m = Matrix4x4.zero;

		if (right)
		{
			m[0,0] = 1 - 2*r.y*r.y - 2*r.z*r.z;
			m[0,1] = 2*r.x*r.y + 2*r.w*r.z;
			m[0,2] = 2*r.x*r.z - 2*r.w*r.y;

			m[1,0] = 2*r.x*r.y - 2*r.w*r.z;
			m[1,1] = 1 - 2*r.x*r.x - 2*r.z*r.z;
			m[1,2] = 2*r.y*r.z + 2*r.w*r.x;

			m[2,0] = 2*r.x*r.z + 2*r.w*r.y;
			m[2,1] = 2*r.y*r.z - 2*r.w*r.x;
			m[2,2] = 1 - 2*r.x*r.x - 2*r.y*r.y;
		} else {
			m[0,0] = 1 - 2*r.y*r.y - 2*r.z*r.z;
			m[0,1] = 2*r.x*r.y - 2*r.w*r.z;
			m[0,2] = 2*r.x*r.z + 2*r.w*r.y;

			m[1,0] = 2*r.x*r.y + 2*r.w*r.z;
			m[1,1] = 1 - 2*r.x*r.x - 2*r.z*r.z;
			m[1,2] = 2*r.y*r.z - 2*r.w*r.x;

			m[2,0] = 2*r.x*r.z - 2*r.w*r.y;
			m[2,1] = 2*r.y*r.z + 2*r.w*r.x;
			m[2,2] = 1 - 2*r.x*r.x - 2*r.y*r.y;
		}

		m[0,3] = p.x;
		m[1,3] = p.y;
		m[2,3] = p.z;

		m[3,0] = 0;
		m[3,1] = 0;
		m[3,2] = 0;
		m[3,3] = 1.0f;
		
		return m;
	}
}