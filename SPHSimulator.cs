using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SPHSimulator : MonoBehaviour
{
    [Header("Simulation Space")]
    public float sizeX;
    public float sizeY;
    public int amt;
    public float gravity;
    public float elasticity;

    public float smoothingRadius;
    public float particleRadius;
    public float targetDensity;
    public float pressureMultiplier;

    [Header("Mouse Force")]
    Vector2 mousePos;
    float strength = 0.0f;
    public float mouseRadius;
    public float mouseStrength;

    [Header("Particle")]
    public GameObject particlePrefab;


    Vector2[] positions;
    Vector2[] predictedPositions;
    Vector2[] velocities;
    float[] densities;
    Particle[] particleGos;

    NeighborSearch neighborSearch;
    void Start()
    {
        neighborSearch = new NeighborSearch(amt);
        initParticles();
    }

    private void Update()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Camera.main.nearClipPlane;
        mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        

        if (Input.GetMouseButtonDown(0)) strength = mouseStrength;

        else if (Input.GetMouseButtonDown(1)) strength = -mouseStrength;
       
        else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) strength = 0;
      
    }

    private void FixedUpdate()
    {
       integration(Time.fixedDeltaTime);
    }

    void integration(float dt)
    {

        Parallel.For(0, amt, i =>
        {
            Vector2 force = InteractionForce(mousePos, 5, strength, i) + new Vector2(0, -gravity);
            velocities[i] += force * dt;
            predictedPositions[i] = positions[i] + velocities[i] * (1 / 120f);
        });

        neighborSearch.updateSpacialLookup(predictedPositions, smoothingRadius);

        Parallel.For(0, amt, i =>
        {
            densities[i] = calculateDensity(predictedPositions[i]);
        });

        Parallel.For(0, amt, i =>
        {
            Vector2 pressureForce = calculatePressureForce(i);
            Vector2 pressureAcceleration = pressureForce / densities[i];
            velocities[i] += pressureAcceleration * dt;
        });

        Parallel.For(0, amt, i =>
        {
            positions[i] += velocities[i] * dt;
            resolveCollision(i);
            
        });

        for(int i = 0; i < amt; i++)
        {
            particleGos[i].setPosition(positions[i]);
            particleGos[i].setColor(velocities[i]);
        }

    }
    void resolveCollision(int index)
    {
        if (positions[index].x - particleRadius < -sizeX / 2)
        {
            positions[index].x = (-sizeX / 2) + particleRadius;
            velocities[index].x *= -elasticity;
        }

        if (positions[index].x + particleRadius > sizeX / 2)
        {
            positions[index].x = (sizeX / 2) - particleRadius;
            velocities[index].x *= -elasticity;
        }

        if (positions[index].y - particleRadius < -sizeY / 2)
        {
            positions[index].y = (-sizeY / 2) + particleRadius;
            velocities[index].y *= -elasticity;
        }

        if (positions[index].y + particleRadius > sizeY / 2)
        {
            positions[index].y = (sizeY / 2) - particleRadius;
            velocities[index].y *= -elasticity;
        }
    }
    void initParticles()
    {
        positions = new Vector2[amt];
        predictedPositions = new Vector2[amt];
        velocities = new Vector2[amt];
        densities = new float[amt];
        particleGos = new Particle[amt];
        

        for(int i = 0; i < amt; i++)
        {
            Vector2 position = new Vector2(Random.Range(-sizeX / 2, sizeX / 2), Random.Range(-sizeY / 2, sizeY / 2));

            positions[i] = position;
            predictedPositions[i] = position;
            velocities[i] = Vector2.zero;
            densities[i] = 0;
            
            GameObject particleGo = Instantiate(particlePrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            particleGos[i] = particleGo.GetComponent<Particle>();
        }
    }


    float smoothingKernelPoly(float dst, float radius)
    {
        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4;
        float value = Mathf.Max(0, radius * radius - dst * dst);
        return value * value * value / volume;
    }
    float smoothingKernelDerivativePoly(float dst, float radius)
    {
        if(dst >= radius) return 0;
        float f = radius * radius - dst * dst;
        float scale = -24/(Mathf.PI * Mathf.Pow(radius, 8));
        return scale * dst * f * f;
    }

    float smoothingKernel(float dst, float radius)
    {
       if(dst >= radius) return 0;

       float volume = (Mathf.PI * Mathf.Pow(radius, 4)) / 6;
       return (radius - dst) * (radius - dst) / volume;
    }
    float smoothingKernelDerivative(float dst, float radius)
    {
        if(dst >= radius) return 0;

        float scale = 12 / (Mathf.Pow(radius, 4) * Mathf.PI);
        return (dst - radius) * scale;
    }

    float calculateDensity(Vector2 samplePoint)
    {
        float density = 0;
        float mass = 1;


        (int centerX, int centerY) = neighborSearch.getCellIndex(samplePoint);
        float sqrRadius = smoothingRadius * smoothingRadius;

        foreach ((int offsetX, int offsetY) in neighborSearch.offset)
        {
            uint key = neighborSearch.getKeyFromHash(neighborSearch.hashCell(centerX + offsetX, centerY + offsetY));
            int startIndex = neighborSearch.startIndices[key];

            for (int i = startIndex; i < neighborSearch.spacialLookup.Length; i++)
            {
                if (neighborSearch.spacialLookup[i].cellKey != key) break;

                int particleIndex = neighborSearch.spacialLookup[i].index;
                float sqrDistance = (predictedPositions[particleIndex] - samplePoint).sqrMagnitude;

                if (sqrDistance <= sqrRadius)
                {
                    float dst = (predictedPositions[particleIndex] - samplePoint).magnitude;
                    //float influence = smoothingKernel(dst, smoothingRadius);
                    float influence = smoothingKernelPoly(dst, smoothingRadius);
                    density += mass * influence;
                }
            }
        }

        return density;
    }
    Vector2 calculatePressureForce(int particleIndex)
    {
        Vector2 pressureForce = Vector2.zero;

        (int centerX, int centerY) = neighborSearch.getCellIndex(predictedPositions[particleIndex]);
        float sqrRadius = smoothingRadius * smoothingRadius;

        foreach ((int offsetX, int offsetY) in neighborSearch.offset)
        {
            uint key = neighborSearch.getKeyFromHash(neighborSearch.hashCell(centerX + offsetX, centerY + offsetY));
            int startIndex = neighborSearch.startIndices[key];

            for (int i = startIndex; i < neighborSearch.spacialLookup.Length; i++)
            {
                if (neighborSearch.spacialLookup[i].cellKey != key) break;

                int index = neighborSearch.spacialLookup[i].index;
                float sqrDistance = (predictedPositions[particleIndex] - predictedPositions[particleIndex]).sqrMagnitude;

                if (sqrDistance <= sqrRadius)
                {

                    if (particleIndex == index) continue;

                    Vector2 offset = predictedPositions[index] - predictedPositions[particleIndex];
                    float dst = offset.magnitude;
                    Vector2 dir = offset / dst;

                    float slope = smoothingKernelDerivative(dst, smoothingRadius);
                    float density = densities[index];
                    float sharedPressure = calculateSharedPressure(density, densities[particleIndex]);
                    pressureForce += sharedPressure * dir * slope * 1 / density;
                
                }
            }
        }

        return pressureForce;
    }
  
    float convertDensityToPressure(float density)
    {
        return pressureMultiplier * (density - targetDensity);
    }
    float calculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = convertDensityToPressure(densityA);
        float pressureB = convertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    Vector2 InteractionForce(Vector2 inputPos, float radius, float strength, int particleIndex)
    {
        Vector2 interactionForce = Vector2.zero;
        Vector2 offset = inputPos - positions[particleIndex];
        float sqrDst = Vector2.Dot(offset, offset);

        if (sqrDst < radius * radius)
        {
            float dst = Mathf.Sqrt(sqrDst);
            Vector2 dirToInputPoint = dst <= float.Epsilon ? Vector2.zero : offset / dst;
            float centreT = 1 - dst / radius;
            interactionForce += (dirToInputPoint * strength - velocities[particleIndex]) * centreT;
        }

        return interactionForce;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(sizeX, sizeY, 0));

        Gizmos.DrawWireSphere(mousePos, mouseRadius);
    }
}


