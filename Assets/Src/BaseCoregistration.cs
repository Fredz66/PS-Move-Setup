using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public struct Correlation
{
	public double Angle;
	public Vector3 HelmetPosition;
	public Vector3 ControllerPosition;
	public double Distance;
	public double AbsoluteDistance;
}

abstract class BaseCoregistration : IEnumerable<Correlation>
{
    public List<Correlation> Correlations = new List<Correlation>();
    public bool IsClean = false;
    public bool IsRegistered = false;
	
	protected Quaternion Rotation;
	protected Vector3 Translation;
	protected Vector3 Scale;
	protected Matrix4x4 TransformMatrix;

    public Vector3 HelmetCentroid;
    public Vector3 ControllerCentroid;

    public IEnumerator<Correlation> GetEnumerator()
    {
        return Correlations.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Correlations.GetEnumerator();
    }

    public void AddCorrelation(double angle, Vector3 helmetPosition, Vector3 controllerPosition)
    {
        Correlation correlation = new Correlation();

        correlation.Angle = angle;
        correlation.HelmetPosition = helmetPosition;
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

		IsClean = true;
    }

    // Calculate and returns the mean distance between the helmet and the controller.
    public Vector3 GetMeanDistance()
    {
        Vector3 meanDistance = new Vector3(0, 0, 0);

		for (int i = 0 ; i < Correlations.Count ; i++)
		{
            meanDistance.x += (float)Math.Sqrt(
                Math.Pow(Correlations[i].HelmetPosition.x - Correlations[i].ControllerPosition.x, 2));
            meanDistance.y += (float)Math.Sqrt(
                Math.Pow(Correlations[i].HelmetPosition.y - Correlations[i].ControllerPosition.y, 2));
            meanDistance.z += (float)Math.Sqrt(
                Math.Pow(Correlations[i].HelmetPosition.z - Correlations[i].ControllerPosition.z, 2));
		}
		
		meanDistance /= Correlations.Count;
		
		return meanDistance;
    }

    protected void ComputeCentroids()
    {
		HelmetCentroid = Vector3.zero;
		ControllerCentroid = Vector3.zero;

        for (int i = 0 ; i < Correlations.Count ; i++)
        {
            HelmetCentroid += Correlations[i].HelmetPosition;
            ControllerCentroid += Correlations[i].ControllerPosition;
        }

        HelmetCentroid /= Correlations.Count;
        ControllerCentroid /= Correlations.Count;
    }

	public abstract void ComputeRotation();
	public abstract void ComputeTranslation();
	public abstract void ComputeScale();
	
	public void ComputeTransformMatrix()
	{
		RemoveOutliers();
		ComputeCentroids();

		ComputeRotation();
		ComputeTranslation();
		ComputeScale();

		TransformMatrix = Matrix4x4.TRS(Translation, Rotation, Scale);

		IsRegistered = true;
	}

	public Quaternion GetRotation()
	{
		return Rotation;
	}

	public Vector3 GetTranslation()
	{
		return Translation;
	}

	public Vector3 GetScale()
	{
		return Scale;
	}
	
	public Matrix4x4 GetTransformMatrix()
	{
		return TransformMatrix;
	}
}