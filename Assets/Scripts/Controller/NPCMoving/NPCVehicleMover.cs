using SnowPlow.Model.Map;
using System.Collections.Generic;
using UnityEngine;
using SnowPlow.Model.Map.Generator;

namespace SnowPlow.Controller.NPCMovement
{
    // ez a class csak a lathato mozgast kezeli
    // nem valaszt celt, nem keres utat, es nem allitja a modell CurrentPosition erteket
    // azt a sensor fogja csinalni
    public class NPCVehicleMover : MonoBehaviour
    {
        [Header("Map Visual")]
        [SerializeField] private MapVisualizer mapVisualizer;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float reachDistance = 0.05f;

        private readonly List<LanePosition> path = new();
        private int pathIndex;
        private bool isPaused;

        public bool HasPath => path.Count > 0 && pathIndex < path.Count;

        private void Update()
        {
            if (isPaused) return;
            if (!HasPath) return;
            if (mapVisualizer == null) return;

            LanePosition targetPosition = path[pathIndex];

            if (!TryGetWorldPosition(targetPosition, out Vector3 targetWorldPosition))
            {
                ClearPath();
                return;
            }

            MoveTowards(targetWorldPosition);

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

        private void MoveTowards(Vector3 targetWorldPosition)
        {
            Vector3 currentWorldPosition = transform.position;

            Vector3 nextWorldPosition = Vector3.MoveTowards(
                currentWorldPosition,
                targetWorldPosition,
                moveSpeed * Time.deltaTime
            );

            Vector3 movementDirection = nextWorldPosition - currentWorldPosition;

            if (movementDirection.sqrMagnitude > 0.0001f)
            {
                RotateTowards(movementDirection);
            }

            transform.position = nextWorldPosition;
        }

        private void RotateTowards(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                Vector3.forward,
                direction.normalized
            );

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
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
    }
}