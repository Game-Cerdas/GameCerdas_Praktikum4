using UnityEngine;

// Level 1 - Perubahan warna NPC sesuai kondisi/state steering.
[RequireComponent(typeof(SteeringAgent))]
public class SteeringAgentVisual : MonoBehaviour
{
    [Header("Referensi")]

    [SerializeField]
    private SteeringAgent agent;

    [SerializeField]
    private Renderer targetRenderer;

    [Header("Warna per State")]

    [SerializeField]
    private Color arriveColor = Color.red;

    [SerializeField]
    private Color wanderColor = new Color(0.2f, 0.45f, 1f);

    [SerializeField]
    private Color avoidingColor = Color.yellow;

    [SerializeField]
    private Color fleeColor = Color.magenta;

    [SerializeField]
    private Color pursueColor = new Color(0.1f, 0.9f, 0.3f);

    [Header("Transisi")]

    [SerializeField]
    private float colorLerpSpeed = 10f;

    private MaterialPropertyBlock propertyBlock;
    private Color currentColor;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private void Reset()
    {
        agent = GetComponent<SteeringAgent>();
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<SteeringAgent>();
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        currentColor = GetColorForState(SteeringState.Wander);

        ApplyColor(currentColor);
    }

    private void Update()
    {
        if (agent == null || targetRenderer == null)
        {
            return;
        }

        Color desiredColor =
            GetColorForState(agent.CurrentState);

        currentColor =
            Color.Lerp(
                currentColor,
                desiredColor,
                Mathf.Clamp01(colorLerpSpeed * Time.deltaTime)
            );

        ApplyColor(currentColor);
    }

    private Color GetColorForState(SteeringState state)
    {
        switch (state)
        {
            case SteeringState.Arrive:
                return arriveColor;

            case SteeringState.Wander:
                return wanderColor;

            case SteeringState.Avoiding:
                return avoidingColor;

            case SteeringState.Flee:
                return fleeColor;

            case SteeringState.Pursue:
                return pursueColor;

            default:
                return Color.white;
        }
    }

    private void ApplyColor(Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        targetRenderer.GetPropertyBlock(propertyBlock);

        // _BaseColor untuk URP/HDRP, _Color untuk Built-in.
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }
}
