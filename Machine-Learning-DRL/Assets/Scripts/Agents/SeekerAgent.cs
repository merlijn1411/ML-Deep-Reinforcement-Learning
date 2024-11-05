using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;

public class SeekerAgent : Agent
{
    private Transform _agentTransform; 
    [SerializeField] private GameObject target;
    
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float rotationSpeed;

    private const float _forceDownMultiplier = 50f;
    private const float _maxFallSpeed = -20f;
        
    private Rigidbody _rBody;
    private Rigidbody _targetRbody;
    private bool _isGrounded;
    
    private float _previousDistance;
    private float _distanceToTarget;
    
    [SerializeField] private UnityEvent onNewEpisode;
    
    private int _frameCounter;

    private void Awake()
    {
        _agentTransform = transform;
        _rBody = GetComponent<Rigidbody>();
        _targetRbody = target.GetComponent<Rigidbody>();
    }
    
    public override void Initialize() { }

    public override void OnEpisodeBegin()
    {
        _rBody.velocity = Vector3.zero;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localRotation.y);
        
        Vector3 toTarget = target.transform.localPosition - transform.localPosition;
        sensor.AddObservation(toTarget.magnitude * rotationSpeed);
        sensor.AddObservation(toTarget.normalized);
        sensor.AddObservation(_rBody.velocity);
        sensor.AddObservation(_targetRbody.velocity);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        CheckIfGrounded();
        MoveAgent(actionBuffers.DiscreteActions);
        AddReward(-0.001f);
    }
    
    private void MoveAgent(ActionSegment<int> act)
    {
        var speedModifier = _isGrounded ? 1f : 0.5f;
        Vector3 dirToGo = Vector3.zero;

        // Movement direction
        dirToGo += act[(int)AgentActions.Forward] == 1 ? speedModifier * _agentTransform.forward : Vector3.zero;
        dirToGo -= act[(int)AgentActions.Forward] == 2 ? speedModifier * _agentTransform.forward : Vector3.zero;

        dirToGo += act[(int)AgentActions.Sideward] == 1 ? speedModifier * _agentTransform.right : Vector3.zero;
        dirToGo -= act[(int)AgentActions.Sideward] == 2 ? speedModifier * _agentTransform.right : Vector3.zero;

        ApplyMovement(dirToGo);
        ApplyRotation(GetRotationDirection(act));

        if (!_isGrounded && act[(int)AgentActions.Jump] == 0 && _rBody.velocity.y > -_maxFallSpeed)
        {
            _rBody.AddForce(Vector3.down * _forceDownMultiplier, ForceMode.Acceleration);
        }

        if (act[(int)AgentActions.Jump] == 1)
        {
            Jump();
        }
        
        // Update distance to target every 5 frames
        if (_frameCounter % 5 == 0)
        {
            UpdateDistanceToTarget();
        }
        _frameCounter++;
    }
    
    private void ApplyRotation(Vector3 rotateDir)
    {
        transform.Rotate(rotateDir, rotationSpeed);
    }
    
    private void ApplyMovement(Vector3 dirToGo)
    {
        _rBody.velocity = new Vector3(dirToGo.x * walkSpeed, _rBody.velocity.y, dirToGo.z * walkSpeed);
    }
    
    private void Jump()
    {
        if (_isGrounded)
        {
            _rBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _isGrounded = false;
        }
    }
    
    private void UpdateDistanceToTarget()
    {
        _distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        if (_previousDistance > _distanceToTarget)
            AddReward(0.02f);
        else
            AddReward(-0.02f);
        
        _previousDistance = _distanceToTarget;
    }
    
    public void TimerReachedZeroReward()
    {
        SetReward((_distanceToTarget / 20f) * -1f);
        EndEpisode();
    }
    
    private void CheckIfGrounded()
    {
        if (_isGrounded) return; // Only raycast if not grounded
        
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.1f))
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
        if (other.gameObject.CompareTag("Runner"))
        {
            SetReward(1f);
            onNewEpisode.Invoke();
            EndEpisode();
        }        
        else if (other.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f - 0.01f * _distanceToTarget); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("RewardPoint"))
        {
            SetReward(0.3f);
            Destroy(other.gameObject);
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();

        discreteActionsOut[0] = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? 2 : 0;
        discreteActionsOut[1] = Input.GetKey(KeyCode.D) ? 2 : Input.GetKey(KeyCode.A) ? 1 : 0;
        discreteActionsOut[3] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    private Vector3 GetRotationDirection(ActionSegment<int> act)
    {
        return act[(int)AgentActions.Rotation] switch
        {
            1 => -_agentTransform.up,
            2 => _agentTransform.up,
            _ => Vector3.zero,
        };
    }
}
