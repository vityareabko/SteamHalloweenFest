using UnityEngine;

public class CrashToggle : MonoBehaviour
{
    public Rigidbody rb;
    public float forceMult = 1f;

    public float minMagToBreak = 2f;

    public AudioSource audioSource;

    public GameObject frags;
    public Rigidbody[] fragRigids;
    public float maxForce;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.parent == null) return;

        string surfaceName = collision.transform.parent.name;
        if (surfaceName != null && (surfaceName == "Walls" || surfaceName == "Floors"))
        {
            if (collision.relativeVelocity.magnitude > minMagToBreak)
            {
                float mag = rb.linearVelocity.magnitude * forceMult;
                frags.transform.parent = null;
                gameObject.SetActive(false);
                frags.SetActive(true);
                audioSource.Play();

                for (int i = 0; i < fragRigids.Length; i++)
                {
                    float fragmentMass = fragRigids[i].mass;
                    float forceFactor = (mag * forceMult) / fragmentMass;

                    Vector3 collisionDirection = -(collision.relativeVelocity * 1.4f).normalized;
                    Vector3 surfaceNormal = collision.contacts[0].normal;
                    Vector3 finalDirection = Vector3.Reflect(collisionDirection, surfaceNormal).normalized;

                    Vector3 randomDir = Random.onUnitSphere;
                    float alignmentFactor = Vector3.Dot(collisionDirection.normalized, surfaceNormal.normalized);
                    randomDir *= alignmentFactor;

                    Vector3 force = (randomDir + 0.5f * finalDirection).normalized * forceFactor * maxForce;

                    fragRigids[i].AddForce(force, ForceMode.Impulse);
                }
            }
        }
    }
}
