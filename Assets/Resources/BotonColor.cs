using UnityEngine;
using UnityEngine.UI;

public class BotonColor : MonoBehaviour
{
    public Button boton;
    public bool derecho = false; // true = derecho (cliente), false = izquierdo (servidor)
    public int color = 1; // 1 = rojo, 2 = verde, 3 = azul

    void Start()
    {
        boton.onClick.AddListener(OnClick);
    }

    void Update()
    {
        // Solo activo si es el turno correcto y es el rol correcto
        bool puedeElegir = (!derecho && GameManager.Instance.turno == 1 && GameManager.Instance.esServidor) ||
                           (derecho && GameManager.Instance.turno == 2 && !GameManager.Instance.esServidor);
        boton.interactable = puedeElegir;
    }

    void OnClick()
    {
        // Solo procesa si el turno y rol coinciden
        if ((!derecho && GameManager.Instance.turno == 1 && GameManager.Instance.esServidor) ||
            (derecho && GameManager.Instance.turno == 2 && !GameManager.Instance.esServidor))
        {
            if (!derecho) // izquierdo = servidor
            {
                GameManager.Instance.color1 = color;
                Debug.Log("Servidor seleccionó color: " + color);
            }
            else // derecho = cliente
            {
                GameManager.Instance.color2 = color;
                Debug.Log("Cliente seleccionó color: " + color);
            }
        }
    }
}


