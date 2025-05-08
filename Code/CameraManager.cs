using UnityEngine;
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance{get; private set;}
    public Transform diceCameraTarget; // Position/rotation for dice camera
    private Transform _currentTarget; // Current camera target
    private Camera _mainCamera;
    private GameManager _gameManager;
    public float transitionSpeed = 5f; // Speed of camera transition
    
    [Header("Special Tile Cameras")]
    public Transform chanceCameraTarget;
    public Transform communityChestCameraTarget;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Ensure only one instance exists
            return;
        }

        Instance = this;
    }

    void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void LateUpdate()
    {
        if (_currentTarget != null)
        {
            // Smoothly transition position and rotation
            _mainCamera.transform.position = Vector3.Lerp(
                _mainCamera.transform.position, 
                _currentTarget.position, 
                Time.deltaTime * transitionSpeed
            );

            _mainCamera.transform.rotation = Quaternion.Lerp(
                _mainCamera.transform.rotation, 
                _currentTarget.rotation, 
                Time.deltaTime * transitionSpeed
            );
        }
    }

    public void SwitchToTarget(Transform target)
    {
        _currentTarget = target;
    }

    public void SwitchToPlayer(Transform player)
    {
        _currentTarget = player;
    }

    public void SwitchToDiceCamera()
    {
        SwitchToTarget(diceCameraTarget);
    }
    
}