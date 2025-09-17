using System.Collections.Generic;
using LiteDB;
using UkiChat.Entities;

namespace UkiChat.Core;

public abstract class BaseRepository<TEntity, TId>(LiteDatabase db) : IBaseCrudRepository<TEntity, TId>
    where TEntity : IBaseEntity<TId>
{
    protected readonly ILiteCollection<TEntity> Collection = db.GetCollection<TEntity>();

    public long Count()
    {
        return Collection.Count();
    }

    public IEnumerable<TEntity> GetAll()
    {
        return Collection.FindAll();
    }

    public TEntity GetById(TId id)
    {
        return Collection.FindById(new BsonValue(id));
    }

    public void Save(TEntity entity)
    {
        Collection.Upsert(entity);
    }

    public void Delete(TId id)
    {
        Collection.Delete(new BsonValue(id));
    }
}