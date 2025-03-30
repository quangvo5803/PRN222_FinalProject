using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Repositories.Interface
{
    public interface IRepository<T>
        where T : class
    {
        IEnumerable<T> GetAll(string? includeProperties = null);
        public T? Get(Expression<Func<T, bool>> filter);
        public T? Get(Expression<Func<T, bool>> filter, string? includeProperties = null);
        IEnumerable<T> GetRange(Expression<Func<T, bool>> filter, string? includeProperties = null);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        void AddRange(IEnumerable<T> entities);
    }
}
