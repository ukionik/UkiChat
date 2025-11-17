using LiteDB;
using UkiChat.Core;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public class AppSettingsRepository(LiteDatabase db) : BaseRepository<AppSettings, long>(db) 
    , IAppSettingsRepository;