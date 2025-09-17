using UkiChat.Core;
using UkiChat.Entities;

namespace UkiChat.Repositories;

public interface IProfileRepository : IBaseCrudRepository<Profile, long>
{
    Profile GetDefaultProfile();
}