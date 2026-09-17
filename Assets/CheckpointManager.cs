using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    private Bonfire currentBonfire;

    public Bonfire CurrentBonfire => currentBonfire;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(Bonfire bonfire)
    {
        if (bonfire == null) 
        { Debug.LogWarning("Tried to set a null bonfire as checkpoint."); 
            return; 
        
        }

        currentBonfire = bonfire;

        Debug.Log("Checkpoint set to: " + bonfire.name);    
    }

    public Transform GetRespawnPoint()
    {
        if (currentBonfire == null)
        {
            return null;
        }

        return currentBonfire.GetRespawnPoint();
    }

    public bool HasCheckpoint()
    {
        return currentBonfire != null;
    }
}
