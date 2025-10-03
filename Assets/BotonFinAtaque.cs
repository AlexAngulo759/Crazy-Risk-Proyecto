using UnityEngine;
using UnityEngine.UI;

public class BotonFinAtaque : MonoBehaviour
{
    private Button boton;

    void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(FinalizarAtaque);
    }

    void FinalizarAtaque()
    {
        if (GameManager.Instance == null) return;

        // ✅ Solo el jugador activo puede terminar ataque
        if (GameManager.Instance.fase == 2 &&
            ((GameManager.Instance.turno == 1 && MyNetworkManager.Instance.esServidor) ||
             (GameManager.Instance.turno == 2 && !MyNetworkManager.Instance.esServidor)))
        {
            // Pasar a fase 3
            GameManager.Instance.fase = 3;
            Debug.Log("✅ Ataque terminado → fase = 3");

            // Avisar al otro jugador (puede incluir fase también)
            if (MyNetworkManager.Instance != null)
            {
                string mensaje = $"TURN:{GameManager.Instance.turno}";
                MyNetworkManager.Instance.EnviarMensaje(mensaje);
            }

            // Cambiar turno y reiniciar a fase 1
            CambiarTurno();
        }
    }

    void CambiarTurno()
    {
        // Cambiar de turno
        GameManager.Instance.turno = (GameManager.Instance.turno == 1) ? 2 : 1;
        GameManager.Instance.fase = 1;

        Debug.Log($"🔄 Cambio de turno → turno = {GameManager.Instance.turno}, fase = {GameManager.Instance.fase}");

        // Notificar al otro jugador
        if (MyNetworkManager.Instance != null)
        {
            string mensaje = $"TURN:{GameManager.Instance.turno}";
            MyNetworkManager.Instance.EnviarMensaje(mensaje);
        }
    }
}
