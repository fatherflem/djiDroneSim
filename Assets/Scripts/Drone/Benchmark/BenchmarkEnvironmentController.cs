using System.Collections.Generic;
using DroneSim.Drone.Bootstrap;
using DroneSim.Drone.Camera;
using UnityEngine;

namespace DroneSim.Drone.Benchmark
{
    /// <summary>
    /// Isolates benchmark runs from presentation-only scene objects.
    /// During benchmark playback, colliders/rigidbodies on decorative/operator/controller props are disabled,
    /// and drone spawn can be offset into a clean benchmark area.
    /// </summary>
    public class BenchmarkEnvironmentController : MonoBehaviour
    {
        [Header("Benchmark spawn isolation")]
        [SerializeField] private bool useDedicatedBenchmarkArea = true;
        [Tooltip("World-space offset applied to maneuver initial positions while a benchmark run is active.")]
        [SerializeField] private Vector3 benchmarkSpawnOffset = new Vector3(0f, 0f, 40f);

        [Header("Presentation prop isolation")]
        [Tooltip("Optional explicit roots that should never physically interfere with benchmarks.")]
        [SerializeField] private List<Transform> explicitPresentationRoots = new List<Transform>();
        [SerializeField] private bool autoDiscoverPresentationRoots = true;

        private readonly List<Transform> cachedRoots = new List<Transform>();
        private readonly List<ColliderState> colliderStates = new List<ColliderState>();
        private readonly List<RigidbodyState> rigidbodyStates = new List<RigidbodyState>();
        private bool isIsolationActive;

        private struct ColliderState
        {
            public Collider Collider;
            public bool Enabled;
        }

        private struct RigidbodyState
        {
            public Rigidbody Rigidbody;
            public bool IsKinematic;
            public bool DetectCollisions;
            public RigidbodyConstraints Constraints;
        }

        private void Awake()
        {
            RebuildRootCache();
        }

        public void RebuildRootCache()
        {
            cachedRoots.Clear();

            for (int i = 0; i < explicitPresentationRoots.Count; i++)
            {
                Transform root = explicitPresentationRoots[i];
                if (root != null)
                {
                    cachedRoots.Add(root);
                }
            }

            if (!autoDiscoverPresentationRoots)
            {
                return;
            }

            AddRootIfPresent(FindFirstObjectByType<VRUserPlaceholder>()?.transform);
            AddRootIfPresent(FindFirstObjectByType<DroneControllerPlaceholder>()?.transform);

            DroneFeedDisplaySurface[] displaySurfaces = FindObjectsByType<DroneFeedDisplaySurface>(FindObjectsSortMode.None);
            for (int i = 0; i < displaySurfaces.Length; i++)
            {
                AddRootIfPresent(displaySurfaces[i] != null ? displaySurfaces[i].transform : null);
            }

            GameObject demoDisplays = GameObject.Find("DemoDisplays");
            AddRootIfPresent(demoDisplays != null ? demoDisplays.transform : null);

            AddAllByName("Marker");
            AddAllByName("HoverBoxEdge");
            AddAllByName("VRControllerScreenPlaceholder_Fallback");
        }

        public void SetBenchmarkIsolationActive(bool active)
        {
            if (active == isIsolationActive)
            {
                return;
            }

            if (active)
            {
                EnableIsolation();
            }
            else
            {
                RestoreIsolationState();
            }

            isIsolationActive = active;
        }

        public Vector3 GetBenchmarkSpawnPosition(Vector3 maneuverInitialPosition)
        {
            return useDedicatedBenchmarkArea ? maneuverInitialPosition + benchmarkSpawnOffset : maneuverInitialPosition;
        }

        private void EnableIsolation()
        {
            RebuildRootCache();
            colliderStates.Clear();
            rigidbodyStates.Clear();

            for (int i = 0; i < cachedRoots.Count; i++)
            {
                Transform root = cachedRoots[i];
                if (root == null)
                {
                    continue;
                }

                Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < colliders.Length; c++)
                {
                    Collider collider = colliders[c];
                    colliderStates.Add(new ColliderState { Collider = collider, Enabled = collider.enabled });
                    collider.enabled = false;
                }

                Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
                for (int b = 0; b < bodies.Length; b++)
                {
                    Rigidbody rigidbody = bodies[b];
                    rigidbodyStates.Add(new RigidbodyState
                    {
                        Rigidbody = rigidbody,
                        IsKinematic = rigidbody.isKinematic,
                        DetectCollisions = rigidbody.detectCollisions,
                        Constraints = rigidbody.constraints
                    });

                    rigidbody.isKinematic = true;
                    rigidbody.detectCollisions = false;
                    rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                }
            }
        }

        private void RestoreIsolationState()
        {
            for (int i = 0; i < colliderStates.Count; i++)
            {
                ColliderState state = colliderStates[i];
                if (state.Collider != null)
                {
                    state.Collider.enabled = state.Enabled;
                }
            }

            for (int i = 0; i < rigidbodyStates.Count; i++)
            {
                RigidbodyState state = rigidbodyStates[i];
                if (state.Rigidbody != null)
                {
                    state.Rigidbody.isKinematic = state.IsKinematic;
                    state.Rigidbody.detectCollisions = state.DetectCollisions;
                    state.Rigidbody.constraints = state.Constraints;
                }
            }

            colliderStates.Clear();
            rigidbodyStates.Clear();
        }

        private void AddAllByName(string objectName)
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject current = allObjects[i];
                if (current != null && current.name == objectName)
                {
                    AddRootIfPresent(current.transform);
                }
            }
        }

        private void AddRootIfPresent(Transform root)
        {
            if (root == null)
            {
                return;
            }

            if (!cachedRoots.Contains(root))
            {
                cachedRoots.Add(root);
            }
        }
    }
}
