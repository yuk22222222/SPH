using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundingBoxControl : MonoBehaviour
{
    private LineRenderer lineRenderer;
    // Fixed size of box
    private float width = 60.0f; 
    private float height = 30.0f;
    // Fixed center of box
    private Vector2 center = new Vector2(0f, 0f);

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupRectangle();
        ResetLineWidth(0.3f);
    }

    void SetupRectangle()
    {
        // half width and half height 
        float halfWidth = width / 2;
        float halfHeight = height / 2;

        // find four corners of box
        Vector3[] points = new Vector3[5];
        points[0] = new Vector3(center.x - halfWidth, center.y + halfHeight, 0); 
        points[1] = new Vector3(center.x + halfWidth, center.y + halfHeight, 0); 
        points[2] = new Vector3(center.x + halfWidth, center.y - halfHeight, 0); 
        points[3] = new Vector3(center.x - halfWidth, center.y - halfHeight, 0); 
        points[4] = points[0]; // Close the box by connecting to the start point

        // Set the calculated points to the line renderer
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);

        // Set the width of the line
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    void ResetLineWidth(float newWidth)
    {
        lineRenderer.startWidth = newWidth;
        lineRenderer.endWidth = newWidth;
    }

}
