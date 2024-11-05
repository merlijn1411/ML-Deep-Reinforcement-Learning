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
    
    [SerializeField] private Timer countDown;

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
    }

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

        var forwardAction = act[(int)AgentActions.Forward];
        var sidewardAction = act[(int)AgentActions.Sideward];
        var rotationAction = act[(int)AgentActions.Rotation];
        var jumpAction = act[(int)AgentActions.Jump];
        
        var speedModifier = _isGrounded ? 1f : 0.5f;
        var dirToGo = Vector3.zero;

        // Bewegingsrichting instellen
        if (forwardAction == 1) dirToGo += speedModifier * _agentTransform.forward;
        else if (forwardAction == 2) dirToGo -= speedModifier * _agentTransform.forward;

        if (sidewardAction == 1) dirToGo += speedModifier * _agentTransform.right;
        else if (sidewardAction == 2) dirToGo -= speedModifier * _agentTransform.right;

        // Rotatierichting instellen
        var rotateDir = rotationAction == 1 ? -_agentTransform.up : 
            rotationAction == 2 ? _agentTransform.up : 
            Vector3.zero;

        ApplyMovement(dirToGo);
        ApplyRotation(rotateDir);
        
        // Gravity boost als agent niet op de grond is en niet springt
        if (!_isGrounded && jumpAction == 0 && _rBody.velocity.y > -_maxFallSpeed)
        {
            _rBody.AddForce(Vector3.down * _forceDownMultiplier, ForceMode.Acceleration);
        }

        // Sprongactie
        if (jumpAction == 1)
        {
            Jump(jumpForce);
        }
        
        // Bereken afstand tot doel minder vaak
        if (_frameCounter % 5 == 0)
        {
            DistanceToTarget();
        }
        _frameCounter++;
    }
    
    private void ApplyRotation(Vector3 rotateDir)
    {
        transform.Rotate(rotateDir, rotationSpeed);
    }
    
    private void ApplyMovement(Vector3 dirToGo)
    {
        var horizontalVelocity = dirToGo.normalized * walkSpeed;
        _rBody.velocity = new Vector3(horizontalVelocity.x, _rBody.velocity.y, horizontalVelocity.z);
    }
    
    private void Jump(float jumpForce)
    {
        if (_isGrounded)
        {
            _rBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _isGrounded = false; // Zorg ervoor dat deze wordt gezet om dubbele sprongen te vermijden
        }
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
            AddReward(-0.02f); // Give a small negative reward for getting closer
        else
            AddReward(0.02f); // Give a small Positve reward for getting Furthur away
        
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
        else if (other.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f - 0.01f * _distanceToTarget); 
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();

        if (Input.GetKey(KeyCode.W))
            discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.S))
            discreteActionsOut[0] = 2;

        if (Input.GetKey(KeyCode.D))
            discreteActionsOut[1] = 2;
        else if (Input.GetKey(KeyCode.A))
            discreteActionsOut[1] = 1;

        if (Input.GetKey(KeyCode.Space))
            discreteActionsOut[3] = 1;
    }
}
