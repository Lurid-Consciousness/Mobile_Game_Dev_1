using UnityEngine;
using UnityEngine.Rendering;

public class HoneyPuddle : MonoBehaviour
{
    public float slowMultiplier = 0.5f;
    public float lifetime = 5f;

    private Collider puddleCollider;
    private Renderer puddleRenderer;
    private Material puddleMaterial;

    void Awake()
    {
        puddleCollider = GetComponent<Collider>();
        puddleRenderer = GetComponent<Renderer>();

        if (puddleRenderer != null)
            puddleMaterial = puddleRenderer.material;
    }

    public void ShowWarning()
    {
        if (puddleCollider != null)
            puddleCollider.enabled = false;

        if (puddleMaterial != null)
        {
            SetTransparent(true);
            puddleMaterial.color = new Color(1f, 0.75f, 0f, 0.3f);
        }
    }

    public void Activate()
    {
        gameObject.name = "Honey Puddle";

        if (puddleCollider != null)
        {
            puddleCollider.enabled = true;
            puddleCollider.isTrigger = true;
        }

        if (puddleMaterial != null)
        {
            SetTransparent(false);
            puddleMaterial.color = Color.yellow;
        }

        Destroy(gameObject, lifetime);
    }

    void OnTriggerStay(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null)
            player.SetSlowMultiplier(slowMultiplier);

        RaccoonEnemy raccoon = other.GetComponentInParent<RaccoonEnemy>();

        if (raccoon != null)
            raccoon.SetSlowMultiplier(slowMultiplier);
    }

    void SetTransparent(bool transparent)
    {
        if (transparent)
        {
            puddleMaterial.SetFloat("_Surface", 1f);
            puddleMaterial.SetFloat("_Blend", 0f);
            puddleMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            puddleMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            puddleMaterial.SetFloat("_ZWrite", 0f);
            puddleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            puddleMaterial.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            puddleMaterial.SetFloat("_Surface", 0f);
            puddleMaterial.SetFloat("_SrcBlend", (float)BlendMode.One);
            puddleMaterial.SetFloat("_DstBlend", (float)BlendMode.Zero);
            puddleMaterial.SetFloat("_ZWrite", 1f);
            puddleMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            puddleMaterial.renderQueue = (int)RenderQueue.Geometry;
        }
    }
}
