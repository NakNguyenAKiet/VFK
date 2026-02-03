using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    #region Settings
    [Header("Target")]
    [SerializeField] private Transform target;
    #endregion

    #region Lifecycle
    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;       
    }

    void Update()
    {
        transform.position = target.position;
    }
    #endregion
}
