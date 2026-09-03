using UnityEngine;

public class ParryScript : MonoBehaviour
{
    private PlayerAnimation playerAnimation;
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerAnimation = GetComponent<PlayerAnimation>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TryParry()
    {
        playerAnimation.PlayParryAnimation();
    }
}
