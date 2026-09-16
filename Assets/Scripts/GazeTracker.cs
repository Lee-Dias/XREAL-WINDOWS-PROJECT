using UnityEngine;

public class GazeTracker : MonoBehaviour
{
    [Header("Configurações do Raio")]
    [SerializeField] private float rayDistance = 20f; 
    [SerializeField] private LayerMask objectLayer; 

    [Header("Cor de Destaque")]
    [SerializeField] private Color hoverColor = Color.red; 

    private Renderer lastRenderer;
    private Color originalColor;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;


        if (Physics.Raycast(ray, out hit, rayDistance, objectLayer))
        {
            Renderer currentRenderer = hit.collider.GetComponent<Renderer>();

            if (currentRenderer != null)
            { 
                if (lastRenderer != currentRenderer)
                {
                    ResetLastObject(); 

                    lastRenderer = currentRenderer;
                    originalColor = currentRenderer.material.color;

                    currentRenderer.material.color = hoverColor;
                }
            }
        }
        else
        {

            ResetLastObject();
        }
    }

    private void ResetLastObject()
    {
        if (lastRenderer != null)
        {
            lastRenderer.material.color = originalColor;
            lastRenderer = null;
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
    }
}