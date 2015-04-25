using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Factorization;

class HornCoregistration : BaseCoregistration
{
    // Closed-form solution of the absolute orientation using unit quaternions.
    // References:
    // - http://people.csail.mit.edu/bkph/papers/Absolute_Orientation.pdf
    // - http://www.ncbr.muni.cz/~dave/SiteBinder/docs/thesis.pdf
    // - http://is.muni.cz/th/140435/fi_b/thesis.pdf
    // - http://www.mathworks.com/matlabcentral/fileexchange/26186-absolute-orientation-horn-s-method
    // - http://www.math.unm.edu/~vageli/papers/rmsd17.pdf
    public override void ComputeRotation()
    {
        // Compute the matrix of the sum of products.
		double sxx = 0.0f; double sxy = 0.0f; double sxz = 0.0f;
		double syx = 0.0f; double syy = 0.0f; double syz = 0.0f;
		double szx = 0.0f; double szy = 0.0f; double szz = 0.0f;

        for (int i = 0 ; i < Correlations.Count ; i++)
        {
            Correlation c = Correlations[i];

			sxx += (double)((c.HelmetPosition.x - HelmetCentroid.x) * (c.ControllerPosition.x - ControllerCentroid.x));
			sxy += (double)((c.HelmetPosition.x - HelmetCentroid.x) * (c.ControllerPosition.y - ControllerCentroid.y));
			sxz += (double)((c.HelmetPosition.x - HelmetCentroid.x) * (c.ControllerPosition.z - ControllerCentroid.z));
			syx += (double)((c.HelmetPosition.y - HelmetCentroid.y) * (c.ControllerPosition.x - ControllerCentroid.x));
			syy += (double)((c.HelmetPosition.y - HelmetCentroid.y) * (c.ControllerPosition.y - ControllerCentroid.y));
			syz += (double)((c.HelmetPosition.y - HelmetCentroid.y) * (c.ControllerPosition.z - ControllerCentroid.z));
			szx += (double)((c.HelmetPosition.z - HelmetCentroid.z) * (c.ControllerPosition.x - ControllerCentroid.x));
			szy += (double)((c.HelmetPosition.z - HelmetCentroid.z) * (c.ControllerPosition.y - ControllerCentroid.y));
			szz += (double)((c.HelmetPosition.z - HelmetCentroid.z) * (c.ControllerPosition.z - ControllerCentroid.z));
        }

        // Convert a quaternion to its matrix representation (N).
        var N = Matrix<double>.Build.DenseOfArray(new double[4, 4] {
            {sxx + syy + szz,   syz - szy,          szx - sxz,          sxy - syx},
            {syz - szy,         sxx - syy - szz,    sxy + syx,          szx + sxz},
            {szx - sxz,         sxy + syx,          -sxx + syy - szz,   syz + szy},
            {sxy - syx,         szx + sxz,          syz + szy,          -sxx - syy + szz}
        });
        
        //Debug.Log("N: " + N);

        // Find the eigenvalues of N.
        // References:
        // - http://stackoverflow.com/questions/24719366/getting-an-eigenvector-as-a-vectordouble
        // - http://stackoverflow.com/questions/28057033/how-can-i-cast-vectorcomplex-to-vectordouble
        // - http://www.akiti.ca/Eig4Solv.html
        Evd<double> eigen = N.Evd();

		int index = 0;
		double maxEigenvalue;

        if (eigen.IsSymmetric)
        {
            var eigenvalues = eigen.EigenValues.Map(x => x.Real);
            //Debug.Log("Eigenvalues of N: " + eigenvalues);

			// Find the maximum positive eigenvalue of N.
			maxEigenvalue = eigenvalues[0];
			for (int i = 1 ; i < eigenvalues.Count ; i++)
			{
				if (eigenvalues[i] > maxEigenvalue)
				{
					index = i;
					maxEigenvalue = eigenvalues[i];
				}
			}
			//Debug.Log("Maximum positive eigenvalue of N: " + maxEigenvalue + " (index:" + index + ")");
        } else {
            //Debug.Log("Error: matrix N should be symmetric!");
        }

        // Find the eigenvector corresponding to the maximum positive eigenvalue.
        var eigenvectors = eigen.EigenVectors;
        //Debug.Log("Eigenvectors: " + eigenvectors);

		var eigenvector = eigenvectors.Column(index);
		//Debug.Log("Eigenvector: " + eigenvector);

		// Convert the eigenvector to a quaternion.
		// References:
		// - http://peterdn.com/files/3DMappingProjectReport.pdf
        Rotation = new Quaternion(
            (float)eigenvector[1],
            (float)eigenvector[2],
            (float)eigenvector[3],
            (float)eigenvector[0]
        );
        
        Debug.Log("Rotation: " + Rotation.eulerAngles);
    }

	public override void ComputeTranslation()
	{
		Translation = ControllerCentroid - Rotation * HelmetCentroid;

		Debug.Log("Translation: " + Translation);
	}

	public override void ComputeScale()
	{
		double helmetScale = 0.0f;
		double controllerScale = 0.0f;

        for (int i = 0 ; i < Correlations.Count ; i++)
        {
            Correlation c = Correlations[i];
            
            helmetScale += Math.Sqrt(
				Math.Pow(c.HelmetPosition.x - HelmetCentroid.x, 2) +
				Math.Pow(c.HelmetPosition.y - HelmetCentroid.y, 2) +
				Math.Pow(c.HelmetPosition.z - HelmetCentroid.z, 2)
			);

            controllerScale += Math.Sqrt(
				Math.Pow(c.ControllerPosition.x - ControllerCentroid.x, 2) +
				Math.Pow(c.ControllerPosition.y - ControllerCentroid.y, 2) +
				Math.Pow(c.ControllerPosition.z - ControllerCentroid.z, 2)
			);
        }

		float scale = (float)Math.Sqrt(helmetScale / controllerScale);

		Scale = new Vector3(scale, scale, scale);
		
		Debug.Log("Scale: " + scale);
	}
}