# Spring Mass Simulation

WebGL build: https://accardonull.github.io/SpringMass-main/

A Spring Mass Simulation made in Unity engine with a soft-body physics system consists of three differently parametrized soft cubes and their interactions with a flat ground.

Features include:

- Automatic initialization of the particles. Every vertex in the mesh has an associated particle and in the correct coordinate system. 

- Automatic initialization of the spring configuration. All particles in a mesh are connected to all other particles by a single spring. The rest length is computed from the initial mesh configuration.

- The ground contact penetration penalty springs are initialized when penetration is detected, just once during the duration of the penalty. The attach point for the penalty spring is the nearest point on the plan from the particle at the moment the contact penetration is detection. The spring has the property values and rest length. 

-  The ground contact penetration penalty springs are updated during the penalty and detached when the collision is resolved.

- The vertices of the mesh are updated at the end of each simulation loop.

- The particle-particle spring forces are computed and the reflected force "trick" is used to reduce redundant computations of spring forces between particle pairs.

- The mesh bounds and normals are updated after the mesh is modified.

- The symplectic Euler integration scheme is implemented.

- The simulator loop updates all particle states using the update callback and time. 
