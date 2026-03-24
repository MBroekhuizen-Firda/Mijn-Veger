using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CharacterMover : MonoBehaviour
{
    public event Action OnStartedMoving;
    public event Action OnArrived;

    [Header("Settings")]
    public float StoppingDistance = 0.1f;

    private NavMeshAgent agent;
    private CharacterAnimator characterAnimator;
    private bool isMoving;
    private List<Vector3> waypoints;
    private int currentWaypointIndex;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        characterAnimator = GetComponent<CharacterAnimator>();

        if (agent != null)
        {
            agent.stoppingDistance = StoppingDistance;
        }
    }

    void Update()
    {
        if (agent == null) return;

        if (isMoving && !agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // Check if there are more waypoints to follow
                if (waypoints != null && currentWaypointIndex < waypoints.Count - 1)
                {
                    currentWaypointIndex++;
                    agent.SetDestination(waypoints[currentWaypointIndex]);
                }
                else
                {
                    isMoving = false;
                    waypoints = null;
                    currentWaypointIndex = 0;
                    if (characterAnimator != null)
                        characterAnimator.SetSpeed(0f);
                    OnArrived?.Invoke();
                }
            }
        }
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (agent == null) return;

        waypoints = null;
        currentWaypointIndex = 0;
        agent.SetDestination(targetPosition);
        isMoving = true;

        if (characterAnimator != null)
            characterAnimator.SetSpeed(1f);

        OnStartedMoving?.Invoke();
    }

    public void MoveAlongPath(List<Vector3> path)
    {
        if (agent == null || path == null || path.Count == 0) return;

        waypoints = path;
        currentWaypointIndex = 0;
        agent.SetDestination(waypoints[0]);
        isMoving = true;

        if (characterAnimator != null)
            characterAnimator.SetSpeed(1f);

        OnStartedMoving?.Invoke();
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public void Stop()
    {
        if (agent != null)
        {
            agent.ResetPath();
        }
        isMoving = false;
        waypoints = null;
        currentWaypointIndex = 0;
        if (characterAnimator != null)
            characterAnimator.SetSpeed(0f);
    }
}
