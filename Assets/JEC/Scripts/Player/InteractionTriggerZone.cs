using System.Collections.Generic;
using UnityEngine;

public class InteractionTriggerZone : MonoBehaviour
{
    [SerializeField] private Interactable target;

    private readonly Dictionary<Player_Interact, int> overlapCounts = new();

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponentInParent<Interactable>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Player_Interact player = other.GetComponentInParent<Player_Interact>();

        if (player == null || target == null)
        {
            return;
        }

        overlapCounts.TryGetValue(player, out int count);
        overlapCounts[player] = count + 1;

        if (count == 0)
        {
            player.RegisterTriggerInteractable(target);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Player_Interact player = other.GetComponentInParent<Player_Interact>();

        if (player == null || !overlapCounts.TryGetValue(player, out int count))
        {
            return;
        }

        count--;

        if (count <= 0)
        {
            overlapCounts.Remove(player);
            player.UnregisterTriggerInteractable(target);
        }
        else
        {
            overlapCounts[player] = count;
        }
    }

    private void OnDisable()
    {
        foreach (Player_Interact player in overlapCounts.Keys)
        {
            if (player != null)
            {
                player.UnregisterTriggerInteractable(target);
            }
        }

        overlapCounts.Clear();
    }
}