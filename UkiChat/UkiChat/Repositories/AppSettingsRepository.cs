using System;
using System.Collections.ObjectModel;
using System.Linq;
using DryIoc.ImTools;
using LiteDB;
using UkiChat.Core;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public class AppSettingsRepository(LiteDatabase db) : BaseRepository<AppSettings, long>(db)
    , IAppSettingsRepository
{
    public AppSettings GetActiveAppSettings()
    {
        return Collection
            .Include(x => x.Profile)
            .FindOne(x => x.Profile.Active);
    }
}