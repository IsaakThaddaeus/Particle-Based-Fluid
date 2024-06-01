using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NeighborSearch2 : MonoBehaviour
{
    public Vector2[] points = new Vector2[20];
    public float radius = 1.0f;

    private Entry[] spacialLookup;
    private int[] startIndices;
    void Start()
    {

        for (int i = 0; i < points.Length; i++)
        {
            float randomX = UnityEngine.Random.Range(0f, 4f);
            float randomY = UnityEngine.Random.Range(0f, 4f);
            points[i] = new Vector2(randomX, randomY);
        }


        updateSpacialLookup(points, radius);

        for (int i = 0; i < spacialLookup.Length; i++)
        {
            Debug.Log(spacialLookup[i].index + " " + spacialLookup[i].cellKey + " (" + spacialLookup[i].cellIndex.x + " | " + spacialLookup[i].cellIndex.y + ")");
        }

        Debug.Log("---StartInd-------------------------------------------");

        for (int i = 0; i < startIndices.Length; i++)
        {
            Debug.Log(i + " " + startIndices[i]);
        }

    }


    void Update()
    {
        
    }


    public void updateSpacialLookup(Vector2[] points, float radius)
    {
        spacialLookup = new Entry[points.Length];
        startIndices = new int[points.Length];

        for(int i = 0; i < points.Length; i++)
        {
            (int cellX, int cellY) = getCellIndex(points[i]);
            uint cellKey = getKeyFromHash(hashCell(cellX, cellY));
            spacialLookup[i] = new Entry(i, cellKey, new Vector2Int(cellX, cellY));
            startIndices[i] = int.MaxValue;
        }

        Array.Sort(spacialLookup, (a, b) => a.cellKey.CompareTo(b.cellKey));

        for(int i = 0; i < points.Length; i++)
        {
            uint key = spacialLookup[i].cellKey;
            uint keyPrev = i == 0 ? uint.MaxValue : spacialLookup[i - 1].cellKey;
            if(key != keyPrev)
            {
                startIndices[key] = i;
            }
        }

    }
    public List<int> getPointsInRadius(Vector2 samplePoint)
    {
        List<int> result = new List<int>();

        (int centerX, int centerY) = getCellIndex(samplePoint);
        float sqrRadius = radius * radius;

        (int offsetX, int offsetY)[] offset = new (int, int)[] { (0, 0), (1, 0), (0, 1), (1, 1), (-1, 0), (0, -1), (-1, -1), (1, -1), (-1, 1) };
       
        foreach((int offsetX, int offsetY) in offset)
        {
            uint key = getKeyFromHash(hashCell(centerX + offsetX, centerY + offsetY));
            int startIndex = startIndices[key];

            for(int i = startIndex; i < spacialLookup.Length; i++)
            {
                if (spacialLookup[i].cellKey != key) break;
                
                int particleIndex = spacialLookup[i].index;
                float sqrDistance = (points[particleIndex] - samplePoint).sqrMagnitude;

                if(sqrDistance <= sqrRadius)
                {
                    result.Add(particleIndex);
                }
            }
        }

        return result;

    }
    public (int x, int y) getCellIndex(Vector2 point)
    {
        int cellX = (int)(point.x / radius);
        int cellY = (int)(point.y / radius);

        return (cellX, cellY);
    }
    public uint hashCell(int cellX, int cellY)
    {
        uint a = (uint)cellX * 15823;
        uint b = (uint)cellY * 9737333;
        return a + b;
    }
    public uint getKeyFromHash(uint hash)
    {
        return hash % (uint)spacialLookup.Length;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < points.Length; i++)
        {
            Gizmos.DrawSphere(points[i], 0.1f);
        }

        for(int x = 0; x < 10; x++)
        {
            for(int y = 0; y < 10; y++)
            {
                Gizmos.DrawWireCube(new Vector2(x * radius + radius/2, y * radius + radius / 2), new Vector2(radius, radius));
            }
        }

        for(int i = 0; i < points.Length; i++)
        {
            Handles.Label(points[i] + new Vector2(-0.2f, 0), i.ToString());
        }

        Vector3 mousePosition = GetMouseWorldPosition();
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mousePosition, 0.1f);
        Gizmos.DrawWireSphere(mousePosition, radius);

        List<int> pointsInRadius = getPointsInRadius(mousePosition);
        foreach(int i in pointsInRadius)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(points[i], 0.1f);
        }

    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Camera.main.nearClipPlane;
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        mouseWorldPosition.z = 0;

        return mouseWorldPosition;
    }
}


