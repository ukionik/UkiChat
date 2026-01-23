using UkiChat.Core;
using UkiChat.Entities;

namespace UkiChat.Repositories.Database;

public interface IProfileRepository : IBaseCrudRepository<Profile, long>
{
    Profile GetDefaultProfile();
}