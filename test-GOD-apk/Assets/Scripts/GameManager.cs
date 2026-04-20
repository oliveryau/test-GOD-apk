using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public event Action<int> UnitsMerged;

    [Header("Scene Spawn Points (set these directly in scene)")]
    [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();

    [Header("Unit Prefabs by Tier Index (0 = Tier 1, 1 = Tier 2, ...)")]
    [SerializeField] private List<Units> _unitPrefabsByTier = new List<Units>();
    [SerializeField] private Vector3 _unitFacingEuler = new Vector3(0f, 180f, 0f);

    [Header("Spawn Rules")]
    [SerializeField] private Vector2Int _initialSpawnRange = new Vector2Int(2, 3);
    [SerializeField] private Vector2Int _postMergeSpawnRange = new Vector2Int(2, 3);
    [SerializeField] private float _mergeDropSnapDistance = 0.75f;
    [SerializeField] private float _mergeMoveSpeed = 4f;
    [SerializeField] private float _minMergeMoveDuration = 0.15f;
    [SerializeField] private string _runTriggerName = "run";

    private readonly Dictionary<Transform, Units> _spawnOccupancy = new Dictionary<Transform, Units>();
    private readonly Dictionary<Units, Transform> _dragStartSpawnPoints = new Dictionary<Units, Transform>();
    private bool _isMergeInProgress;
    private bool _isBoardInteractable = true;

    private void Start()
    {
        SpawnRandomTierOneUnits(UnityEngine.Random.Range(_initialSpawnRange.x, _initialSpawnRange.y + 1));
    }

    public void NotifyDragStarted(Units draggedUnit)
    {
        if (!_isBoardInteractable || _isMergeInProgress || draggedUnit == null || draggedUnit.CurrentSpawnPoint == null)
        {
            return;
        }

        _dragStartSpawnPoints[draggedUnit] = draggedUnit.CurrentSpawnPoint;
        _spawnOccupancy.Remove(draggedUnit.CurrentSpawnPoint);
    }

    public void NotifyDragEnded(Units draggedUnit)
    {
        if (!_isBoardInteractable || draggedUnit == null)
        {
            return;
        }

        Units mergeTarget = FindMergeTargetFor(draggedUnit);
        if (mergeTarget != null)
        {
            _dragStartSpawnPoints.TryGetValue(draggedUnit, out Transform mergeStartPoint);
            _dragStartSpawnPoints.Remove(draggedUnit);
            StartCoroutine(PlayMergeAndResolve(draggedUnit, mergeTarget, mergeStartPoint));
            return;
        }

        if (!_dragStartSpawnPoints.TryGetValue(draggedUnit, out Transform originalSpawnPoint) || originalSpawnPoint == null)
        {
            Transform nearestFreePoint = FindNearestFreeSpawnPoint(draggedUnit.transform.position);
            if (nearestFreePoint == null)
            {
                return;
            }

            PlaceUnitOnSpawnPoint(draggedUnit, nearestFreePoint);
            _dragStartSpawnPoints.Remove(draggedUnit);
            return;
        }

        PlaceUnitOnSpawnPoint(draggedUnit, originalSpawnPoint);
        _dragStartSpawnPoints.Remove(draggedUnit);
    }

    private Units FindMergeTargetFor(Units draggedUnit)
    {
        int maxTier = _unitPrefabsByTier.Count;
        if (maxTier == 0 || draggedUnit.Tier >= maxTier)
        {
            return null;
        }

        Transform nearestOccupiedPoint = FindNearestOccupiedSpawnPoint(draggedUnit.transform.position);
        if (nearestOccupiedPoint == null)
        {
            return null;
        }

        if (!_spawnOccupancy.TryGetValue(nearestOccupiedPoint, out Units candidate) || candidate == null)
        {
            return null;
        }

        return candidate.Tier == draggedUnit.Tier ? candidate : null;
    }

    private IEnumerator PlayMergeAndResolve(Units draggedUnit, Units targetUnit, Transform mergeStartPoint)
    {
        if (draggedUnit == null || targetUnit == null)
        {
            yield break;
        }

        _isMergeInProgress = true;
        draggedUnit.SetInteractable(false);
        targetUnit.SetInteractable(false);

        if (mergeStartPoint != null)
        {
            draggedUnit.transform.position = mergeStartPoint.position;
        }

        Vector3 startPosition = draggedUnit.transform.position;
        Vector3 targetPosition = targetUnit.transform.position;
        Vector3 moveDirection = targetPosition - startPosition;
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            draggedUnit.transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        }

        draggedUnit.TriggerRunAnimation(_runTriggerName);

        float elapsed = 0f;
        float travelDistance = Vector3.Distance(startPosition, targetPosition);
        float moveDuration = _mergeMoveSpeed <= 0f ? _minMergeMoveDuration : travelDistance / _mergeMoveSpeed;
        moveDuration = Mathf.Max(_minMergeMoveDuration, moveDuration);

        while (draggedUnit != null && elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            Vector3 expectedPosition = Vector3.Lerp(startPosition, targetPosition, t);
            float distanceFromPath = Vector3.Distance(draggedUnit.transform.position, expectedPosition);

            if (distanceFromPath > _mergeDropSnapDistance)
            {
                // Merge cancelled - unit was dragged away
                _isMergeInProgress = false;
                draggedUnit.SetInteractable(true);
                targetUnit.SetInteractable(true);
                yield break;
            }

            draggedUnit.transform.position = expectedPosition;
            yield return null;
        }

        if (draggedUnit != null)
        {
            draggedUnit.transform.position = targetPosition;
        }

        CompleteMerge(draggedUnit, targetUnit);
        _isMergeInProgress = false;
    }

    private void CompleteMerge(Units draggedUnit, Units targetUnit)
    {
        Vector3 mergePosition = targetUnit != null ? targetUnit.transform.position : Vector3.zero;
        Transform targetSpawnPoint = targetUnit.CurrentSpawnPoint;

        if (targetSpawnPoint != null)
        {
            _spawnOccupancy.Remove(targetSpawnPoint);
        }

        Destroy(draggedUnit.gameObject);
        Destroy(targetUnit.gameObject);

        int maxTier = _unitPrefabsByTier.Count;
        int nextTier = Mathf.Min(targetUnit.Tier + 1, maxTier);
        SpawnUnitOfTier(nextTier, targetSpawnPoint, mergePosition);
        UnitsMerged?.Invoke(nextTier);

        if (CountUnitsOfTier(1) < 2)
        {
            int freeSpawnPoints = 0;
            foreach (Transform point in _spawnPoints)
            {
                if (point != null && !_spawnOccupancy.ContainsKey(point))
                {
                    freeSpawnPoints++;
                }
            }

            // Only spawn if we have free points
            if (freeSpawnPoints > 0)
            {
                int minPostMergeSpawn = Mathf.Max(2, _postMergeSpawnRange.x);
                int maxPostMergeSpawn = Mathf.Max(minPostMergeSpawn, _postMergeSpawnRange.y);
                int extraSpawnCount = Mathf.Min(
                    UnityEngine.Random.Range(minPostMergeSpawn, maxPostMergeSpawn + 1),
                    freeSpawnPoints
                );
                SpawnRandomTierOneUnits(extraSpawnCount);
            }
        }
    }

    private void SpawnRandomTierOneUnits(int count)
    {
        if (_unitPrefabsByTier.Count == 0 || _spawnPoints.Count == 0 || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Transform freePoint = GetRandomFreeSpawnPoint();
            if (freePoint == null)
            {
                return;
            }

            SpawnUnitOfTier(1, freePoint, freePoint.position);
        }
    }

    private Transform GetRandomFreeSpawnPoint()
    {
        List<Transform> freePoints = new List<Transform>();
        foreach (Transform point in _spawnPoints)
        {
            if (point != null && !_spawnOccupancy.ContainsKey(point))
            {
                freePoints.Add(point);
            }
        }

        if (freePoints.Count == 0)
        {
            return null;
        }

        return freePoints[UnityEngine.Random.Range(0, freePoints.Count)];
    }

    private Transform FindNearestFreeSpawnPoint(Vector3 worldPosition)
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (Transform point in _spawnPoints)
        {
            if (point == null || _spawnOccupancy.ContainsKey(point))
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(point.position - worldPosition);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = point;
            }
        }

        return nearest;
    }

    private Transform FindNearestOccupiedSpawnPoint(Vector3 worldPosition)
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;
        float maxDistanceSqr = _mergeDropSnapDistance * _mergeDropSnapDistance;

        foreach (KeyValuePair<Transform, Units> pair in _spawnOccupancy)
        {
            Transform point = pair.Key;
            if (point == null || pair.Value == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(point.position - worldPosition);
            if (distance <= maxDistanceSqr && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = point;
            }
        }

        return nearest;
    }

    private Units SpawnUnitOfTier(int tier, Transform spawnPoint, Vector3 spawnPosition)
    {
        int prefabIndex = tier - 1;
        if (prefabIndex < 0 || prefabIndex >= _unitPrefabsByTier.Count)
        {
            return null;
        }

        Units prefab = _unitPrefabsByTier[prefabIndex];
        if (prefab == null)
        {
            return null;
        }

        Units spawned = Instantiate(prefab, spawnPosition, Quaternion.identity);
        spawned.Setup(this, tier, spawnPoint);
        spawned.transform.rotation = Quaternion.Euler(_unitFacingEuler);

        if (spawnPoint != null)
        {
            _spawnOccupancy[spawnPoint] = spawned;
            spawned.transform.position = spawnPoint.position;
            spawned.transform.rotation = Quaternion.Euler(_unitFacingEuler);
        }

        return spawned;
    }

    private int CountUnitsOfTier(int tier)
    {
        int count = 0;
        foreach (Units unit in _spawnOccupancy.Values)
        {
            if (unit != null && unit.Tier == tier)
            {
                count++;
            }
        }

        return count;
    }

    private void PlaceUnitOnSpawnPoint(Units unit, Transform spawnPoint)
    {
        unit.SetSpawnPoint(spawnPoint);
        unit.SetInteractable(true);
        unit.transform.position = spawnPoint.position;
        unit.transform.rotation = Quaternion.Euler(_unitFacingEuler);
        _spawnOccupancy[spawnPoint] = unit;
    }

    public void SetBoardInteractable(bool canInteract)
    {
        _isBoardInteractable = canInteract;
        foreach (Units unit in _spawnOccupancy.Values)
        {
            if (unit != null)
            {
                unit.SetInteractable(canInteract);
            }
        }
    }
}
