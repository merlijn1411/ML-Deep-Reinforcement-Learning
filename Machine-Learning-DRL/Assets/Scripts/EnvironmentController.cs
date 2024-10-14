using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class EnvironmentController : MonoBehaviour
{
    [SerializeField] private float timer;
    [SerializeField] private float defaultBlockDistance;

    [SerializeField] private GameObject seekerAgent;
    [SerializeField] private GameObject runnerAgent;
    
    [SerializeField] private Timer countDown;
    public UnityEvent onTimeReachesZero;

    private void Start()
    {
        BeginEpisodePosition();
    }

    public void BeginEpisodePosition()
    {
        countDown.t = timer; //reset de timer
        
        // deze is verbonden met het Curriculum system in het yaml file.
        var blockDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("TrainingArea_Seeker", defaultBlockDistance);
        
        seekerAgent.transform.localPosition = new Vector3( -blockDistance, 1f, -1);
        seekerAgent.transform.eulerAngles = new Vector3(0, 90, 0);  
        
        runnerAgent.transform.localPosition = new Vector3(blockDistance,1f,-1);
        runnerAgent.transform.eulerAngles = new Vector3(0, -90, 0);  
    }

    private void Update()
    {
        if (!(countDown.t <= 0)) return;
        BeginEpisodePosition();
        onTimeReachesZero.Invoke();
    }
}
