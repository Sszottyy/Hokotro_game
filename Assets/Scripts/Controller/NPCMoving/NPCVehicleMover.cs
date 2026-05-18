using SnowPlow.Model.Map;
using SnowPlow.Model.Map.Generator;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SnowPlow.Controller.NPCMovement
{
    // ez a class csak a lathato mozgast kezeli
    // nem valaszt celt, nem keres utat, es nem allitja a modell CurrentPosition erteket
    // azt a sensor fogja csinalni
    public class NPCVehicleMover : NetworkBehaviour
    {
        [Header("Map Visual")]
        [SerializeField] private MapVisualizer mapVisualizer;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float reachDistance = 0.05f;

        [Header("Ice slide visual")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float iceMoveSpeedMultiplier = 0.9f;
        [SerializeField] private float iceRotationSpeedMultiplier = 0.35f;
        [SerializeField] private float iceSlideDuration = 0.45f;
        [SerializeField] private float iceSlideOffset = 0.25f;
        [SerializeField] private float iceSlideAngle = 15f;

        private Vector3 visualRootStartLocalPosition;
        private Quaternion visualRootStartLocalRotation;

        private float iceSlideTimer;
        private float iceSlideDirection = 1f;
        private int lastIceSlidePathIndex = -1;

        [Header("Stun")]
        [SerializeField] private float stunDuration = 5f;

        private float stunTimer;
        public bool IsStunned => stunTimer > 0f;

        private readonly List<LanePosition> path = new();
        private int pathIndex;
        private bool isPaused;

        public bool HasPath => path.Count > 0 && pathIndex < path.Count;

        private void Update()
        {
            if (!IsServer)
                return;
            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                return;
            }

            if (isPaused) return;
            if (!HasPath) return;
            if (mapVisualizer == null) return;

            LanePosition targetPosition = path[pathIndex];

            if (!TryGetWorldPosition(targetPosition, out Vector3 targetWorldPosition))
            {
                ClearPath();
                return;
            }

            bool affectedByIce = IsAffectedByIce(targetPosition);

            if (affectedByIce && lastIceSlidePathIndex != pathIndex)
            {
                StartIceSlide();
                lastIceSlidePathIndex = pathIndex;
            }

            MoveTowards(targetWorldPosition, affectedByIce);
            UpdateIceSlideVisual();

            if (Vector3.Distance(transform.position, targetWorldPosition) <= reachDistance)
            {
                pathIndex++;
            }
        }

        public void SetMapVisualizer(MapVisualizer newMapVisualizer)
        {
            mapVisualizer = newMapVisualizer;
        }

        public void SetPath(IReadOnlyList<LanePosition> newPath)
        {
            path.Clear();
            pathIndex = 0;
            isPaused = false;
            lastIceSlidePathIndex = -1;

            if (newPath == null || newPath.Count == 0) return;

            for (int i = 0; i < newPath.Count; i++)
            {
                if (newPath[i] != null)
                {
                    path.Add(newPath[i]);
                }
            }

            // az elso elem altalaban az aktualis pozicio,
            // ezert ha van kovetkezo elem, rogton oda indulunk
            if (path.Count > 1)
            {
                pathIndex = 1;
            }
        }

        public void ClearPath()
        {
            path.Clear();
            pathIndex = 0;
            isPaused = false;
            lastIceSlidePathIndex = -1;
        }

        public void PauseMovement()
        {
            isPaused = true;
        }

        public void ResumeMovement()
        {
            isPaused = false;
        }

        public LanePosition GetCurrentTargetPosition()
        {
            if (!HasPath) return null;

            return path[pathIndex];
        }

        public void SyncWithCurrentPosition(LanePosition currentPosition)
        {
            if (currentPosition == null) return;

            // ha a sensor szerint mar azon a pozicion vagyunk,
            // ami fele a mover menne, akkor tovabblephetunk
            while (HasPath && path[pathIndex].Equals(currentPosition))
            {
                pathIndex++;
            }
        }

        private void MoveTowards(Vector3 targetWorldPosition, bool affectedByIce)
        {
            Vector3 currentWorldPosition = transform.position;

            float currentMoveSpeed = affectedByIce
                ? moveSpeed * iceMoveSpeedMultiplier
                : moveSpeed;

            Vector3 nextWorldPosition = Vector3.MoveTowards(
                currentWorldPosition,
                targetWorldPosition,
                currentMoveSpeed * Time.deltaTime
            );

            Vector3 movementDirection = nextWorldPosition - currentWorldPosition;

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                RotateTowards(movementDirection, affectedByIce);
            }

            transform.position = nextWorldPosition;
        }

        private void RotateTowards(Vector3 direction, bool affectedByIce)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                Vector3.forward,
                direction.normalized
            );

            float currentRotationSpeed = affectedByIce
                ? rotationSpeed * iceRotationSpeedMultiplier
                : rotationSpeed;

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                currentRotationSpeed * Time.deltaTime
            );
        }

        private bool TryGetWorldPosition(LanePosition position, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (position == null) return false;
            if (position.Lane == null) return false;
            if (position.SegmentIndex < 0) return false;
            if (position.SegmentIndex >= position.Lane.Segments.Count) return false;
            if (mapVisualizer == null) return false;

            LaneSegment segment = position.Lane[position.SegmentIndex];

            if (!mapVisualizer.SegmentDirectory.TryGetValue(segment, out VisualSegment visualSegment))
            {
                return false;
            }

            if (visualSegment == null) return false;

            worldPosition = visualSegment.transform.position;
            worldPosition.z = transform.position.z;

            return true;
        }

        public void Stun()
        {
            Stun(stunDuration);
        }

        public void Stun(float duration)
        {
            if (duration <= 0f) return;

            stunTimer = Mathf.Max(stunTimer, duration);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Vehicle")) return;

            NPCVehicleMover otherNpcMover = other.GetComponentInParent<NPCVehicleMover>();
            if (otherNpcMover != null && otherNpcMover != this)
            {
                Stun();
                otherNpcMover.Stun();
                return;
            }

            global::BusMovement otherBusMovement = other.GetComponentInParent<global::BusMovement>();
            if (otherBusMovement != null)
            {
                Stun();
                otherBusMovement.Stun();
                return;
            }
        }

        private void Awake()
        {
            if (visualRoot != null)
            {
                visualRootStartLocalPosition = visualRoot.localPosition;
                visualRootStartLocalRotation = visualRoot.localRotation;
            }
        }

        private bool IsAffectedByIce(LanePosition targetPosition)
        {
            if (IsIcy(targetPosition))
            {
                return true;
            }

            if (pathIndex > 0 && IsIcy(path[pathIndex - 1]))
            {
                return true;
            }

            return false;
        }

        private bool IsIcy(LanePosition position)
        {
            if (position == null) return false;
            if (position.Lane == null) return false;
            if (position.SegmentIndex < 0) return false;
            if (position.SegmentIndex >= position.Lane.Segments.Count) return false;

            return position.Lane[position.SegmentIndex].HasIce;
        }

        private void StartIceSlide()
        {
            if (visualRoot == null) return;

            iceSlideTimer = iceSlideDuration;
            iceSlideDirection = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        }

        private void UpdateIceSlideVisual()
        {
            if (visualRoot == null) return;

            if (iceSlideTimer > 0f)
            {
                iceSlideTimer -= Time.deltaTime;

                float progress = 1f - Mathf.Clamp01(iceSlideTimer / iceSlideDuration);
                float intensity = Mathf.Sin(progress * Mathf.PI);

                visualRoot.localPosition =
                    visualRootStartLocalPosition +
                    Vector3.right * iceSlideDirection * iceSlideOffset * intensity;

                visualRoot.localRotation =
                    visualRootStartLocalRotation *
                    Quaternion.Euler(0f, 0f, -iceSlideDirection * iceSlideAngle * intensity);

                return;
            }

            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                visualRootStartLocalPosition,
                12f * Time.deltaTime
            );

            visualRoot.localRotation = Quaternion.Lerp(
                visualRoot.localRotation,
                visualRootStartLocalRotation,
                12f * Time.deltaTime
            );
        }
    }
}