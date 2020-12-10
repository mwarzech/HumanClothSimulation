using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line
{
    public Vector3 startPos;
    public Vector3 endPos;
}

public class MathHelper
{
    public struct CollisionResult
    {
        public float distance, sqrDistance;
        public float[] parameter;
        public Vector3[] coords;
    };

    private static float GetClampedRoot(float slope, float h0, float h1)
    {
        float r;
        if (h0 < (float)0)
        {
            if (h1 > (float)0)
            {
                r = -h0 / slope;
                if (r > (float)1)
                {
                    r = (float)0.5;
                }
            }
            else
            {
                r = (float)1;
            }
        }
        else
        {
            r = (float)0;
        }
        return r;
    }

    private static void ComputeIntersection(float[] sValue, int[] classify,
            int[] edge, float[][] end, float mF00, float mF10, float mB)
        {
            if (classify[0] < 0)
            {
                edge[0] = 0;
                end[0][0] = (float)0;
                end[0][1] = mF00 / mB;
                if (end[0][1] < (float)0 || end[0][1] > (float)1)
                {
                    end[0][1] = (float)0.5;
                }

                if (classify[1] == 0)
                {
                    edge[1] = 3;
                    end[1][0] = sValue[1];
                    end[1][1] = (float)1;
                }
                else  // classify[1] > 0
                {
                    edge[1] = 1;
                    end[1][0] = (float)1;
                    end[1][1] = mF10 / mB;
                    if (end[1][1] < (float)0 || end[1][1] > (float)1)
                    {
                        end[1][1] = (float)0.5;
                    }
                }
            }
            else if (classify[0] == 0)
            {
                edge[0] = 2;
                end[0][0] = sValue[0];
                end[0][1] = (float)0;

                if (classify[1] < 0)
                {
                    edge[1] = 0;
                    end[1][0] = (float)0;
                    end[1][1] = mF00 / mB;
                    if (end[1][1] < (float)0 || end[1][1] > (float)1)
                    {
                        end[1][1] = (float)0.5;
                    }
                }
                else if (classify[1] == 0)
                {
                    edge[1] = 3;
                    end[1][0] = sValue[1];
                    end[1][1] = (float)1;
                }
                else
                {
                    edge[1] = 1;
                    end[1][0] = (float)1;
                    end[1][1] = mF10 / mB;
                    if (end[1][1] < (float)0 || end[1][1] > (float)1)
                    {
                        end[1][1] = (float)0.5;
                    }
                }
            }
            else  // classify[0] > 0
            {
                edge[0] = 1;
                end[0][0] = (float)1;
                end[0][1] = mF10 / mB;
                if (end[0][1] < (float)0 || end[0][1] > (float)1)
                {
                    end[0][1] = (float)0.5;
                }

                if (classify[1] == 0)
                {
                    edge[1] = 3;
                    end[1][0] = sValue[1];
                    end[1][1] = (float)1;
                }
                else
                {
                    edge[1] = 0;
                    end[1][0] = (float)0;
                    end[1][1] = mF00 / mB;
                    if (end[1][1] < (float)0 || end[1][1] > (float)1)
                    {
                        end[1][1] = (float)0.5;
                    }
                }
            }
        }

    private static void ComputeMinimumParameters(int[] edge, float[][] end,
            float[] parameter, float mG00, float mG01, float mG10, float mG11, float mB, float mC, float mE)
    {
        float delta = end[1][1] - end[0][1];
        float h0 = delta * (-mB * end[0][0] + mC * end[0][1] - mE);
        if (h0 >= (float)0)
        {
            if (edge[0] == 0)
            {
                parameter[0] = (float)0;
                parameter[1] = GetClampedRoot(mC, mG00, mG01);
}
            else if (edge[0] == 1)
            {
                parameter[0] = (float)1;
                parameter[1] = GetClampedRoot(mC, mG10, mG11);
            }
            else
            {
                parameter[0] = end[0][0];
                parameter[1] = end[0][1];
            }
        }
        else
        {
            float h1 = delta * (-mB * end[1][0] + mC * end[1][1] - mE);
            if (h1 <= (float)0)
            {
                if (edge[1] == 0)
                {
                    parameter[0] = (float)0;
                    parameter[1] = GetClampedRoot(mC, mG00, mG01);
                }
                else if (edge[1] == 1)
                {
                    parameter[0] = (float)1;
                    parameter[1] = GetClampedRoot(mC, mG10, mG11);
                }
                else
                {
                    parameter[0] = end[1][0];
                    parameter[1] = end[1][1];
                }
            }
            else  // h0 < 0 and h1 > 0
            {
                float z = Mathf.Min(Mathf.Max(h0 / (h0 - h1), (float)0), (float)1);
                float omz = (float)1 - z;
                parameter[0] = omz* end[0][0] + z* end[1][0];
                parameter[1] = omz* end[0][1] + z* end[1][1];
            }
        }
    }

    public static CollisionResult DistBetweenSegments(Line segment0,
        Line segment1)
    {
        return DistBetweenSegments(segment0.startPos, segment0.endPos, segment1.startPos, segment1.endPos);
    }

    public static CollisionResult DistBetweenSegments(Vector3 P0, Vector3 P1, Vector3 Q0, Vector3 Q1)
    {
        CollisionResult result = new CollisionResult() { parameter = new float[2], coords = new Vector3[2] };
        float mA, mB, mC, mD, mE;
        float mF00, mF10, mF01, mF11;
        float mG00, mG10, mG01, mG11;

        Vector3 P1mP0 = P1 - P0;
        Vector3 Q1mQ0 = Q1 - Q0;
        Vector3 P0mQ0 = P0 - Q0;
        mA = Vector3.Dot(P1mP0, P1mP0);
        mB = Vector3.Dot(P1mP0, Q1mQ0);
        mC = Vector3.Dot(Q1mQ0, Q1mQ0);
        mD = Vector3.Dot(P1mP0, P0mQ0);
        mE = Vector3.Dot(Q1mQ0, P0mQ0);

        mF00 = mD;
        mF10 = mF00 + mA;
        mF01 = mF00 - mB;
        mF11 = mF10 - mB;

        mG00 = -mE;
        mG10 = mG00 - mB;
        mG01 = mG00 + mC;
        mG11 = mG10 + mC;

        if (mA > (float)0 && mC > (float)0)
        {
            float[] sValue = new float[2];
            sValue[0] = GetClampedRoot(mA, mF00, mF10);
            sValue[1] = GetClampedRoot(mA, mF01, mF11);

            int[] classify = new int[2];
            for (int i = 0; i< 2; ++i)
            {
                if (sValue[i] <= (float)0)
                {
                    classify[i] = -1;
                }
                else if (sValue[i] >= (float)1)
                {
                    classify[i] = +1;
                }
                else
                {
                    classify[i] = 0;
                }
            }

            if (classify[0] == -1 && classify[1] == -1)
            {
                // The minimum must occur on s = 0 for 0 <= t <= 1.
                result.parameter[0] = (float)0;
                result.parameter[1] = GetClampedRoot(mC, mG00, mG01);
            }
            else if (classify[0] == +1 && classify[1] == +1)
            {
                // The minimum must occur on s = 1 for 0 <= t <= 1.
                result.parameter[0] = (float)1;
                result.parameter[1] = GetClampedRoot(mC, mG10, mG11);
            }
            else
            {
                int[] edge = new int[2];
                float[][] end = new float[2][];
                end[0] = new float[2];
                end[1] = new float[2];
                ComputeIntersection(sValue, classify, edge, end, mF00, mF10, mB);
                ComputeMinimumParameters(edge, end, result.parameter, mG00, mG01, mG10, mG11, mB, mC, mE);
            }
        }
        else
        {
            if (mA > (float)0)
            {
                result.parameter[0] = GetClampedRoot(mA, mF00, mF10);
                result.parameter[1] = (float)0;
            }
            else if (mC > (float)0)
            {
                result.parameter[0] = (float)0;
                result.parameter[1] = GetClampedRoot(mC, mG00, mG01);
            }
            else
            {
                // P-segment and Q-segment are degenerate.
                result.parameter[0] = (float)0;
                result.parameter[1] = (float)0;
            }
        }

        result.coords[0] = ((float)1 - result.parameter[0]) * P0 + result.parameter[0] * P1;
        result.coords[1] = ((float)1 - result.parameter[1]) * Q0 + result.parameter[1] * Q1;
        Vector3 diff = result.coords[0] - result.coords[1];
        result.sqrDistance = Vector3.Dot(diff, diff);
        result.distance = Mathf.Sqrt(result.sqrDistance);
        return result;
    }

}
