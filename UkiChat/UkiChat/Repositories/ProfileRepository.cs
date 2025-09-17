using LiteDB;
using UkiChat.Core;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public class ProfileRepository(LiteDatabase db) : BaseRepository<Profile, long>(db)
    , IProfileRepository
{
    public Profile GetDefaultProfile()
    {
        return Collection.FindOne(x => x.Default);
    }
}