using System;

namespace BasketService.Domain.Exceptions
{
    /// <summary>
    /// Levée par <see cref="AddItemToBasketUseCase"/> quand la quantité finale accumulée
    /// dépasse le plafond <c>limitMax</c> fourni par l'appelant.
    /// Contient les données brutes — le message utilisateur est formé par la couche présentation.
    /// </summary>
    public sealed class StockLimitExceededException : Exception
    {
        public int FinalQuantity { get; }
        public int LimitMax      { get; }

        public StockLimitExceededException(int finalQuantity, int limitMax)
            : base($"Final quantity {finalQuantity} exceeds limit {limitMax}.")
        {
            FinalQuantity = finalQuantity;
            LimitMax      = limitMax;
        }
    }
}
