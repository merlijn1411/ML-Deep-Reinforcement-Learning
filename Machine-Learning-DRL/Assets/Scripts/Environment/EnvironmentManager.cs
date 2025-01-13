using TMPro;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class EnvironmentManager : MonoBehaviour
{
    [SerializeField] private float timer;
    [SerializeField] private float defaultBlockDistance;

    [SerializeField] private GameObject seekerAgent;
    [SerializeField] private GameObject runnerAgent;
    
    [SerializeField] private Timer countDown;
    public UnityEvent onTimeReachesZero;

    [SerializeField] private TextMeshPro seekerVisualCounter;
    [SerializeField] private TextMeshPro runnerVisualCounter;

    private int _seekerCounter = 0;
    private int _runnerCounter = 0;
    
    
    private void Start()
    {
        BeginEpisodePosition();
        runnerVisualCounter.text = $"Runner: {_runnerCounter}";
        seekerVisualCounter.text = $"Seeker: {_seekerCounter}";
    }

    public void BeginEpisodePosition()
    {
        countDown.t = timer; //reset de timer
        
        // deze is verbonden met het Curriculum system in het yaml file.
        var blockDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("TrainingArea_Seeker", defaultBlockDistance);
        
        seekerAgent.transform.localPosition = new Vector3( -blockDistance, 1f, -6);
        seekerAgent.transform.eulerAngles = new Vector3(0, 90, 0);  
        
        runnerAgent.transform.localPosition = new Vector3(blockDistance,1f,-6);
        runnerAgent.transform.eulerAngles = new Vector3(0, -90, 0);  
    }
    
    private void UpdateRunnerCounter()
    {
        ++_runnerCounter;
        seekerVisualCounter.text = $"Runner: {_runnerCounter}";
    }

    public void UpdateSeekerCounter()
    {
        ++_seekerCounter;
        seekerVisualCounter.text = $"Seeker: {_seekerCounter}";
    }

    private void Update()
    {
        if (!(countDown.t <= 0)) return;
        BeginEpisodePosition();
        UpdateRunnerCounter();
        onTimeReachesZero.Invoke();
        
        
    }
}
