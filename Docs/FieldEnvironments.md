# Field Environments

## Purpose
The Field system lets training scenes swap environment content without code changes. A `FieldLoader` reads a `FieldDefinition` asset and loads either a configured prefab or a runtime placeholder.

## Authoring a new FieldDefinition
1. In the Project window, create an asset via **Create → DroneSim → Field Definition**.
2. Fill out:
   - **fieldName**: Display name for UI/logging.
   - **captureLocation**: School/site/address/coordinates context.
   - **captureDate**: Capture date string.
   - **fieldPrefab**: Prefab containing environment mesh/colliders/lighting.
   - **groundY**: Ground origin Y for vertical placement.
   - **recommendedDrillAltitude**: Target drill altitude above field ground.
   - **operatingAreaSize**: Safe X/Z footprint.
   - **maxAltitude**: Safe height cap above ground.
   - **notes**: Hazards/seasonal caveats.

## Placeholder field
The placeholder exists so drills can run before a real capture is available. It provides:
- 30m x 30m muted-green ground plane with collider.
- Four orange corner posts marking a 12m x 12m operating box.
- A directional sun light only if none exists in-scene.

## Future photogrammetry workflow
1. Fly and capture the real field.
2. Process imagery in RealityCapture, Meshroom, or Polycam.
3. Export the reconstruction as FBX.
4. Import to Unity.
5. Build a prefab with mesh, colliders, and lighting helpers.
6. Create a new `FieldDefinition` referencing that prefab.
7. Assign that `FieldDefinition` to the scene `FieldLoader`.

## Capture guidance
- Fly a grid around **~30–40m altitude**.
- Keep **~70–80% image overlap**.
- Include oblique passes for bleachers, fences, and other vertical features.

## Mesh prep before Unity
- Decimate to under **200k triangles** for runtime performance.
- Bake textures to **2048px or 4096px** maps.
- Create colliders only for surfaces students may contact/land on.
