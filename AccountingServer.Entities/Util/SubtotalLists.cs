using System.Collections.Generic;

namespace AccountingServer.Entities.Util
{
    public class ListOfSubtotalRoots<TC> : List<ISubtotalRoot<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalDates<TC> : List<ISubtotalDate<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalUsers<TC> : List<ISubtotalUser<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalCurrencys<TC> : List<ISubtotalCurrency<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalTitles<TC> : List<ISubtotalTitle<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalSubTitles<TC> : List<ISubtotalRoot<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalContents<TC> : List<ISubtotalContent<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }

    public class ListOfSubtotalRemarks<TC> : List<ISubtotalRemark<TC>>, ISubtotalResults<ISubtotalResult<TC>>
        where TC : ISubtotalResult
    {
        IEnumerator<ISubtotalResult<TC>> IEnumerable<ISubtotalResult<TC>>.GetEnumerator() => base.GetEnumerator();

        public T Accept<T>(ISubtotalItemsVisitor<T> visitor) => visitor.Visit(this);
    }
}
