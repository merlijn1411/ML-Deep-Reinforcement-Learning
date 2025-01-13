using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Canvas menu;
    [SerializeField] private Button seekerButton;
    [SerializeField] private Button runnerButton;

    [SerializeField] private SeekerAgent seeker;
    [SerializeField] private RunnerAgent runner;
    
    
    private void Awake()
    {
        Time.timeScale = 0f;
        seekerButton.onClick.AddListener(StartGameWithSeeker);
        runnerButton.onClick.AddListener(StartGameWithRunner);
    }
    
    private void StartGameWithSeeker()
    {
        seeker.SetBehaviourType(BehaviorType.HeuristicOnly);
        runner.SetBehaviourType(BehaviorType.InferenceOnly);
        Time.timeScale = 1f;
        menu.enabled = false;
    }
    
    private void StartGameWithRunner()
    {
        runner.SetBehaviourType(BehaviorType.HeuristicOnly);
        seeker.SetBehaviourType(BehaviorType.InferenceOnly);
        Time.timeScale = 1f;
        menu.enabled = false;
    }
}
