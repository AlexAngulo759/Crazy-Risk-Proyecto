using TMPro;
using UnityEngine;

public class TextoTurno : MonoBehaviour
{
    private TextMeshProUGUI texto;

    void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        switch (GameManager.Instance.turno)
        {
            case 1:
                texto.text = "Turno: Servidor";
                break;
            case 2:
                texto.text = "Turno: Cliente";
                break;
            case 3:
                texto.text = "Turno: Neutro";
                break;
        }
    }
}
