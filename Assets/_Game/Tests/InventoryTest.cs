using UnityEngine;
using Homebound.Core;
using Homebound.Features.Economy;

namespace Homebound.Tests
{
    public class InventoryTest : MonoBehaviour
    {
        // Arrastra aquí el ItemData de "Madera" desde el proyecto
        public ItemData TestItemWood; 

        private void Start()
        {
            Invoke(nameof(RunTest), 0.5f);
        }

        private void RunTest()
        {
            var cityInv = ServiceLocator.Get<CityInventory>();

            if (cityInv == null)
            {
                Debug.LogError("❌ TEST FALLIDO: No se encontró CityInventory.");
                return;
            }
            
            if (TestItemWood == null)
            {
                Debug.LogError("⚠️ TEST WARNING: Asigna el ItemData 'Madera' en el inspector para probar.");
                return;
            }

            // 1. Estado inicial
            Debug.Log($"Madera inicial: {cityInv.Count(TestItemWood)}");

            // 2. Añadir 10 de madera
            cityInv.Add(TestItemWood, 10);
            
            // 3. Verificar
            if (cityInv.Has(TestItemWood, 10))
            {
                Debug.Log($"<color=green>✅ TEST PASADO: El inventario global recibió 10 de madera correctamente.</color>");
            }
            else
            {
                Debug.LogError($"❌ TEST FALLIDO: Se esperaban 10, hay {cityInv.Count(TestItemWood)}");
            }
        }
    }
}