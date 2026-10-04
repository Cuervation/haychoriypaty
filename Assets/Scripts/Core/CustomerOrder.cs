namespace HayChoriYPaty
{
    public enum OrderKind
    {
        Chori,
        ChoriAndCoca,
        CocaOnly
    }

    public enum GuestState
    {
        Waiting,
        Cooking,
        Seasoning,
        Ready
    }

    /// <summary>Transient guest/order state. Kept independent of the replaceable view.</summary>
    public sealed class CustomerOrder
    {
        public int Id { get; private set; }
        public OrderKind Kind { get; private set; }
        public GuestState State { get; set; }
        public float PatienceRemaining { get; set; }
        public float WorkRemaining { get; set; }

        public bool HasFood { get { return Kind != OrderKind.CocaOnly; } }
        public string DisplayName
        {
            get
            {
                switch (Kind)
                {
                    case OrderKind.ChoriAndCoca: return "CHORI + COCA";
                    case OrderKind.CocaOnly: return "COCA EN VASO";
                    default: return "SANDWICH DE CHORI";
                }
            }
        }

        public CustomerOrder(int id, OrderKind kind, float patience)
        {
            Id = id;
            Kind = kind;
            PatienceRemaining = patience;
            State = GuestState.Waiting;
        }
    }
}
