using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Check this out we can require components be on a game object!
[RequireComponent(typeof(MeshFilter))]

public class BParticleSimMesh : MonoBehaviour
{
    public struct BSpring
    {
        public float kd;                        // damping coefficient
        public float ks;                        // spring coefficient
        public float restLength;                // rest length of this spring
        public int attachedParticle;            // index of the attached other particle (use me wisely to avoid doubling springs and sprign calculations)
    }

    public struct BContactSpring
    {
        public float kd;                        // damping coefficient
        public float ks;                        // spring coefficient
        public float restLength;                // rest length of this spring (think about this ... may not even be needed o_0
        public Vector3 attachPoint;             // the attached point on the contact surface
    }

    public struct BParticle
    {
        public Vector3 position;                // position information
        public Vector3 velocity;                // velocity information
        public float mass;                      // mass information
        public BContactSpring contactSpring;    // Special spring for contact forces
        public bool attachedToContact;          // is thi sparticle currently attached to a contact (ground plane contact)
        public List<BSpring> attachedSprings;   // all attached springs, as a list in case we want to modify later fast
        public Vector3 currentForces;           // accumulate forces here on each step        
    }

    public struct BPlane
    {
        public Vector3 position;                // plane position
        public Vector3 normal;                  // plane normal
    }

    public float contactSpringKS = 1000.0f;     // contact spring coefficient with default 1000
    public float contactSpringKD = 20.0f;       // contact spring daming coefficient with default 20

    public float defaultSpringKS = 100.0f;      // default spring coefficient with default 100
    public float defaultSpringKD = 1.0f;        // default spring daming coefficient with default 1

    public bool debugRender = false;            // To render or not to render


    /*** 
     * I've given you all of the above to get you started
     * Here you need to publicly provide the:
     * - the ground plane transform (Transform)
     * - handlePlaneCollisions flag (bool)
     * - particle mass (float)
     * - useGravity flag (bool)
     * - gravity value (Vector3)
     * Here you need to privately provide the:
     * - Mesh (Mesh)
     * - array of particles (BParticle[])
     * - the plane (BPlane)
     ***/
    public Transform groundPlaneTransform;
    public bool handlePlaneCollisions = true;
    public float particleMass = 1.0f;
    public bool useGravity = true;
    public Vector3 gravity = new Vector3(0, -9.8f, 0);

    Mesh mesh;
    Vector3[] localVertices;
    Vector3[] initialLocalVertices;
    BParticle[] particles;
    BPlane plane;

    /// <summary>
    /// Init everything
    /// HINT: in particular you should probbaly handle the mesh, init all the particles, and the ground plane
    /// HINT 2: I'd for organization sake put the init particles and plane stuff in respective functions
    /// HINT 3: Note that mesh vertices when accessed from the mesh filter are in local coordinates.
    ///         This script will be on the object with the mesh filter, so you can use the functions
    ///         transform.TransformPoint and transform.InverseTransformPoint accordingly 
    ///         (you need to operate on world coordinates, and render in local)
    /// HINT 4: the idea here is to make a mathematical particle object for each vertex in the mesh, then connect
    ///         each particle to every other particle. Be careful not to double your springs! There is a simple
    ///         inner loop approach you can do such that you attached exactly one spring to each particle pair
    ///         on initialization. Then when updating you need to remember a particular trick about the spring forces
    ///         generated between particles. 
    /// </summary>
    void Start()
    {
        InitMeshAndParticles();
        InitPlane();
    }

    void FixedUpdate()
    {
        if (particles == null || particles.Length == 0) return;
        float dt = Time.fixedDeltaTime;
        ResetParticleForces();
        if (useGravity)
        {
            Vector3 g = gravity;
            for (int i = 0; i < particles.Length; i++)
                particles[i].currentForces += particles[i].mass * g;
        }
        if (handlePlaneCollisions)
        {
            HandlePlaneContactsAndForces();
        }
        ApplyParticleSpringForces();
        IntegrateSymplectic(dt);
        UpdateMeshFromParticles();
    }



    /*** BIG HINT: My solution code has as least the following functions
     * InitParticles()
     * InitPlane()
     * UpdateMesh() (remember the hint above regarding global and local coords)
     * ResetParticleForces()
     * ...
     ***/



    /// <summary>
    /// Draw a frame with some helper debug render code
    /// </summary>
    public void Update()
    {
       
        if (debugRender)
        {
            int particleCount = particles.Length;
            for (int i = 0; i < particleCount; i++)
            {
                Debug.DrawLine(particles[i].position, particles[i].position + particles[i].currentForces, Color.blue);

                int springCount = particles[i].attachedSprings.Count;
                for (int j = 0; j < springCount; j++)
                {
                    Debug.DrawLine(particles[i].position, particles[particles[i].attachedSprings[j].attachedParticle].position, Color.red);
                }
            }
        }
        
    }

    void InitMeshAndParticles()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        mesh = mf.mesh;
        localVertices = mesh.vertices;
        initialLocalVertices = (Vector3[])localVertices.Clone();
        int n = localVertices.Length;
        particles = new BParticle[n];
        for (int i = 0; i < n; i++)
        {
            particles[i] = new BParticle
            {
                position = transform.TransformPoint(localVertices[i]),
                velocity = Vector3.zero,
                mass = Mathf.Max(1e-6f, particleMass),
                attachedSprings = new List<BSpring>(),
                currentForces = Vector3.zero,
                attachedToContact = false
            };
        }
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                float rest = (particles[i].position - particles[j].position).magnitude;
                BSpring s = new BSpring
                {
                    ks = defaultSpringKS,
                    kd = defaultSpringKD,
                    restLength = rest,
                    attachedParticle = j
                };
                particles[i].attachedSprings.Add(s);
            }
        }
    }

    void InitPlane()
    {
        if (groundPlaneTransform == null)
        {
            Debug.LogWarning("Ground plane Transform not assigned; plane collisions disabled.");
            handlePlaneCollisions = false;
            return;
        }
        plane = new BPlane
        {
            position = groundPlaneTransform.position,
            normal = groundPlaneTransform.up.normalized
        };
    }

    void ResetParticleForces()
    {
        for (int i = 0; i < particles.Length; i++)
            particles[i].currentForces = Vector3.zero;
    }

     void HandlePlaneContactsAndForces()
     {
        plane.position = groundPlaneTransform.position;
        plane.normal = groundPlaneTransform.up.normalized;
        for (int i = 0; i < particles.Length; i++)
        {
             ref BParticle p = ref particles[i];
             float signedDist = Vector3.Dot(p.position - plane.position, plane.normal);
             bool penetrating = signedDist < 0.0f;
             if (penetrating && !p.attachedToContact)
             {
                Vector3 attachPoint = p.position - signedDist * plane.normal;
                p.contactSpring = new BContactSpring
                {
                    ks = contactSpringKS,
                    kd = contactSpringKD,
                    restLength = 0.0f,
                    attachPoint = attachPoint
                };
                 p.attachedToContact = true;
             }
             if (!penetrating && p.attachedToContact)
             {
                p.attachedToContact = false;
             }
             if (p.attachedToContact)
             {
                Vector3 xp_minus_xg = p.position - p.contactSpring.attachPoint;
                float depthAlongN = Vector3.Dot(xp_minus_xg, plane.normal);
                Vector3 proj = depthAlongN * plane.normal;
                Vector3 Fc = -p.contactSpring.ks * proj - p.contactSpring.kd * p.velocity;
                p.currentForces += Fc;
             }
        }
     }

     void ApplyParticleSpringForces()
     {
         int n = particles.Length;
         for (int i = 0; i < n; i++)
         {
             var springs = particles[i].attachedSprings;
             for (int sIdx = 0; sIdx < springs.Count; sIdx++)
             {
                BSpring s = springs[sIdx];
                int j = s.attachedParticle;
                Vector3 xi = particles[i].position;
                Vector3 xj = particles[j].position;
                Vector3 d = xi - xj;
                float dist = d.magnitude;
                if (dist < 1e-7f) continue;
                Vector3 nhat = d / dist;
                float stretch = (s.restLength - dist);
                Vector3 Fspring = s.ks * (stretch * nhat);
                Vector3 relVel = particles[i].velocity - particles[j].velocity;
                float vRelAlong = Vector3.Dot(relVel, nhat);
                Vector3 Fdamp = -s.kd * (vRelAlong * nhat);
                Vector3 F = Fspring + Fdamp;
                particles[i].currentForces += F;
                particles[j].currentForces -= F;
             }
         }
     }

     void IntegrateSymplectic(float dt)
     {
        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 a = particles[i].currentForces / particles[i].mass;
            particles[i].velocity += a * dt;
            particles[i].position += particles[i].velocity * dt;
        }
     }

     void UpdateMeshFromParticles()
     {
        int n = particles.Length;
        if (localVertices == null || localVertices.Length != n)
            localVertices = new Vector3[n];
        for (int i = 0; i < n; i++)
            localVertices[i] = transform.InverseTransformPoint(particles[i].position);
        mesh.vertices = localVertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
     }
}
