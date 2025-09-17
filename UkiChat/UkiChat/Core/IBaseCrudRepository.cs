using System.Collections.Generic;
using UkiChat.Entities;

namespace UkiChat.Core;

public interface IBaseCrudRepository<TEntity, in TId>
    where TEntity : IBaseEntity<TId>
{
    long Count();
    IEnumerable<TEntity> GetAll();
    TEntity GetById(TId id);
    void Save(TEntity entity);
    void Delete(TId id);
}