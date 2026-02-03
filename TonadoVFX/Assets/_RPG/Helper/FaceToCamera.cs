using UnityEngine;

public class FaceToCamera : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    // Update is called once per frame
    void Update()
    {
        transform.LookAt(_camera.transform);
    }
}
