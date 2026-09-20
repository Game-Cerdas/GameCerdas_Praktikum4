using UnityEngine;

public class PlayerAlert : MonoBehaviour
{
    [Header("Help Call Settings")]

    [SerializeField]
    private float callRadius = 10f;

    [Header("Audio")]

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip helpCallClip;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BroadcastHelpCall();
            PlayHelpCallSound();
        }
    }

    // sebar panggilan tolong ke npc sekitar
    private void BroadcastHelpCall()
    {
        SteeringAgent[] allAgents =
            FindObjectsOfType<SteeringAgent>();

        foreach (SteeringAgent agent in allAgents)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    agent.transform.position
                );

            if (distance <= callRadius)
            {
                agent.OnHelpCallHeard(transform);
            }
        }
    }

    // mainkan suara teriakan minta tolong
    private void PlayHelpCallSound()
    {
        if (audioSource != null && helpCallClip != null)
        {
            audioSource.PlayOneShot(helpCallClip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, callRadius);
    }
}
