using System;
using UnityEngine;


namespace Homebound.Features.Economy
{
    [Serializable]
    public class InventorySlot
    {
        //Variables
        public ItemData Item;
        public int Amount;
        
        //Metodos
        public InventorySlot(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public void Add(int quantity) => Amount += quantity;
        public void Remove(int quantity) => Amount = Mathf.Max(0, Amount - quantity);
        public bool IsEmpty => Amount <= 0;

    }
}
