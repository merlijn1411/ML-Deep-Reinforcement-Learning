using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SeekerAgent : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float rotationSpeed;
    
    [SerializeField] private float t;
    [SerializeField] private TextMeshProUGUI timeCounter;
    
    private Rigidbody _rBody;
    
    public override void Initialize()
    {
        _rBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        t -= 1f * Time.deltaTime;
        var tCounter = Mathf.Ceil(t);
        timeCounter.text = $"{tCounter}";
    }

    public override void OnEpisodeBegin()
    {
        t = 30f;
        
        _rBody.velocity = Vector3.zero;
        _rBody.angularVelocity = Vector3.zero;
        
        transform.localPosition = new Vector3( -4, 1f, -1);
        target.localPosition = new Vector3(4,1f,-1);
    }

    //Deze method houd bij welke gegevens hij moet onthouden dat word gebruikt om een vedere beslissing te maken (dus action). 
    public override void CollectObservations(VectorSensor sensor)
    {
        //Target en agent position
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(transform.localPosition);
        
        //Agent velocity
        sensor.AddObservation(_rBody.velocity.x);
        sensor.AddObservation(_rBody.velocity.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //Dit is inprencipe het zelfde als Input.GetAxis zodat de Machine kan leren bewegen.
        var controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        controlSignal.Normalize();
        
        transform.Translate(controlSignal * walkSpeed, Space.World);

        FaceRotate(controlSignal);
        
        var distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        
        TimerReachedZeroReward(distanceToTarget);
        TargetDistanceCheck(distanceToTarget);
        AgentFellOff();
    }

    private void FaceRotate(Vector3 controlSignal)
    {
        if (controlSignal == Vector3.zero) return;
        var toRotationX = Quaternion.LookRotation(controlSignal, Vector3.up);
    
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotationX, rotationSpeed);
    }

    private void TimerReachedZeroReward(float distanceToTarget)
    {
        if (t <= 0) //als de teller op nul komt beindigd het de episode
        {
            var reward = (distanceToTarget / 10f) * -1f; //de reward word op de hand van hoe dichtbij hij bij de target komt berekend.
            Debug.Log(reward);
            SetReward(reward);
            EndEpisode();   
        }
    } 

    private void TargetDistanceCheck(float distanceToTarget)
    {
        if (!(distanceToTarget < 1f)) return;
        print("+1");
        SetReward(1f);
        EndEpisode();
    }

    private void AgentFellOff()
    {
        if (!(transform.localPosition.y < 0)) return;
        print("-1");
        SetReward(-1f);
        EndEpisode();
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Mathf.Lerp(0,Input.GetAxis("Horizontal"),0.8f);
        continuousActionsOut[1] = Mathf.Lerp(0,Input.GetAxis("Vertical"),0.8f);
    }
}

