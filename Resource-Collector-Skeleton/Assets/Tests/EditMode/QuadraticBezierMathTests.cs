using NUnit.Framework;
using UnityEngine;

public class QuadraticBezierMathTests
{
    const float Tolerance = 0.0001f;

    static readonly Vector3 P0 = new Vector3(1f, -2f, 3f);
    static readonly Vector3 P1 = new Vector3(3f, 4f, -1f);
    static readonly Vector3 P2 = new Vector3(7f, 1f, 5f);

    [Test]
    public void DeCasteljauQuadratic_SamplesPointFromEquivalentQuadraticFormula()
    {
        AssertVector3(new Vector3(2.125f, 0.4375f, 1.625f),
            QuadraticBezierMath.SamplePointDeCasteljau(P0, P1, P2, 0.25f));
    }

    [Test]
    public void DeCasteljauQuadratic_SamplesTangentFromFinalInterpolationSegment()
    {
        AssertVector3(new Vector3(5f, 7.5f, -3f),
            QuadraticBezierMath.SampleTangentDeCasteljau(P0, P1, P2, 0.25f));
    }

    [Test]
    public void BernsteinQuadratic_SamplesPointFromEquivalentQuadraticFormula()
    {
        AssertVector3(new Vector3(2.125f, 0.4375f, 1.625f),
            QuadraticBezierMath.SamplePointBernstein(P0, P1, P2, 0.25f));
    }

    [Test]
    public void BernsteinQuadratic_SamplesTangentFromDerivativeFormula()
    {
        AssertVector3(new Vector3(5f, 7.5f, -3f),
            QuadraticBezierMath.SampleTangentBernstein(P0, P1, P2, 0.25f));
    }

    [Test]
    public void PowerBasisQuadratic_SamplesPointFromEquivalentQuadraticFormula()
    {
        QuadraticBezierMath.ComputePowerBasisCoefficients(P0, P1, P2,
            out Vector3 c0, out Vector3 c1, out Vector3 c2);

        AssertVector3(new Vector3(2.125f, 0.4375f, 1.625f),
            QuadraticBezierMath.SamplePointPowerBasis(c0, c1, c2, 0.25f));
    }

    [Test]
    public void PowerBasisQuadratic_SamplesTangentFromDerivativeFormula()
    {
        QuadraticBezierMath.ComputePowerBasisCoefficients(P0, P1, P2,
            out Vector3 c0, out Vector3 c1, out Vector3 c2);

        AssertVector3(new Vector3(5f, 7.5f, -3f),
            QuadraticBezierMath.SampleTangentPowerBasis(c1, c2, 0.25f));
    }

    [Test]
    public void PowerBasisQuadratic_PreparedCoefficientsCanBeReusedWhileCurveIsUnchanged()
    {
        QuadraticBezierMath.ComputePowerBasisCoefficients(P0, P1, P2,
            out Vector3 c0, out Vector3 c1, out Vector3 c2);

        AssertVector3(P0, QuadraticBezierMath.SamplePointPowerBasis(c0, c1, c2, 0f));
        AssertVector3(new Vector3(3.5f, 1.75f, 1.5f),
            QuadraticBezierMath.SamplePointPowerBasis(c0, c1, c2, 0.5f));
        AssertVector3(P2, QuadraticBezierMath.SamplePointPowerBasis(c0, c1, c2, 1f));
    }

    static void AssertVector3(Vector3 expected, Vector3 actual)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance));
    }
}
