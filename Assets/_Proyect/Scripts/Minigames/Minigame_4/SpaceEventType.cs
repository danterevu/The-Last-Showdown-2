/// Todos los eventos RANDOM disponibles para el pool de sorteo.
/// Los eventos de zona fija NO van acá (se configuran directo en el Inspector del SpaceEventManager).
public enum SpaceEventType
{
    None,
    BlackHole,          // Agujero negro que atrae a los jugadores (Random)
    DimensionalPortals, // Portales que teletransportan(Random)
    PowerUpRain,        // Muchos power ups spawnean(Random)
    AlienShips,         // Naves alienígenas que empujan (zona fija también disponible)
    MediumShip          // Nave mediana que empuja al colisionar (zona fija también disponible)
}
