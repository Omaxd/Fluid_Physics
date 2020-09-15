using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SPHManager : MonoBehaviour
{

    public Transform startingPosition;
    public Vector3 GRAVITY = new Vector3(0.0f, -9.81f, 0.0f);
    public float BOUND_DAMPING = -0.5f;
    public float DT = 0.0008f;




    public SpatialHash<SPHParticle> Hash;





    public class SPHParticle
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Forces;

        public float Density;
        public float Pressure;

        public GameObject Go;


        public void Initialize(Vector3 position, GameObject go)
        {
            Position = position;
            Go = go;

            Velocity = Vector3.zero;
            Forces = Vector3.zero;

            Density = 0.0f;
            Pressure = 0.0f;
        }

        public SPHParticle(Vector3 position, GameObject go)
        {
            Position = position;

            Go = go;

            Velocity = Vector3.zero;
            Forces = Vector3.zero;

            Density = 0.0f;
            Pressure = 0.0f;
        }
    }



    [System.Serializable]
    private struct SPHParameters
    {

        public float particleRadius;
        public float smoothingRadius;
        public float smoothingRadiusSq;
        public float restDensity;
        public float gravityMult;
        public float particleMass;
        public float particleViscosity;
        public float particleDrag;

    }



    private struct SPHCollider
    {
        public Vector3 position;
        public Vector3 right;
        public Vector3 up;
        public Vector2 scale;

        public void Initialize(Transform _transform)
        {
            position = _transform.position;
            right = _transform.right;
            up = _transform.up;
            scale = new Vector2(_transform.lossyScale.x, _transform.lossyScale.y);
        }
    }



    // Consts
    private const float GAS_CONST = 2000.0f;



    [SerializeField] private GameObject character0Prefab = null;



    [SerializeField] private SPHParameters parameters;


    [SerializeField] private int amount = 250;
    [SerializeField] private int rowSize = 16;

    // Data
    private List<SPHParticle> particles;
    private GameObject[] gameObjectColliders;



    private void Start()
    {
        Hash = new SpatialHash<SPHParticle>(2);

        InitSPH();
        InitColliders();


    }



    private void Update()
    {
        UpdateNeighbors();
        ComputeDensityPressure();
        ComputeForces();
        Integrate();
        ComputeColliders();

        ApplyPosition();
    }

    private void UpdateNeighbors()
    {
        foreach (SPHParticle p in particles)
        {
            Hash.UpdatePosition(p.Position, p);
        }
    }

    private void InitSPH()
    {
        particles = new SPHParticle[amount].ToList();

        for (int i = 0; i < amount; i++)
        {

            float x = (i % rowSize) + UnityEngine.Random.Range(-0.1f, 0.1f) + startingPosition.position.x;
            float y = (float)((i / rowSize) / rowSize) * 1.1f + startingPosition.position.y;
            float z = ((i / rowSize) % rowSize) + UnityEngine.Random.Range(-0.1f, 0.1f) + startingPosition.position.z;

            GameObject go = Instantiate(character0Prefab);
            go.tag = "SPH";
            go.transform.localScale = Vector3.one * parameters.particleRadius;
            go.transform.position = new Vector3(x, y, z);
            go.name = "Particle" + i.ToString();

            MarchingCubes.FindParticles();

            particles[i] = new SPHParticle(new Vector3(x, y, z), go);

            Hash.Insert(new Vector3(x, y, z), particles[i]);
        }
    }

    private void InitColliders()
    {
        gameObjectColliders = GameObject.FindGameObjectsWithTag("SPHCollider");
    }

    private static bool Intersect(SPHCollider collider, Vector3 position, float radius, out Vector3 penetrationNormal, out Vector3 penetrationPosition, out float penetrationLength)
    {
        Vector3 colliderProjection = collider.position - position;

        penetrationNormal = Vector3.Cross(collider.right, collider.up);
        penetrationLength = Mathf.Abs(Vector3.Dot(colliderProjection, penetrationNormal)) - (radius / 2.0f);
        penetrationPosition = collider.position - colliderProjection;

        return penetrationLength < 0.0f
            && Mathf.Abs(Vector3.Dot(colliderProjection, collider.right)) < collider.scale.x / 2
            && Mathf.Abs(Vector3.Dot(colliderProjection, collider.up)) < collider.scale.y / 2;
    }



    private Vector3 DampVelocity(SPHCollider collider, Vector3 velocity, Vector3 penetrationNormal, float drag)
    {
        Vector3 newVelocity = Vector3.Dot(velocity, penetrationNormal) * penetrationNormal * BOUND_DAMPING
                            + Vector3.Dot(velocity, collider.right) * collider.right * drag
                            + Vector3.Dot(velocity, collider.up) * collider.up * drag;
        newVelocity = Vector3.Dot(newVelocity, Vector3.forward) * Vector3.forward
                    + Vector3.Dot(newVelocity, Vector3.right) * Vector3.right
                    + Vector3.Dot(newVelocity, Vector3.up) * Vector3.up;
        return newVelocity;
    }



    private void ComputeColliders()
    {

        SPHCollider[] colliders = new SPHCollider[gameObjectColliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].Initialize(gameObjectColliders[i].transform);
        }

        for (int i = 0; i < particles.Count; i++)
        {
            for (int j = 0; j < colliders.Length; j++)
            {

                Vector3 penetrationNormal;
                Vector3 penetrationPosition;
                float penetrationLength;
                if (Intersect(colliders[j], particles[i].Position, parameters.particleRadius,
                    out penetrationNormal, out penetrationPosition, out penetrationLength))
                {
                    particles[i].Velocity = DampVelocity(colliders[j], particles[i].Velocity, penetrationNormal, 1.0f - parameters.particleDrag);
                    particles[i].Position = penetrationPosition - penetrationNormal * Mathf.Abs(penetrationLength);
                }
            }
        }
    }



    private void Integrate()
    {
        for (int i = 0; i < particles.Count(); i++)
        {
            particles[i].Velocity += DT * (particles[i].Forces) / particles[i].Density; // Laplace correction
            particles[i].Position += DT * (particles[i].Velocity);
        }
    }



    private void ComputeDensityPressure()
    {
        for (int i = 0; i < particles.Count(); i++)
        {
            particles[i].Density = 0.0f;

            for (int j = 0; j < particles.Count(); j++)
            {


                {
                    Vector3 rij = particles[j].Position - particles[i].Position;
                    float r2 = rij.sqrMagnitude;

                    if (r2 < parameters.smoothingRadius)
                    {
                        particles[i].Density += parameters.particleMass *
                            (315.0f / (64.0f * Mathf.PI * Mathf.Pow(parameters.smoothingRadius, 9.0f))) *
                            Mathf.Pow(parameters.smoothingRadius - r2, 3.0f);
                    }
                }


                //ideal gas law
                particles[i].Pressure = GAS_CONST * (particles[i].Density - parameters.restDensity);
            }
        }
    }


    private void ComputeForces()
    {
        for (int i = 0; i < particles.Count(); i++)
        {
            Vector3 forcePressure = Vector3.zero;
            Vector3 forceViscosity = Vector3.zero;

            // Physics
            for (int j = 0; j < particles.Count(); j++)
            {
                if (i == j) continue;

                Vector3 rij = particles[j].Position - particles[i].Position;
                float r2 = rij.sqrMagnitude;
                float r = Mathf.Sqrt(r2);

                if (r < parameters.smoothingRadius)
                {
                    forcePressure += -rij.normalized *
                        parameters.particleMass *
                        (particles[i].Pressure + particles[j].Pressure) /
                        (2.0f * particles[j].Density) *
                        (-45.0f / (Mathf.PI * Mathf.Pow(parameters.smoothingRadius, 6.0f))) *
                        Mathf.Pow(parameters.smoothingRadius - r, 2.0f);

                    forceViscosity += parameters.particleViscosity *
                        parameters.particleMass *
                        (particles[j].Velocity - particles[i].Velocity) / particles[j].Density *
                        (45.0f / (Mathf.PI * Mathf.Pow(parameters.smoothingRadius, 6.0f))) *
                        (parameters.smoothingRadius - r);
                }



                Vector3 forceGravity = GRAVITY * particles[i].Density * parameters.gravityMult;

                // Apply forces
                particles[i].Forces = forcePressure + forceViscosity + forceGravity;
            }
        }
    }



    //private void ComputeDensityPressure()
    //{
    //    for (int i = 0; i < particles.Count(); i++)
    //    {
    //        particles[i].Density = 0.0f;

    //        foreach (SPHParticle sph in Hash.QueryPosition(particles[i].Position))
    //        {


    //            {
    //                Vector3 rij = sph.Position - particles[i].Position;
    //                float r2 = rij.sqrMagnitude;

    //                if (r2 < parameters.smoothingRadius)
    //                {
    //                    particles[i].Density += parameters.particleMass *
    //                        (315.0f / (64.0f * Mathf.PI * Mathf.Pow(parameters.smoothingRadius, 9.0f))) *
    //                        Mathf.Pow(parameters.smoothingRadius - r2, 3.0f);
    //                }
    //            }


    //            //ideal gas law
    //            particles[i].Pressure = GAS_CONST * (particles[i].Density - parameters.restDensity);
    //        }
    //    }
    //}






    //private void ComputeForces()
    //{
    //    for (int i = 0; i < particles.Count(); i++)
    //    {
    //        Vector3 forcePressure = Vector3.zero;
    //        Vector3 forceViscosity = Vector3.zero;

    //        // Physics
    //        foreach (SPHParticle sph in Hash.QueryPosition(particles[i].Position))

    //        {

    //            Vector3 rij = sph.Position - particles[i].Position;
    //            float r2 = rij.sqrMagnitude;
    //            float r = Mathf.Sqrt(r2);

    //            if (r < parameters.smoothingRadius)
    //            {
    //                forcePressure += -rij.normalized *
    //                    parameters.particleMass *
    //                    (particles[i].Pressure + sph.Pressure) /
    //                    (2.0f * sph.Density) *
    //                    (-45.0f / (Mathf.PI * Mathf.Pow(parameters.smoothingRadius, 6.0f))) *
    //                    Mathf.Pow(parameters.smoothingRadius - r, 2.0f);

    //                forceViscosity += parameters.particleViscosity *
    //                    parameters.particleMass *
    //                    (sph.Velocity - particles[i].Velocity) / sph.Density *
    //                    (45.0f / (Mathf.PI * Mathf.Pow(parameters.smoothingRadius, 6.0f))) *
    //                    (parameters.smoothingRadius - r);
    //            }


    //            Vector3 forceGravity = GRAVITY * particles[i].Density * parameters.gravityMult;

    //            // Apply forces
    //            particles[i].Forces = forcePressure + forceViscosity + forceGravity;
    //        }
    //    }
    //}



    private void ApplyPosition()
    {
        for (int i = 0; i < particles.Count(); i++)
        {
            particles[i].Go.transform.position = particles[i].Position;
        }
    }
}
