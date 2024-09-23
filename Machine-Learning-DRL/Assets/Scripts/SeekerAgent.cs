using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine.Events;
using UnityEngine;

public class SeekerAgent : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform wall;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float rotationSpeed;

    private Rigidbody _rBody;

    [SerializeField] private UnityEvent onNewEpisode;

    [SerializeField] private Timer countDown;

    [SerializeField] private float distanceToTarget;
    [SerializeField] private float previousDistanceToTarget;
    [SerializeField] private float distanceToWall;
    [SerializeField] float timeStuckNearWall;


    public override void Initialize()
    {
        _rBody = GetComponent<Rigidbody>();
    }
    public override void OnEpisodeBegin()
    {
        countDown.t = 30f;

        _rBody.velocity = Vector3.zero;
        _rBody.angularVelocity = Vector3.zero;

        onNewEpisode.Invoke();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(_rBody.velocity.x);
        sensor.AddObservation(_rBody.velocity.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var controlSignal = Vector3.zero;
        var movement = actionBuffers.DiscreteActions[0];

        if (movement == 1) { controlSignal.x = -1; }
        if (movement == 2) { controlSignal.x = 1; }
        if (movement == 3) { controlSignal.z = -1; }
        if (movement == 4) { controlSignal.z = 1; }

        controlSignal.Normalize();

        transform.Translate(controlSignal * walkSpeed, Space.World);
        FaceRotate(controlSignal);

        var distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);

        RewardForMovingTowardTarget(distanceToTarget);
        TimerReachedZeroReward(distanceToTarget);
        AgentFellOff();
        AddReward(-0.001f);
        ToLongCloseToWall();
    }

    private void FaceRotate(Vector3 controlSignal)
    {
        if (controlSignal == Vector3.zero) return;
        var toRotationX = Quaternion.LookRotation(controlSignal, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotationX, rotationSpeed);
    }

    // Reward for decreasing the distance to the target
    private void RewardForMovingTowardTarget(float distanceToTarget)
    {
        if (previousDistanceToTarget > distanceToTarget)
        {
            AddReward(0.02f);  // Reward for moving toward the cube
        }
        previousDistanceToTarget = distanceToTarget;
    }

    private void TimerReachedZeroReward(float distanceToTarget)
    {
        if (countDown.t <= 0)
        {
            var reward = (distanceToTarget / 10f) * -1f; // Reward based on how close the agent gets
            SetReward(reward);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag($"Runner"))
        {
            print("+1");
            SetReward(1f);  // Large reward for touching the cube
            EndEpisode();
        }
        if (other.gameObject.CompareTag($"Wall"))
        {
            print("touch wall :(");
            SetReward(-0.8f);  // Penalty for touching the wall
            EndEpisode();
        }
    }

    private void ToLongCloseToWall()
    {
        distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        distanceToWall = Vector3.Distance(transform.localPosition, wall.localPosition);

        if (previousDistanceToTarget > distanceToTarget)
        {
            AddReward(0.02f); // Reward for moving toward the cube
            timeStuckNearWall = 0f; // Reset the timer since progress is made
        }
        else
        {
            // Penalize for being close to the wall without progress
            if (distanceToWall < 2.5f)
            {
                timeStuckNearWall += Time.deltaTime;
                if (timeStuckNearWall > 1f) // If stuck near the wall for more than 1 second
                {
                    AddReward(-0.1f);  // Larger penalty for staying near the wall too long
                }
            }
        }

        previousDistanceToTarget = distanceToTarget;
    }

    private void AgentFellOff()
    {
        if (!(transform.localPosition.y < 0)) return;
        print("-1");
        SetReward(-1f); // Penalize for falling off
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Mathf.Lerp(0, Input.GetAxis("Horizontal"), 0.8f);
        continuousActionsOut[1] = Mathf.Lerp(0, Input.GetAxis("Vertical"), 0.8f);
    }
}
