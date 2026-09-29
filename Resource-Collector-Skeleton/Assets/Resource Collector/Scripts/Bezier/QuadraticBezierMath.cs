using UnityEngine;

/*
 * QuadraticBezierMath holds three ways to sample a quadratic Bezier:
 * De Casteljau, Bernstein basis, and power basis. Callers supply the points,
 * so this class does not depend on scene objects. Tangents are derivatives,
 * not unit directions.
 */

public static class QuadraticBezierMath
{
    public static Vector3 SamplePointDeCasteljau(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);

        return Vector3.Lerp(a, b, t);
    }

    public static Vector3 SampleTangentDeCasteljau(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);

        return 2f * (b - a);
    }

    // B(t) = (1-t)^2 P0 + 2(1-t)t P1 + t^2 P2
    public static Vector3 SamplePointBernstein(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float tComplement = 1f - t;
        float w0 = tComplement * tComplement;
        float w1 = 2f * tComplement * t;
        float w2 = t * t;

        return w0 * p0 + w1 * p1 + w2 * p2;
    }

    public static Vector3 SampleTangentBernstein(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float tComplement = 1f - t;
        float w0 = -2f * tComplement;
        float w1 = 2f * (1f - 2f * t);
        float w2 = 2f * t;

        return w0 * p0 + w1 * p1 + w2 * p2;
    }

    // C0 = P0
    // C1 = 2(P1 - P0)
    // C2 = P0 - 2P1 + P2
    //
    // Compute once when the control points change;
    // reuse the coefficients for every t.
    public static void ComputePowerBasisCoefficients(
        Vector3 p0, Vector3 p1, Vector3 p2,
        out Vector3 c0, out Vector3 c1, out Vector3 c2)
    {
        c0 = p0;
        c1 = 2f * (p1 - p0);
        c2 = p0 - 2f * p1 + p2;
    }

    // P(t) = C0 + C1 t + C2 t^2
    public static Vector3 SamplePointPowerBasis(Vector3 c0, Vector3 c1, Vector3 c2, float t)
    {
        return c0 + c1 * t + c2 * t * t;
    }

    // P'(t) = C1 + 2 C2 t
    public static Vector3 SampleTangentPowerBasis(Vector3 c1, Vector3 c2, float t)
    {
        return c1 + 2f * c2 * t;
    }
}
