using UnityEngine;

// Interfaz que todos los minijuegos deben implementar
public interface IMinijuegoControlable
{
    // Se llama para inicializar el minijuego (pero no empezar)
    void InicializarMinijuego();
    
    // Se llama para INICIAR el minijuego cuando termine el contador
    void IniciarMinijuego();
    
    // Se llama para congelar a los jugadores
    void CongelarJugadores();
    
    // Se llama para descongelar a los jugadores
    void DescongelarJugadores();
}
