using LiteDB;

namespace UkiChat.Entities;

public class DonationAlertsSettings : IBaseEntity<long>
{
    [BsonId]
    public long Id { get; set; }

    // Креды приложения (Authorization Code flow), сидятся из дефолтов
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    // Токены авторизованного пользователя
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }

    // Данные авторизованного пользователя DonationAlerts
    public string? UserId { get; set; }
    public string? UserName { get; set; }

    [BsonRef]
    public required AppSettings? AppSettings { get; set; }
}
