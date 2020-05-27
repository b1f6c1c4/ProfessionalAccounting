namespace AccountingServer.Entities.Util
{
    public interface ISubtotalHelper
    {
        T Accept<T>(ISubtotalVisitor<T> visitor);
    }

    public class SubtotalHelperRoot : ISubtotalHelper
    {
        public T Accept<T>(ISubtotalVisitor<T> visitor) => visitor.Visit(this);
    }
}
