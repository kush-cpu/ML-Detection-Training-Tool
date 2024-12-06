using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;

public class TrainingEnvironmentManager : MonoBehaviour
{
    [Header("Environment Settings")]
    public Vector3 environmentSize = new Vector3(40f, 0f, 40f);
    public GameObject environmentPrefab;
    public GameObject[] obstaclePrefabs;
    public Material[] environmentMaterials;
    
    [Header("Training Settings")]
    public int numberOfAgents = 10;
    public GameObject agentPrefab;
    public float minAgentSpacing = 5f;
    public bool randomizeEnvironmentOnReset = true;
    
    [Header("Performance Monitoring")]
    public bool enablePerformanceTracking = true;
    public float performanceUpdateInterval = 1f;
    
    private List<ObjectDetectionAgent> agents = new List<ObjectDetectionAgent>();
    private List<GameObject> spawnedEnvironments = new List<GameObject>();
    private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
    
    void Start()
    {
        InitializeEnvironments();
        SpawnAgents();
        
        if (enablePerformanceTracking)
        {
            InvokeRepeating("UpdatePerformanceMetrics", 0f, performanceUpdateInterval);
        }
    }
    
    private void InitializeEnvironments()
    {
        int environmentsNeeded = Mathf.CeilToInt(numberOfAgents / 4f); // 4 agents per environment
        
        for (int i = 0; i < environmentsNeeded; i++)
        {
            Vector3 position = new Vector3(
                i * environmentSize.x,
                0f,
                0f
            );
            
            GameObject environment = Instantiate(environmentPrefab, position, Quaternion.identity);
            environment.transform.parent = transform;
            spawnedEnvironments.Add(environment);
            
            if (randomizeEnvironmentOnReset)
            {
                RandomizeEnvironment(environment);
            }
        }
    }
    
    private void RandomizeEnvironment(GameObject environment)
    {
        // Randomize lighting
        Light[] lights = environment.GetComponentsInChildren<Light>();
        foreach (Light light in lights)
        {
            light.intensity = Random.Range(0.5f, 1.5f);
            light.color = Color.Lerp(Color.white, Color.yellow, Random.value);
        }
        
        // Randomize materials
        Renderer[] renderers = environment.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.material = environmentMaterials[Random.Range(0, environmentMaterials.Length)];
        }
        
        // Spawn random obstacles
        SpawnObstacles(environment);
    }
    
    private void SpawnObstacles(GameObject environment)
    {
        int obstacleCount = Random.Range(5, 15);
        Bounds environmentBounds = environment.GetComponent<Collider>().bounds;
        
        for (int i = 0; i < obstacleCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(environmentBounds.min.x, environmentBounds.max.x),
                0f,
                Random.Range(environmentBounds.min.z, environmentBounds.max.z)
            );
            
            GameObject obstacle = Instantiate(
                obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)],
                position,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                environment.transform
            );
            
            // Random scale variation
            float scale = Random.Range(0.8f, 1.2f);
            obstacle.transform.localScale *= scale;
        }
    }
    
    private void SpawnAgents()
    {
        for (int i = 0; i < numberOfAgents; i++)
        {
            GameObject environmentObject = spawnedEnvironments[i / 4];
            Bounds environmentBounds = environmentObject.GetComponent<Collider>().bounds;
            
            Vector3 position = GetValidAgentPosition(environmentBounds);
            GameObject agentObject = Instantiate(agentPrefab, position, Quaternion.identity);
            agentObject.transform.parent = environmentObject.transform;
            
            ObjectDetectionAgent agent = agentObject.GetComponent<ObjectDetectionAgent>();
            agents.Add(agent);
        }
    }
    
    private Vector3 GetValidAgentPosition(Bounds environmentBounds)
    {
        Vector3 position;
        bool validPosition = false;
        int attempts = 0;
        
        do
        {
            position = new Vector3(
                Random.Range(environmentBounds.min.x, environmentBounds.max.x),
                0f,
                Random.Range(environmentBounds.min.z, environmentBounds.max.z)
            );
            
            validPosition = IsValidAgentPosition(position);
            attempts++;
        } while (!validPosition && attempts < 100);
        
        return position;
    }
    
    private bool IsValidAgentPosition(Vector3 position)
    {
        // Check distance from other agents
        foreach (var agent in agents)
        {
            if (Vector3.Distance(position, agent.transform.position) < minAgentSpacing)
            {
                return false;
            }
        }
        
        // Check for obstacles
        Collider[] colliders = Physics.OverlapSphere(position, 1f);
        return colliders.Length == 0;
    }
    
    private void UpdatePerformanceMetrics()
    {
        performanceMetrics.Clear();
        
        float totalDetections = 0f;
        float totalReward = 0f;
        float averageEpisodeLength = 0f;
        
        foreach (var agent in agents)
        {
            totalDetections += agent.detectionsThisEpisode;
            totalReward += agent.GetCumulativeReward();
            averageEpisodeLength += agent.episodeTimer;
        }
        
        performanceMetrics["Average Detections"] = totalDetections / agents.Count;
        performanceMetrics["Average Reward"] = totalReward / agents.Count;
        performanceMetrics["Average Episode Length"] = averageEpisodeLength / agents.Count;
        
        LogPerformanceMetrics();
    }
    
    private void LogPerformanceMetrics()
    {
        string metrics = "Training Performance:\n";
        foreach (var kvp in performanceMetrics)
        {
            metrics += $"{kvp.Key}: {kvp.Value:F2}\n";
        }
        Debug.Log(metrics);
    }
    
    void OnDestroy()
    {
        CancelInvoke("UpdatePerformanceMetrics");
    }
}