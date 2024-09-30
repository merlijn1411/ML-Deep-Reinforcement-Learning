using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RunnerAgent : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Timer countDown;
    private float _previousDistance; 
    private float _distanceToTarget;

    private Rigidbody _rBody;
    
    public override void Initialize()
    {
        _rBody = GetComponent<Rigidbody>();
    }
    
    public override void OnEpisodeBegin()
    {
        _rBody.velocity = Vector3.zero;
        _rBody.angularVelocity = Vector3.zero;
    }
    
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
        _distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

        VelocityController();
        
        TimerReachedZeroReward();
        
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
    
    public void TimerReachedZeroReward()
    {
        SetReward(1f);
        EndEpisode();
    }
    
    private void DistanceToTarget()
    {
        // Reward for reducing distance to the cube
        if (_previousDistance > _distanceToTarget)
        {
            AddReward(-0.02f); // Give a small negative reward for getting closer to the target
        }
        _previousDistance = _distanceToTarget;
    }

    private void VelocityController()
    {
        if (_rBody.velocity.magnitude > 1f)
            _rBody.velocity = Vector3.ClampMagnitude(_rBody.velocity, 1);
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag($"Seeker"))
        {
            print("Runner: -1");
            SetReward(-1f);
            EndEpisode();
        }        
        if (other.gameObject.CompareTag($"Wall"))
        {
            SetReward(-0.01f);
        }
    }
    
    private void AgentFellOff()
    {
        if (!(transform.localPosition.y < 0)) return;
        SetReward(-1f);
        EndEpisode();
    }
}
