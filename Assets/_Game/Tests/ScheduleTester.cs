using UnityEngine;
using Homebound.Features.TimeSystem;
using Homebound.Core;

public class SchedulerTester : MonoBehaviour
{
    private TimeEventSystem _scheduler;

    private void Start()
    {
        _scheduler = ServiceLocator.Get<TimeEventSystem>();
        
        Debug.Log("[TEST] Prueba iniciada. Programando evento para dentro de 30 minutos (juego)...");

        // Prueba 1: Evento Relativo
        _scheduler.ScheduleEventRelative("Test_Alarma", 0, 30, () => 
        {
            Debug.Log("<color=green>[TEST] ¡ÉXITO! Han pasado 30 minutos en el juego. El Scheduler funciona.</color>");
        });
    }

    [ContextMenu("Test Late Event")]
    public void TestLate()
    {
        // Prueba manual desde el inspector
        _scheduler.ScheduleEventRelative("Test_Manual", 1, 0, () => Debug.Log("Evento manual disparado tras 1 hora."));
    }
}