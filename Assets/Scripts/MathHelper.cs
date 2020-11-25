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
    public static Vector3 ClampPointToLine(Vector3 pointToClamp, Line lineToClampTo)
    {
        Vector3 clampedPoint = new Vector3();
        float minX, minY, minZ, maxX, maxY, maxZ;
        if (lineToClampTo.startPos.x <= lineToClampTo.endPos.x)
        {
            minX = lineToClampTo.startPos.x;
            maxX = lineToClampTo.endPos.x;
        }
        else
        {
            minX = lineToClampTo.endPos.x;
            maxX = lineToClampTo.startPos.x;
        }
        if (lineToClampTo.startPos.y <= lineToClampTo.endPos.y)
        {
            minY = lineToClampTo.startPos.y;
            maxY = lineToClampTo.endPos.y;
        }
        else
        {
            minY = lineToClampTo.endPos.y;
            maxY = lineToClampTo.startPos.y;
        }
        if (lineToClampTo.startPos.z <= lineToClampTo.endPos.z)
        {
            minZ = lineToClampTo.startPos.z;
            maxZ = lineToClampTo.endPos.z;
        }
        else
        {
            minZ = lineToClampTo.endPos.z;
            maxZ = lineToClampTo.startPos.z;
        }
        clampedPoint.x = (pointToClamp.x < minX) ? minX : (pointToClamp.x > maxX) ? maxX : pointToClamp.x;
        clampedPoint.y = (pointToClamp.y < minY) ? minY : (pointToClamp.y > maxY) ? maxY : pointToClamp.y;
        clampedPoint.z = (pointToClamp.z < minZ) ? minZ : (pointToClamp.z > maxZ) ? maxZ : pointToClamp.z;
        return clampedPoint;
    }

    public static Line distBetweenLines(Line l1, Line l2)
    {
        Vector3 p1, p2, p3, p4, d1, d2;
        p1 = l1.startPos;
        p2 = l1.endPos;
        p3 = l2.startPos;
        p4 = l2.endPos;
        d1 = p2 - p1;
        d2 = p4 - p3;
        float eq1nCoeff = (d1.x * d2.x) + (d1.y * d2.y) + (d1.z * d2.z);
        float eq1mCoeff = (-(Mathf.Pow(d1.x, 2)) - (Mathf.Pow(d1.y, 2)) - (Mathf.Pow(d1.z, 2)));
        float eq1Const = ((d1.x * p3.x) - (d1.x * p1.x) + (d1.y * p3.y) - (d1.y * p1.y) + (d1.z * p3.z) - (d1.z * p1.z));
        float eq2nCoeff = ((Mathf.Pow(d2.x, 2)) + (Mathf.Pow(d2.y, 2)) + (Mathf.Pow(d2.z, 2)));
        float eq2mCoeff = -(d1.x * d2.x) - (d1.y * d2.y) - (d1.z * d2.z);
        float eq2Const = ((d2.x * p3.x) - (d2.x * p1.x) + (d2.y * p3.y) - (d2.y * p2.y) + (d2.z * p3.z) - (d2.z * p1.z));
        float[,] M = new float[,] { { eq1nCoeff, eq1mCoeff, -eq1Const }, { eq2nCoeff, eq2mCoeff, -eq2Const } };
        int rowCount = M.GetUpperBound(0) + 1;
        // pivoting
        for (int col = 0; col + 1 < rowCount; col++) if (M[col, col] == 0)
            // check for zero coefficients
            {
                // find non-zero coefficient
                int swapRow = col + 1;
                for (; swapRow < rowCount; swapRow++) if (M[swapRow, col] != 0) break;

                if (M[swapRow, col] != 0) // found a non-zero coefficient?
                {
                    // yes, then swap it with the above
                    float[] tmp = new float[rowCount + 1];
                    for (int i = 0; i < rowCount + 1; i++)
                    { tmp[i] = M[swapRow, i]; M[swapRow, i] = M[col, i]; M[col, i] = tmp[i]; }
                }
                else return null; // no, then the matrix has no unique solution
            }

        // elimination
        for (int sourceRow = 0; sourceRow + 1 < rowCount; sourceRow++)
        {
            for (int destRow = sourceRow + 1; destRow < rowCount; destRow++)
            {
                float df = M[sourceRow, sourceRow];
                float sf = M[destRow, sourceRow];
                for (int i = 0; i < rowCount + 1; i++)
                    M[destRow, i] = M[destRow, i] * df - M[sourceRow, i] * sf;
            }
        }

        // back-insertion
        for (int row = rowCount - 1; row >= 0; row--)
        {
            float f = M[row, row];
            if (f == 0) return null;

            for (int i = 0; i < rowCount + 1; i++) M[row, i] /= f;
            for (int destRow = 0; destRow < row; destRow++)
            { M[destRow, rowCount] -= M[destRow, row] * M[row, rowCount]; M[destRow, row] = 0; }
        }
        float n = M[0, 2];
        float m = M[1, 2];
        Vector3 i1 = new Vector3(p1.x + (m * d1.x), p1.y + (m * d1.y), p1.z + (m * d1.z));
        Vector3 i2 = new Vector3(p3.x + (n * d2.x), p3.y + (n * d2.y), p3.z + (n * d2.z));
        Vector3 i1Clamped = ClampPointToLine(i1, l1);
        Vector3 i2Clamped = ClampPointToLine(i2, l2);
        return new Line { startPos = i1Clamped, endPos = i2Clamped };
    }

    public struct Result
    {
        public float distance, sqrDistance;
        public float[] parameter;
        public Vector3[] closest;
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

    public static Result DistBetweenSegments(Line segment0,
        Line segment1)
    {
        return DistBetweenSegments(segment0.startPos, segment0.endPos, segment1.startPos, segment1.endPos);
    }

    public static Result DistBetweenSegments(Vector3 P0, Vector3 P1, Vector3 Q0, Vector3 Q1)
    {
        Result result = new Result() { parameter = new float[2], closest = new Vector3[2] };
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

        result.closest[0] = ((float)1 - result.parameter[0]) * P0 + result.parameter[0] * P1;
        result.closest[1] = ((float)1 - result.parameter[1]) * Q0 + result.parameter[1] * Q1;
        Vector3 diff = result.closest[0] - result.closest[1];
        result.sqrDistance = Vector3.Dot(diff, diff);
        result.distance = Mathf.Sqrt(result.sqrDistance);
        return result;
    }

}
