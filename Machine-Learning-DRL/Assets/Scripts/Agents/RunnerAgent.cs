using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Events;

public class RunnerAgent : Agent
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
    
    private int _frameCounter = 0;

    private void Awake()
    {
        _agentTransform = transform;
        _rBody = GetComponent<Rigidbody>();
        _targetRbody = target.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        _rBody.velocity = Vector3.zero;
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localRotation.y);
        
        // Calculate direction to target
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
        AddReward(0.001f);
    }
    
    private void MoveAgent(ActionSegment<int> act)
    {
        var forwardAction = act[(int)AgentActions.Forward];
        var sidewardAction = act[(int)AgentActions.Sideward];
        var rotationAction = act[(int)AgentActions.Rotation];
        var jumpAction = act[(int)AgentActions.Jump];
        
        var speedModifier = _isGrounded ? 1f : 0.5f;
        Vector3 dirToGo = Vector3.zero;

        // Set movement direction
        if (forwardAction == 1) dirToGo += speedModifier * _agentTransform.forward;
        if (forwardAction == 2) dirToGo -= speedModifier * _agentTransform.forward;
        if (sidewardAction == 1) dirToGo += speedModifier * _agentTransform.right;
        if (sidewardAction == 2) dirToGo -= speedModifier * _agentTransform.right;

        ApplyMovement(dirToGo);
        ApplyRotation(GetRotationDirection(rotationAction));
        
        // Gravity boost if not grounded and not jumping
        if (!_isGrounded && jumpAction == 0 && _rBody.velocity.y > -_maxFallSpeed)
        {
            _rBody.AddForce(Vector3.down * _forceDownMultiplier, ForceMode.Acceleration);
        }
        if (jumpAction == 1) Jump();

        // Update distance to target less frequently
        if (_frameCounter % 5 == 0) UpdateDistanceToTarget();
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
            _isGrounded = false; // Prevent double jumps
        }
    }
    
    public void TimerReachedZeroReward()
    {
        SetReward(1f);
        EndEpisode();
    }
    
    private void UpdateDistanceToTarget()
    {
        _distanceToTarget = Vector3.Distance(transform.localPosition, target.transform.localPosition);
        AddReward(_previousDistance > _distanceToTarget ? -0.02f : 0.02f);
        _previousDistance = _distanceToTarget;
    }
    
    private void CheckIfGrounded()
    {
        if (_isGrounded) return; // Only raycast if not grounded

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.1f))
        {
            _isGrounded = hit.collider.CompareTag("walkableSurface");
        }
        else
        {
            _isGrounded = false;
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Seeker"))
        {
            SetReward(-1f);
            onNewEpisode.Invoke();
            EndEpisode();
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f - 0.01f * _distanceToTarget); 
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

    private Vector3 GetRotationDirection(int rotationAction)
    {
        return rotationAction switch
        {
            1 => -_agentTransform.up,
            2 => _agentTransform.up,
            _ => Vector3.zero,
        };
    }
}
