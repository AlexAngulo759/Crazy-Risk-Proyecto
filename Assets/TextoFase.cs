using TMPro;
using UnityEngine;

public class TextoFase : MonoBehaviour
{
    private TextMeshProUGUI texto;

    void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        switch (GameManager.Instance.fase)
        {
            case 1:
                texto.text = "Fase: Refuerzos";
                break;
            case 2:
                texto.text = "Fase: Ataque";
                break;
            case 3:
                texto.text = "Fase: Reagrupamiento";
                break;
            default:
                texto.text = "Fase: Desconocida";
                break;
        }
    }
}
