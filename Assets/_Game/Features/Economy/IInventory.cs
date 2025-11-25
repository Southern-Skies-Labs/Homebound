namespace Homebound.Features.Economy
{
    public interface IInventory
    {
        //Intenta añadir items. Retorna la cantidad que no pudo añadirse
        int Add(ItemData item, int amount);
        
        
        //Intenta remover items. Retorna true si tenia suficientes
        bool Remove(ItemData item, int amount);
        
        //Verifica si tiene cierta cantidad de items
        bool Has(ItemData item, int amount);
        
        //Devuelve cuantos items tiene de un determinado tipo
        int Count(ItemData item);
        
        
    }
}
