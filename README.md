## simple ECS-based pathfinding test

<img width="1830" height="985" alt="image" src="https://github.com/user-attachments/assets/53db149c-9e2a-4c1f-b2b9-e8e3a85a11cd" />

Sample project content:
- grid-based pathfinding
- ECS game code, organized into 3 main namespaces: `Server` (simulation and sim data), `Client` (presentation and input reading) and `ServerAndClient` (shared)
- UI Toolkit in-game interface
- Localization package implemented
- use of my `Segements` and `PrefabSystem` packages

- character animation is `Animator`/Mecanim-based. Animation has 3 layers: lower body, upper body and additive (getting hit etc.)

<p float="center">
  <img src="https://github.com/user-attachments/assets/ec3d3a04-fe2f-4f64-a39f-39728d2b569d" width="49%" height="300px">
  <img src="https://github.com/user-attachments/assets/bbad8eb9-acaa-443f-83d8-530962f75ce6" width="49%" height="300px">
</p>

- editor tools to inspect game data

<p float="center">
  <img src="https://github.com/user-attachments/assets/b8b3dfa9-9fb3-48b8-955e-be0f4befce7d" height="300px">
</p>

