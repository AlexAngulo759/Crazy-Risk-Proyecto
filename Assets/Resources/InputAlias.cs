using UnityEngine;
using TMPro; // Importante para TMP_InputField

public class InputAlias : MonoBehaviour
{
    public TMP_InputField input;  // Ahora es TMP_InputField
    public bool derecho = false; // true = derecho (cliente), false = izquierdo (servidor)

    void Start()
    {
        input.onEndEdit.AddListener(OnEndEdit);
    }

    void Update()
    {
        // Solo editable si turno y rol correctos
        bool editable = (!derecho && GameManager.Instance.turno == 1 && GameManager.Instance.esServidor) ||
                        (derecho && GameManager.Instance.turno == 2 && !GameManager.Instance.esServidor);
        input.interactable = editable;
    }

    void OnEndEdit(string texto)
    {
        // Solo procesa si turno y rol correctos
        bool rolCorrecto = (!derecho && GameManager.Instance.turno == 1 && GameManager.Instance.esServidor) ||
                           (derecho && GameManager.Instance.turno == 2 && !GameManager.Instance.esServidor);

        if (!rolCorrecto) return;

        if (!derecho) GameManager.Instance.jugador1 = texto;
        else GameManager.Instance.jugador2 = texto;

        Debug.Log((!derecho ? "Servidor" : "Cliente") + " ingresó alias: " + texto);
    }
}
