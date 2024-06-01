using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class NeighborSearch
{
    public float radius = 1.0f;

    public Entry[] spacialLookup;
    public int[] startIndices;

    public (int offsetX, int offsetY)[] offset = new (int, int)[] { (0, 0), (1, 0), (0, 1), (1, 1), (-1, 0), (0, -1), (-1, -1), (1, -1), (-1, 1) };

    public NeighborSearch(int amt)
    {
        spacialLookup = new Entry[amt];
        startIndices = new int[amt];
    }
    public void updateSpacialLookup(Vector2[] points, float r)
    {
        radius = r;

        Parallel.For(0, points.Length, i =>
        {
            (int cellX, int cellY) = getCellIndex(points[i]);
            uint cellKey = getKeyFromHash(hashCell(cellX, cellY));
            spacialLookup[i] = new Entry(i, cellKey, new Vector2Int(cellX, cellY));
            startIndices[i] = int.MaxValue;
        });

        Array.Sort(spacialLookup, (a, b) => a.cellKey.CompareTo(b.cellKey));
       // Array.Sort(spacialLookup);

        Parallel.For(0, points.Length, i =>
        {
            uint key = spacialLookup[i].cellKey;
            uint keyPrev = i == 0 ? uint.MaxValue : spacialLookup[i - 1].cellKey;
            if (key != keyPrev)
            {
                startIndices[key] = i;
            }
        });

    }

    /*
    public List<int> getPointsInRadius(Vector2 samplePoint)
    {
        List<int> result = new List<int>();

        (int centerX, int centerY) = getCellIndex(samplePoint);
        float sqrRadius = radius * radius;
 
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
    */

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

}



public class Entry
{
    public int index;
    public uint cellKey;
    public Vector2Int cellIndex;

    public Entry(int i, uint key, Vector2Int cellIndex)
    {
        index = i;
        cellKey = key;
        this.cellIndex = cellIndex;
    }
}
