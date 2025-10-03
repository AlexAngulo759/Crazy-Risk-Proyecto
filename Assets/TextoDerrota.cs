using UnityEngine;
using TMPro;

public class TextoDerrota : MonoBehaviour
{
    private TextMeshProUGUI texto;

    void Start()
    {
        texto = GetComponent<TextMeshProUGUI>();
        if (texto == null) return;

        string nombreJugador;
        Color colorJugador;

        if (GameManager.Instance.esServidor)
        {
            nombreJugador = GameManager.Instance.jugador1;
            colorJugador = GetColorFromInt(GameManager.Instance.color1);
        }
        else
        {
            nombreJugador = GameManager.Instance.jugador2;
            colorJugador = GetColorFromInt(GameManager.Instance.color2);
        }

        texto.text = $"El jugador {nombreJugador} ha perdido";
        texto.color = colorJugador;
    }

    private Color GetColorFromInt(int colorInt)
    {
        switch (colorInt)
        {
            case 1: return Color.red;
            case 2: return Color.green;
            case 3: return Color.blue;
            default: return Color.white;
        }
    }
}
