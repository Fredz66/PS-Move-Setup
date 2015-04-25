// TODO: create a more realistic and at scale 3D model for the PS Move
// TODO: separate the elements from the DK2 3D model and give them a specific material (plastic, glass, foam)

//#define PSMOVESETUP_TEST

using UnityEngine;
using System;
using System.Collections.Generic;

public class MoveController : MonoBehaviour 
{
	public List<MoveManager> moves = new List<MoveManager>();
	
	HornCoregistration Coregistration = new HornCoregistration();
	
	public Vector3 DisplayedPosition;
	public Matrix4x4 TransformMatrix;
	public Vector3 CorrectedPosition;
	public Vector3 RotatedPosition;
	public Vector3 DummyPosition;
	
	private DK2Controller dk2;
	
	GUIStyle style;

	void Start() 
	{
		style = new GUIStyle();
		style.normal.textColor = Color.black;

#if PSMOVESETUP_TEST
		DisplayedPosition = new Vector3(0.0f, 1.0f, 0.0f);
		TransformMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
#else
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
			}
		}
#endif
	}
	
	void Update() 
	{	
		dk2 = GameObject.FindWithTag("DK2").GetComponent<DK2Controller>();

#if PSMOVESETUP_TEST
		float range = 0.0f;
		Quaternion dummyRotation;
		Vector3 dummyTranslation;
		
		if (range == 0.0f) {
			//dummyRotation = Quaternion.Euler(47, 22, 13);
			dummyRotation = Quaternion.Euler(0, 90, 0);
			dummyTranslation = new Vector3(0.1f, 0.2f, 0.3f);
		} else {
			dummyRotation = Quaternion.Euler(
				47 + UnityEngine.Random.Range(-range, range),
				22 + UnityEngine.Random.Range(-range, range),
				13 + UnityEngine.Random.Range(-range, range)
				//90 + UnityEngine.Random.Range(-range, range),
				//0 + UnityEngine.Random.Range(-range, range),
				//0 + UnityEngine.Random.Range(-range, range)
			);

			dummyTranslation = new Vector3(
				0.1f + UnityEngine.Random.Range(-range, range),
				0.2f + UnityEngine.Random.Range(-range, range),
				0.3f + UnityEngine.Random.Range(-range, range)
			);
		}

		DummyPosition = dummyRotation * dk2.Position + dummyTranslation;

		CorrectedPosition = TransformMatrix.MultiplyPoint3x4(DummyPosition);

		transform.position = CorrectedPosition + DisplayedPosition;

		RotatedPosition = DummyPosition;
		//RotatedPosition = Quaternion.Inverse(dk2.Orientation) * (DummyPosition - dk2.Position) + dk2.Position;

		if (Coregistration.Correlations.Count == 300 && !Coregistration.IsRegistered) {
			Coregistration.ComputeTransformMatrix();
			TransformMatrix = Coregistration.GetTransformMatrix().inverse;
		}

		Coregistration.AddCorrelation(0, dk2.Position, RotatedPosition);
#else
		foreach (MoveManager move in moves) 
		{
			if (move.Disconnected) continue;

			CorrectedPosition = TransformMatrix.MultiplyPoint3x4(move.Position);
			
			//transform.rotation = Quaternion.Euler(0, -90, 0) * move.Orientation * Quaternion.Euler(-90, 0, 0);
			transform.rotation = move.Orientation;
			transform.position = CorrectedPosition + DisplayedPosition;

			if (move.GetButtonDown(PSMoveButton.Move)) {
				move.ResetOrientation();
			}

			// Correction of the position of the PS Move to cancel out the rotation from the DK2.
			RotatedPosition = Quaternion.Inverse(dk2.Orientation) * (move.Position - dk2.Position) + dk2.Position;

			float angle = Quaternion.Angle(dk2.Orientation, Quaternion.AngleAxis(0, Vector3.forward));
			//if (angle < 5.0f) {
				if (Coregistration.Correlations.Count == 300 && !Coregistration.IsRegistered) {
					Coregistration.ComputeTransformMatrix();
					TransformMatrix = Coregistration.GetTransformMatrix().inverse;
				}

				Coregistration.AddCorrelation(angle, dk2.Position, RotatedPosition);
			//}
		}
#endif
	}
	
	void HandleControllerDisconnected (object sender, EventArgs e)
	{
	}

	void OnGUI() 
	{
#if PSMOVESETUP_TEST
		string display = string.Format(
			"{0} : " +
			"Dummy (cm): {1:0.0} {2:0.0} {3:0.0} - " +
			"DK2 (cm): {4:0.0} {5:0.0} {6:0.0} - " +
			"Diff (mm): {7:0} {8:0} {9:0} - " +
			"CorrDiff (mm): {10:0} {11:0} {12:0} - " +
			"Rotation (deg): {13:0} {14:0} {15:0} ",
			Coregistration.Correlations.Count,
			DummyPosition.x * 100, DummyPosition.y * 100, DummyPosition.z * 100,
			dk2.Position.x * 100, dk2.Position.y * 100, dk2.Position.z * 100,
			(DummyPosition.x - dk2.Position.x) * 1000,
			(DummyPosition.y - dk2.Position.y) * 1000,
			(DummyPosition.z - dk2.Position.z) * 1000,
			(CorrectedPosition.x - dk2.Position.x) * 1000,
			(CorrectedPosition.y - dk2.Position.y) * 1000,
			(CorrectedPosition.z - dk2.Position.z) * 1000,
			dk2.Orientation.eulerAngles.x, dk2.Orientation.eulerAngles.y, dk2.Orientation.eulerAngles.z
		);
		
		GUI.Label(new Rect(10, Screen.height - 20, 500, 100), display, style);

		if (Coregistration.IsRegistered) {
			string display_registration = string.Format(
				"Rotation (deg): {0} - Translation (cm): {1}",
				Coregistration.GetRotation().eulerAngles, Coregistration.GetTranslation());

			GUI.Label(new Rect(10, Screen.height - 40, 500, 100), display_registration, style);
		}
#else	
		foreach (MoveManager move in moves) 
		{
			string display = string.Format(
				"{0} : " +
				"Move (cm): {1:0.0} {2:0.0} {3:0.0} - " +
				"DK2 (cm): {4:0.0} {5:0.0} {6:0.0} - " +
				"Diff (cm): {7:0.0} {8:0.0} {9:0.0} - " +
				"CorrDiff (cm): {10:0.0} {11:0.0} {12:0.0} - " +
				"Rotation (deg): {13:0} {14:0} {15:0} ",
				Coregistration.Correlations.Count,
				move.Position.x * 100, move.Position.y * 100, move.Position.z * 100,
				dk2.Position.x * 100, dk2.Position.y * 100, dk2.Position.z * 100,
				(move.Position.x - dk2.Position.x) * 100,
				(move.Position.y - dk2.Position.y) * 100,
				(move.Position.z - dk2.Position.z) * 100,
				(CorrectedPosition.x - dk2.Position.x) * 100,
				(CorrectedPosition.y - dk2.Position.y) * 100,
				(CorrectedPosition.z - dk2.Position.z) * 100,
				dk2.Orientation.eulerAngles.x, dk2.Orientation.eulerAngles.y, dk2.Orientation.eulerAngles.z
			);
			
			GUI.Label(new Rect(10, Screen.height - 20, 500, 100), display, style);

			if (Coregistration.IsRegistered) {
				string display_registration = string.Format(
					"Rotation (deg): {0} - Translation (cm): {1}",
					Coregistration.GetRotation().eulerAngles, Coregistration.GetTranslation());

				GUI.Label(new Rect(10, Screen.height - 40, 500, 100), display_registration, style);
			}
		}
#endif
	}
	
	void OnApplicationQuit() {
		if (Coregistration.Correlations.Count > 0) {
			// TODO: Save transform matrix in C:\Users\<User>\AppData\Roaming\.psmoveapi\dk2registration for later use.
		} else {
			Debug.Log("No valid positions tracked, can't compute the transform matrix!");
		}
	}
}