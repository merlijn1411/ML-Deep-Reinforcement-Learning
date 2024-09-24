using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;

public class SeekerAgent : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Timer countDown;
    [SerializeField] private float previousDistance;
    [SerializeField] private float distanceToTarget;

    private Rigidbody _rBody;

    [SerializeField] private UnityEvent onNewEpisode;

    
    
    public override void Initialize()
    {
        _rBody = GetComponent<Rigidbody>();
    }
    public override void OnEpisodeBegin()
    {
        countDown.t = 30f;
        
        _rBody.velocity = Vector3.zero;
        _rBody.angularVelocity = Vector3.zero;
        
        transform.localPosition = new Vector3( -6, 1f, -1);
        transform.localRotation = new Quaternion();
        
        target.localPosition = new Vector3(4,1f,-1);
        target.localRotation = new Quaternion();
        
        onNewEpisode.Invoke();
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

        var movement = actionBuffers.DiscreteActions[0];
        var jump = actionBuffers.DiscreteActions[1];

        if (movement == 1) { controlSignal.x = -1; }
        if (movement == 2) { controlSignal.x = 1; }
        if (movement == 3) { controlSignal.z = -1; }
        if (movement == 4) { controlSignal.z = 1; }
        
        controlSignal.Normalize();
        
        transform.Translate(controlSignal * walkSpeed, Space.World);

        FaceRotate(controlSignal);
        
        var distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        
        TimerReachedZeroReward(distanceToTarget);
        AgentFellOff();
        DistanceToTarget();
        SetReward(-0.001f);
    }

    private void FaceRotate(Vector3 controlSignal)
    {
        if (controlSignal == Vector3.zero) return;
        var toRotationX = Quaternion.LookRotation(controlSignal, Vector3.up);
    
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotationX, rotationSpeed);
    }

    private void TimerReachedZeroReward(float distanceToTarget)
    {
        if (countDown.t <= 0) //als de teller op nul komt beindigd het de episode
        {
            var reward = (distanceToTarget / 20f) * -1f; //de reward word op de hand van hoe dichtbij hij bij de target komt berekend.
            SetReward(reward);
            EndEpisode();
        }
    }
    private void DistanceToTarget()
    {
        distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        // Reward for reducing distance to the cube
        if (previousDistance > distanceToTarget)
        {
            AddReward(0.02f); // Give a small positive reward for getting closer
        }
        previousDistance = distanceToTarget;
    }
    private void MazeRewards()
    {
        
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag($"Runner"))
        {
            print("+1");
            SetReward(1f);
            EndEpisode();
        }        
        if (other.gameObject.CompareTag($"Wall"))
        {
            SetReward(-0.01f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag($"RewardPoint"))
        {
            SetReward(0.3f);
            Destroy(other.gameObject);
        }
    }

    private void AgentFellOff()
    {
        if (!(transform.localPosition.y < 0)) return;
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