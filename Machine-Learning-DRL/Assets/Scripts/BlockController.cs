using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class BlockController : MonoBehaviour
{
    [SerializeField] private float timer;
    [SerializeField] private float defaultBlockDistance = 6.0f;
    private float _blockDistance;

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
        _blockDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("block_distance", defaultBlockDistance);
        
        seekerAgent.transform.localPosition = new Vector3( -_blockDistance, 1f, -1);
        seekerAgent.transform.localRotation = new Quaternion();
        
        runnerAgent.transform.localPosition = new Vector3(_blockDistance,1f,-1);
        runnerAgent.transform.localRotation = new Quaternion();
    }

    private void Update()
    {
        if (!(countDown.t <= 0)) return;
        BeginEpisodePosition();
        onTimeReachesZero.Invoke();
    }
}
