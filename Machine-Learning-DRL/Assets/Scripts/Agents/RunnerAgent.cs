using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;
public class RunnerAgent : Agent
{
    [SerializeField] private GameObject target;
    [SerializeField] private Transform obstacleFence;
    
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float rotationSpeed;
    
    [SerializeField] private Timer countDown;

    private const float _forceDownMultiplier = 50f;
    
    private Rigidbody _rBody;
    private Rigidbody _targetRbody;
    private bool _isGrounded;
    
    private float _previousDistance;
    private float _distanceToTarget;

    [SerializeField] private UnityEvent onNewEpisode;
    
    public override void Initialize()
    {
        _rBody = GetComponent<Rigidbody>();
        _targetRbody = target.GetComponent<Rigidbody>();
    }
    
    public override void OnEpisodeBegin()
    {
        _rBody.velocity = Vector3.zero;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        //Agent y axis rotation(1)
        sensor.AddObservation(transform.localRotation.y);
        
        //Vector van target naar ball (direction naar target)(3)
        var toTarget = new Vector3((target.transform.localPosition.x - transform.localPosition.x) * rotationSpeed,
            (target.transform.localPosition.y - transform.localPosition.y),(target.transform.localPosition.z - transform.localPosition.z)*rotationSpeed);
        
        sensor.AddObservation(toTarget.magnitude);
        
        //Aftsand van de target(1)
        sensor.AddObservation(toTarget.normalized);
        
        //Wall localPosition(3)
        sensor.AddObservation(obstacleFence.localPosition);
            
        //Agent velocity(3)
        sensor.AddObservation(_rBody.velocity);
        
        // target velocity (3 floats)
        sensor.AddObservation(_targetRbody.velocity.y);
        sensor.AddObservation(_targetRbody.velocity.z * rotationSpeed);
        sensor.AddObservation(_targetRbody.velocity.x * rotationSpeed);
    }
    
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        CheckIfGrounded();
        
        MoveAgent(actionBuffers.DiscreteActions);
        AddReward(0.001f);
    }
    
    private void MoveAgent(ActionSegment<int> act)
    {
        //Dit is inprencipe het zelfde als Input.GetAxis zodat de Machine kan leren bewegen.
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var jumpDir = Vector3.zero;

        var forwardAction = act[0];
        var sidewardAction = act[1];
        var rotationAction  = act[2];
        var jumpAction = act[3];
        
        var speedModifier = _isGrounded ? 1f : 0.5f;
        
        dirToGo += forwardAction switch
        {
            1 => speedModifier * transform.forward * 1f,
            2 => speedModifier * transform.forward * -1f,
            _ => Vector3.zero
        };

        dirToGo += sidewardAction switch
        {
            1 => speedModifier * transform.right,
            2 => speedModifier * transform.right * -1f,
            _ => Vector3.zero
        };
        
        rotateDir = rotationAction switch
        {
            1 => transform.up * -1f,
            2 => transform.up * 1f,
            _ => Vector3.zero
        };
        

        transform.Rotate(rotateDir, rotationSpeed);
        
        var horizontalVelocity = dirToGo.normalized * walkSpeed;
        _rBody.velocity = new Vector3(horizontalVelocity.x, _rBody.velocity.y, horizontalVelocity.z);
        
        // Gravity boost als agent niet op de grond is en niet springt
        if (!_isGrounded && jumpAction == 0)
        {
            _rBody.AddForce(Vector3.down * _forceDownMultiplier, ForceMode.Acceleration);
        }
        
        if (_isGrounded && jumpAction == 1)
        {
            jumpDir = Vector3.up * jumpForce;
            Jump(jumpDir);
        }
        
        DistanceToTarget();
    }
    
    private void Jump(Vector3 jumpDir)
    {
        _rBody.AddForce(jumpDir, ForceMode.Impulse);
    }
    
    public void TimerReachedZeroReward()
    {
        SetReward(1f);
        EndEpisode();
    }
    
    private void DistanceToTarget()
    {
        _distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        // Reward for reducing distance to the cube
        if (_previousDistance > _distanceToTarget)
        {
            AddReward(-0.02f); // Give a small negative reward for getting closer
        }
        _previousDistance = _distanceToTarget;
    }
    
    
    private void CheckIfGrounded()
    {
        RaycastHit hit;
        const float distance = 1.1f;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distance))
        {
            if (hit.collider.CompareTag("walkableSurface"))
            {
                _isGrounded = true;
                return;
            }
        }
        _isGrounded = false;
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag($"Seeker"))
        {
            SetReward(-1f);
            onNewEpisode.Invoke();
            EndEpisode();
        }
        
        if (other.gameObject.CompareTag($"Wall"))
            AddReward(-0.01f);
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.D))
        {
            // rotate right
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            // move forward
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            // rotate left
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            // move backward
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            // move left
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // move right
            discreteActionsOut[0] = 2;
        }
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
