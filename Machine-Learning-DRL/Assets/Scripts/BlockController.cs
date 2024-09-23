using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [SerializeField] private float defaultBlockDistance = 2.0f;
    private float _blockDistance;

    [SerializeField] private GameObject seekerAgent;
    [SerializeField] private GameObject runnerAgent;

    private void Start()
    {
        BeginEpisodePosition();
    }

    public void BeginEpisodePosition()
    {
        // deze is verbonden met het Curriculum system in het yaml file.
        _blockDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("block_distance", defaultBlockDistance);
        
        seekerAgent.transform.localPosition = new Vector3( -_blockDistance, 1f, -1);
        runnerAgent.transform.localPosition = new Vector3(_blockDistance,1f,-1);
    }
}
