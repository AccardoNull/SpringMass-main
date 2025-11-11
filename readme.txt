Implemented features:

-  Correct and automatic initialization of the particles. Every vertex in a the mesh has an associated particle, no duplicates, and in the correct coordinate system. 

- Correct and automatic initialization of the spring configuration. No duplicates, all particles in a mesh are connected to all other particles by a single spring. The rest length is correctly computed from the initial mesh configuration.

- Correct initialization of the ground plane. Note the details of the plan come from the object representing it in the testcase.

- The ground contact penetration penalty springs are correctly and appropriately initialized when penetration is detected, just once during the duration of the penalty. The attach point for the penalty spring is the nearest point on the plan from the particle at the moment the contact penetration is detection. The spring has the correct property values and rest length. All penalty springs have the properties ks = 1000 and kd = 20

-  The ground contact penetration penalty springs are correctly and appropriately updated during the penalty and detached when the collision is resolved.

- The vertices of the mesh are correctly updated, in the correct coordinate system, at the end of each simulation loop.

- The particle-particle spring forces are correctly computed and the reflected force "trick" is used to reduce redundant computations of spring forces between particle pairs.

- The mesh bounds and normals are correctly updated after the mesh is modified.

- The symplectic Euler integration scheme is implemented correctly.

- The simulator loop correctly updates all particle states using the correct update callback and time. 

- The recorded testcase requires that the: blue cube has particle spring properties ks = 200 and kd = 0 the red cube ks = 80 and kd = 0.8 and the green cube ks = 45 and kd = 0.2.