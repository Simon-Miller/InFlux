namespace InFlux
{
    public class ChainLink<T>
    {
        internal ChainLink(T payload, Action callbackWhenDone)
        {
            this.payload = payload;
            this.callbackWhenDone = callbackWhenDone;
        }

        public readonly T payload;
        public readonly Action callbackWhenDone;
    }
}
