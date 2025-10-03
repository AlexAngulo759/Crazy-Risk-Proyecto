using TMPro;
using UnityEngine;

public class TextoRefuerzos : MonoBehaviour
{
    private TextMeshProUGUI texto;

    void Awake()
    {
        texto = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.turno == 1)
        {
            texto.text = "Refuerzos: " + GameManager.Instance.tropasReforzar1;
        }
        else if (GameManager.Instance.turno == 2)
        {
            texto.text = "Refuerzos: " + GameManager.Instance.tropasReforzar2;
        }
    }
}

