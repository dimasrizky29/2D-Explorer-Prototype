using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    private readonly List<IInteractable> _inRange = new List<IInteractable>();
    private IInteractable _nearest;

    public IInteractable Nearest => _nearest;

    public event Action<IInteractable> OnNearestChanged;

    private void Update()
    {
        RefreshNearest();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            if (!_inRange.Contains(interactable))
            {
                _inRange.Add(interactable);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            _inRange.Remove(interactable);
        }
    }

    private void RefreshNearest()
    {
        IInteractable bestInteractable = null;
        float bestDistanceSq = float.MaxValue;
        Vector3 myPos = transform.position;

        // Remove any destroyed or null interactables
        _inRange.RemoveAll(x => x == null || (x is MonoBehaviour mb && mb == null));

        foreach (var item in _inRange)
        {
            if (!item.CanInteract) continue;

            if (item is MonoBehaviour mb)
            {
                float distSq = (mb.transform.position - myPos).sqrMagnitude;
                if (distSq < bestDistanceSq)
                {
                    bestDistanceSq = distSq;
                    bestInteractable = item;
                }
            }
        }

        if (_nearest != bestInteractable)
        {
            _nearest = bestInteractable;
            OnNearestChanged?.Invoke(_nearest);
        }
    }
}
