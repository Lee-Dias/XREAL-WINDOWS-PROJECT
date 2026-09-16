using UnityEngine;

public class GazeTracker : MonoBehaviour
{
    [Header("Configurações do Raio")]
    [SerializeField] private float rayDistance = 20f; // Distância máxima do olhar
    [SerializeField] private LayerMask objectLayer;   // Definir a layer "Object" no Inspector

    [Header("Cor de Destaque")]
    [SerializeField] private Color hoverColor = Color.red; // Cor quando olhas para o objeto

    private Renderer lastRenderer;
    private Color originalColor;

    void Update()
    {
        // Cria um raio a partir do centro da câmara direcionado para a frente
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Dispara o raio apenas contra os objetos da Layer selecionada
        if (Physics.Raycast(ray, out hit, rayDistance, objectLayer))
        {
            Renderer currentRenderer = hit.collider.GetComponent<Renderer>();

            if (currentRenderer != null)
            {
                // Se começaste a olhar para um objeto novo
                if (lastRenderer != currentRenderer)
                {
                    ResetLastObject(); // Limpa o objeto anterior

                    // Guarda o objeto atual e a respetiva cor original
                    lastRenderer = currentRenderer;
                    originalColor = currentRenderer.material.color;

                    // Muda a cor para a cor de destaque
                    currentRenderer.material.color = hoverColor;
                }
            }
        }
        else
        {
            // Se deixaste de olhar para qualquer objeto válido
            ResetLastObject();
        }
    }

    // Função para repor a cor original do objeto anterior
    private void ResetLastObject()
    {
        if (lastRenderer != null)
        {
            lastRenderer.material.color = originalColor;
            lastRenderer = null;
        }
    }

    // Desenha uma linha vermelha na Scene View para te ajudar a depurar (Debug)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * rayDistance);
    }
}