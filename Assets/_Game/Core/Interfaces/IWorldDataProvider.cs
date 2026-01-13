using UnityEngine;

namespace Homebound.Core
{
    public interface IWorldDataProvider
    {
        /// <summary>
        /// Devuelve el ID del bloque en una posición global dada.
        /// </summary>
        int GetBlockIDAt(Vector3Int globalPos);
    }
}