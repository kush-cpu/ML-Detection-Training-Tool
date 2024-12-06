using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using Unity.MLAgents.Policies;

public class ObjectDetectionAgent : Agent
{
    [Header("Agent Configuration")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;
    public float rayDistance = 20f;
    public int numRays = 8;
    
    [Header("Sensor Configuration")]
    public bool useRaySensor = true;
    public bool useCameraSensor = true;
    public bool useGridSensor = true;
    public Camera detectionCamera;
    public int cameraWidth = 84;
    public int cameraHeight = 84;
    
    [Header("Detection Settings")]
    public LayerMask detectableLayers;
    public DetectionConfig[] detectionConfigs;
    
    [Header("Curriculum Settings")]
    public int curriculumLevel = 0;
    public float minObjectDistance = 5f;
    public float maxObjectDistance = 20f;
    public int minObjectCount = 3;
    public int maxObjectCount = 15;
    
    [Header("Visualization")]
    public bool showDebugVisuals = true;
    public GameObject detectionMarkerPrefab;
    public LineRenderer rayVisualizer;

    // Internal variables
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private CameraSensorComponent cameraSensor;
    private GridSensorComponent gridSensor;
    private Dictionary<string, GameObject> activeDetections = new Dictionary<string, GameObject>();
    private float episodeTimer = 0f;
    private int detectionsThisEpisode = 0;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    [System.Serializable]
    public class RewardModifiers
    {
        public float distanceMultiplier = 0.1f;
        public float speedBonus = 0.2f;
        public float accuracyBonus = 0.3f;
        public float uniqueDetectionBonus = 0.5f;
        public float falsePositivePenalty = -0.2f;
    }
    public RewardModifiers rewardModifiers;

    void Start()
    {
        InitializeSensors();
        InitializeVisualizers();
    }

    private void InitializeSensors()
    {
        if (useRaySensor)
        {
            rayPerceptionSensor = gameObject.AddComponent<RayPerceptionSensorComponent3D>();
            rayPerceptionSensor.RaysPerDirection = numRays;
            rayPerceptionSensor.MaxRayDegrees = 360f;
            rayPerceptionSensor.SphereCastRadius = 0.5f;
            rayPerceptionSensor.RayLength = rayDistance;
            rayPerceptionSensor.DetectableTags = new List<string>(System.Array.ConvertAll(detectionConfigs, config => config.tag));
        }

        if (useCameraSensor && detectionCamera != null)
        {
            cameraSensor = gameObject.AddComponent<CameraSensorComponent>();
            cameraSensor.Camera = detectionCamera;
            cameraSensor.Width = cameraWidth;
            cameraSensor.Height = cameraHeight;
            cameraSensor.Grayscale = true;
        }

        if (useGridSensor)
        {
            gridSensor = gameObject.AddComponent<GridSensorComponent>();
            gridSensor.GridSize = new Vector3Int(20, 1, 20);
            gridSensor.CellScale = new Vector3(1f, 1f, 1f);
        }
    }

    private void InitializeVisualizers()
    {
        if (showDebugVisuals && rayVisualizer == null)
        {
            rayVisualizer = gameObject.AddComponent<LineRenderer>();
            rayVisualizer.material = new Material(Shader.Find("Sprites/Default"));
            rayVisualizer.startWidth = 0.1f;
            rayVisualizer.endWidth = 0.1f;
            rayVisualizer.positionCount = numRays * 2;
        }
    }

    public override void Initialize()
    {
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;
    }

    private void EnvironmentReset()
    {
        UpdateCurriculumLevel();
        ClearSpawnedObjects();
    }

    public override void OnEpisodeBegin()
    {
        episodeTimer = 0f;
        detectionsThisEpisode = 0;
        ResetScene();
        SpawnObjects();
    }

    private void UpdateCurriculumLevel()
    {
        float progress = Academy.Instance.EnvironmentParameters.GetWithDefault("curriculum_level", 0f);
        curriculumLevel = Mathf.FloorToInt(progress * 5); // 5 levels total
        
        // Adjust difficulty based on curriculum level
        minObjectDistance = Mathf.Lerp(5f, 2f, progress);
        maxObjectDistance = Mathf.Lerp(20f, 30f, progress);
        minObjectCount = Mathf.FloorToInt(Mathf.Lerp(3, 10, progress));
        maxObjectCount = Mathf.FloorToInt(Mathf.Lerp(15, 30, progress));
    }

    private void ResetScene()
    {
        transform.localPosition = new Vector3(Random.Range(-10f, 10f), 0.5f, Random.Range(-10f, 10f));
        transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        ClearDetectionMarkers();
    }

    private void SpawnObjects()
    {
        ClearSpawnedObjects();
        
        int objectCount = Random.Range(minObjectCount, maxObjectCount + 1);
        
        for (int i = 0; i < objectCount; i++)
        {
            foreach (var config in detectionConfigs)
            {
                if (Random.value < config.spawnProbability)
                {
                    SpawnObject(config);
                }
            }
        }
    }

    private void SpawnObject(DetectionConfig config)
    {
        float angle = Random.Range(0f, 360f);
        float distance = Random.Range(minObjectDistance, maxObjectDistance);
        Vector3 position = transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
        
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * config.objectScale;
        obj.tag = config.tag;
        
        Renderer renderer = obj.GetComponent<Renderer>();
        renderer.material.color = config.debugColor;
        
        spawnedObjects.Add(obj);
    }

    private void ClearSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localRotation.eulerAngles.y);
        sensor.AddObservation(episodeTimer);
        sensor.AddObservation(detectionsThisEpisode);
        sensor.AddObservation(curriculumLevel);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveAmount = actions.ContinuousActions[0];
        float rotateAmount = actions.ContinuousActions[1];

        // Movement with momentum and smoothing
        Vector3 targetVelocity = transform.forward * moveAmount * moveSpeed;
        GetComponent<Rigidbody>().velocity = Vector3.Lerp(GetComponent<Rigidbody>().velocity, targetVelocity, Time.deltaTime * 5f);
        transform.Rotate(0f, rotateAmount * rotationSpeed * Time.deltaTime, 0f);

        ProcessDetections();
        UpdateVisualizations();
        
        episodeTimer += Time.deltaTime;
        
        // Episode timeout
        if (episodeTimer >= 30f)
        {
            EndEpisode();
        }
    }

    private void ProcessDetections()
    {
        if (useRaySensor)
        {
            ProcessRayDetections();
        }
        if (useCameraSensor)
        {
            ProcessCameraDetections();
        }
        if (useGridSensor)
        {
            ProcessGridDetections();
        }
    }

    private void ProcessRayDetections()
    {
        RaycastHit[] hits = new RaycastHit[numRays];
        float[] rayAngles = new float[numRays];
        
        for (int i = 0; i < numRays; i++)
        {
            rayAngles[i] = i * (360f / numRays);
            Vector3 rayDirection = Quaternion.Euler(0f, rayAngles[i], 0f) * transform.forward;
            
            if (Physics.Raycast(transform.position, rayDirection, out hits[i], rayDistance, detectableLayers))
            {
                ProcessDetection(hits[i].collider.gameObject, hits[i].point, hits[i].distance);
            }
        }
    }

    private void ProcessCameraDetections()
    {
        if (detectionCamera == null) return;

        RaycastHit hit;
        Ray ray = detectionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        
        if (Physics.Raycast(ray, out hit, rayDistance, detectableLayers))
        {
            ProcessDetection(hit.collider.gameObject, hit.point, hit.distance);
        }
    }

    private void ProcessGridDetections()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, rayDistance, detectableLayers);
        
        foreach (var collider in colliders)
        {
            ProcessDetection(collider.gameObject, collider.transform.position, 
                Vector3.Distance(transform.position, collider.transform.position));
        }
    }

    private void ProcessDetection(GameObject detectedObject, Vector3 detectionPoint, float distance)
    {
        string detectedTag = detectedObject.tag;
        DetectionConfig config = System.Array.Find(detectionConfigs, c => c.tag == detectedTag);
        
        if (config != null)
        {
            // Calculate sophisticated reward
            float reward = config.baseReward;
            
            // Distance bonus
            reward += (1f - distance / rayDistance) * rewardModifiers.distanceMultiplier;
            
            // Speed bonus
            if (GetComponent<Rigidbody>().velocity.magnitude > moveSpeed * 0.8f)
            {
                reward += rewardModifiers.speedBonus;
            }
            
            // Unique detection bonus
            if (!activeDetections.ContainsKey(detectedObject.GetInstanceID().ToString()))
            {
                reward += rewardModifiers.uniqueDetectionBonus;
                detectionsThisEpisode++;
            }
            
            AddReward(reward);
            
            if (showDebugVisuals)
            {
                CreateOrUpdateDetectionMarker(detectedObject.GetInstanceID().ToString(), detectionPoint, config.debugColor);
            }
        }
    }

    private void UpdateVisualizations()
    {
        if (!showDebugVisuals) return;

        // Update ray visualizations
        if (rayVisualizer != null)
        {
            Vector3[] positions = new Vector3[numRays * 2];
            for (int i = 0; i < numRays; i++)
            {
                float angle = i * (360f / numRays);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
                
                positions[i * 2] = transform.position;
                positions[i * 2 + 1] = transform.position + direction * rayDistance;
            }
            rayVisualizer.SetPositions(positions);
        }
    }

    private void CreateOrUpdateDetectionMarker(string id, Vector3 position, Color color)
    {
        if (!activeDetections.ContainsKey(id))
        {
            GameObject marker = Instantiate(detectionMarkerPrefab, position, Quaternion.identity);
            marker.GetComponent<Renderer>().material.color = color;
            activeDetections.Add(id, marker);
        }
        else
        {
            activeDetections[id].transform.position = position;
        }
    }

    private void ClearDetectionMarkers()
    {
        foreach (var marker in activeDetections.Values)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        activeDetections.Clear();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }

    void OnDestroy()
    {
        if (Academy.IsInitialized)
        {
            Academy.Instance.OnEnvironmentReset -= EnvironmentReset;
        }
    }
}

[System.Serializable]
public class DetectionConfig
{
    public string tag;
    public float baseReward;
    public Color debugColor;
    public float objectScale = 1f;
    public float spawnProbability = 0.5f;
}