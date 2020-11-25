using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestLineCollision : MonoBehaviour
{
    public Transform p1,p2,p3,p4;

    public Vector3 normal = new Vector3(0,1,0);
    public float collisionRadius = 0.5f;
    private const float EPSILON = 0.001f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    Vector3 DebugSphereCollision()
    {
        if(Vector3.Dot(p2.position - p4.position, normal.normalized) >= collisionRadius)
        {
            return Vector3.zero;
        }

        Line clothLine = new Line() { startPos = p1.position, endPos = p2.position };
        Line sphereLine = new Line() { startPos = p3.position, endPos = p4.position };

        MathHelper.Result closestLine = MathHelper.DistBetweenSegments(sphereLine, clothLine);

        if (null == closestLine.closest)
        {
            return Vector3.zero;
        }
        Debug.DrawLine(closestLine.closest[1], closestLine.closest[0], Color.white);
        Vector3 diff = closestLine.closest[1] - closestLine.closest[0];
        Vector3 closestPoint = p4.position + diff;

        float dist = diff.magnitude;

        if (dist < collisionRadius + EPSILON)
        {
            Vector3 disp = normal.normalized * (collisionRadius - Vector3.Dot(diff, normal.normalized));
            Vector3 posAfterDisp = closestPoint + disp;
            Vector3 resultDisp = posAfterDisp - p2.position;

            return resultDisp;
        }
        return Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.DrawLine(p1.position, p2.position, Color.green);
        Debug.DrawLine(p3.position, p4.position, Color.cyan);
        Debug.DrawLine(p4.position, p4.position + normal.normalized * collisionRadius, Color.magenta);

        Vector3 disp = DebugSphereCollision();
        Debug.DrawLine(p2.position, p2.position + disp, Color.red);

        /*
    MathHelper.Result shortestLine = MathHelper.DistBetweenSegments(new Line() { startPos = p1.position, endPos = p2.position },
        new Line() { startPos = p3.position, endPos = p4.position });
    if (shortestLine.closest != null)
    {
        Debug.DrawLine(shortestLine.closest[0], shortestLine.closest[1], Color.red);
    }*/
    }
}
