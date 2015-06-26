using UnityEngine;
using System;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra.Double.Solvers;
using MathNet.Numerics.LinearAlgebra.Solvers;

public struct Correlation
{
	public Vector3 HelmetPosition;
	public Quaternion HelmetRotation;
	public Vector3 ControllerPosition;
	public Quaternion ControllerRotation;
	public double Distance;
	public double AbsoluteDistance;
}

class ControllerRegistration : IEnumerable<Correlation>
{
    public List<Correlation> Correlations = new List<Correlation>();

	protected Matrix4x4 GlobalTransform;
	protected Matrix4x4 LocalTransform;

    public bool IsRegistered = false;

    public IEnumerator<Correlation> GetEnumerator()
    {
        return Correlations.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Correlations.GetEnumerator();
    }

	// Add a correlation betwen a helmet pose and a controller position.
    public void AddCorrelation(Vector3 helmetPosition, Quaternion helmetRotation, Vector3 controllerPosition)
    {
        Correlation correlation = new Correlation();

        correlation.HelmetPosition = helmetPosition;
        correlation.HelmetRotation = helmetRotation;
        correlation.ControllerPosition = controllerPosition;
        correlation.Distance = Math.Sqrt(
            Math.Pow(helmetPosition.x - controllerPosition.x, 2) +
            Math.Pow(helmetPosition.y - controllerPosition.y, 2) +
            Math.Pow(helmetPosition.z - controllerPosition.z, 2)
        );

        Correlations.Add(correlation);
    }

    // Remove outliers in the correlation list using the absolute deviation around the median.
    // References :
    // - https://www.academia.edu/5324493/
    // - http://rspublication.com/ijeted/2014/july14/8.pdf
    public void RemoveOutliers()
    {
		double median, mad;
		double averageRank = (Correlations.Count + 1) / 2.0f;
		int minRank = (int)Math.Floor(averageRank) - 1;
		int maxRank = (int)Math.Ceiling(averageRank) - 1;

        // Sort the list of positions on the ascending distance
		Correlations.Sort((x, y) => x.Distance.CompareTo(y.Distance));

		// Calculate the median
		if (Correlations.Count % 2 != 0) {
			median = Correlations[(Correlations.Count - 1) / 2].Distance;
		} else {
			median = (Correlations[minRank].Distance + Correlations[maxRank].Distance) / 2;
		}

		// Calculate the absolute distances
		for (int i = 0 ; i < Correlations.Count ; i++) {
			Correlation correlation = Correlations[i];
			correlation.AbsoluteDistance = Math.Abs(correlation.Distance - median);
			Correlations[i] = correlation;
		}

		// Sort the list of positions on the ascending absolute distance
		Correlations.Sort((x, y) => x.AbsoluteDistance.CompareTo(y.AbsoluteDistance));

		// Calculate the median absolute deviation (MAD)
		if (Correlations.Count % 2 != 0) {
			mad = 1.4826 * Correlations[(Correlations.Count - 1) / 2].AbsoluteDistance;
		} else {
			mad = 1.4826 * ((Correlations[minRank].AbsoluteDistance + Correlations[maxRank].AbsoluteDistance) / 2);
		}
		
        // Remove outliers
		for (int i = Correlations.Count - 1 ; i >= 0 ; i--)
		{
			if (Correlations[i].AbsoluteDistance > 3 * mad)
			{
                Correlations.RemoveAt(i);
            }
		}
    }

	// QR15 calibration algorithm (AX = YB without knowing rotation of the controller)
	// From "Non-orthogonal tool/flange and robot/world calibration" - Ernst 2012
    public void ComputeTransforms()
    {
		int n = Correlations.Count;

		// Build homogenous matrices for the poses.
		Matrix<double> Ai = Matrix<double>.Build.Dense(4, n * 4);
		Matrix<double> Bi = Matrix<double>.Build.Dense(4, n * 4);

		for (int i = 0 ; i < n ; i++)
		{
			var h = Matrix4x4.TRS(Correlations[i].HelmetPosition, Correlations[i].HelmetRotation, Vector3.one);
			var c = Matrix4x4.TRS(Correlations[i].ControllerPosition, Quaternion.identity, Vector3.one);

			for (int k = 0 ; k < 4 ; k++) {
				for (int j = 0 ; j < 4 ; j++) {
					Ai[j, i * 4 + k] = h[j, k];
					Bi[j, i * 4 + k] = c[j, k];
				}
			}
			//Debug.Log("H" + i + ": " + h);
			//Debug.Log("C" + i + ": " + c);
		}

		// Compute the transformation matrices.
		Matrix<double> A = Matrix<double>.Build.Dense(3 * n, 15);
		Matrix<double> b = Matrix<double>.Build.Dense(3 * n, 1);
		Matrix<double> eye3 = new DiagonalMatrix(3, 3, 1.0);

		for (int i = 0 ; i < n ; i++)
		{
			var RMi = Ai.SubMatrix(0, 3, i * 4, 3).Transpose();
			var Ti = Bi.SubMatrix(0, 4, i * 4, 4);

			A.SetSubMatrix(i * 3, 0, RMi * Ti[0,3]);
			A.SetSubMatrix(i * 3, 3, RMi * Ti[1,3]);
			A.SetSubMatrix(i * 3, 6, RMi * Ti[2,3]);
			A.SetSubMatrix(i * 3, 9, RMi);
			A.SetSubMatrix(i * 3, 12, -eye3);

			b.SetSubMatrix(i * 3, 0, RMi * Ai.SubMatrix(0, 3, i * 4 + 3, 1));
		}

//Debug.Log("A: " + A);
//Debug.Log("b: " + b);

		var x = A.Solve(b.Column(0));

		GlobalTransform = Matrix4x4.identity;

		GlobalTransform[0,0] = (float)x[0];
		GlobalTransform[1,0] = (float)x[1];
		GlobalTransform[2,0] = (float)x[2];

		GlobalTransform[0,1] = (float)x[3];
		GlobalTransform[1,1] = (float)x[4];
		GlobalTransform[2,1] = (float)x[5];

		GlobalTransform[0,2] = (float)x[6];
		GlobalTransform[1,2] = (float)x[7];
		GlobalTransform[2,2] = (float)x[8];

		GlobalTransform[0,3] = (float)x[9];
		GlobalTransform[1,3] = (float)x[10];
		GlobalTransform[2,3] = (float)x[11];

		LocalTransform = Matrix4x4.identity;

		LocalTransform[0,3] = (float)x[12];
		LocalTransform[1,3] = (float)x[13];
		LocalTransform[2,3] = (float)x[14];

Debug.Log("Global: " + GlobalTransform);
Debug.Log("Local: " + LocalTransform);

		IsRegistered = true;
    }
	
	public void Compute()
	{
		RemoveOutliers();

		ComputeTransforms();

		IsRegistered = true;
	}
	
	public Matrix4x4 GetGlobalTransform()
	{
		return GlobalTransform;
	}

	public Matrix4x4 GetLocalTransform()
	{
		return LocalTransform;
	}
}