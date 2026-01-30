using System.IO;
using Tomlyn;
using UkiChat.Tests.AppSettingsData;
using Xunit;
using Xunit.Abstractions;

namespace UkiChat.Tests;

public class TomlTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void AppSettingsTemplateTest()
    {
        var tomlContent = File.ReadAllText("app-settings.template.toml");
        Assert.NotNull(tomlContent);
        var model = Toml.ToModel<AppSettings>(tomlContent);
        Assert.NotNull(model);
        Assert.NotNull(model.Twitch);
        Assert.NotNull(model.Twitch.Chat);
        Assert.Equal("your_twitch_username", model.Twitch.Chat.Username);
        Assert.Equal("your_twitch_access_token", model.Twitch.Chat.AccessToken);
        Assert.Equal("your_twitch_channel", model.Twitch.Chat.Channel);
        Assert.Equal("your_twitch_client_id", model.Twitch.Chat.ClientId);
        Assert.Equal("your_twitch_refresh_token", model.Twitch.Chat.RefreshToken);
        Assert.NotNull(model.VkVideoLive);
        Assert.NotNull(model.VkVideoLive.Chat);
        Assert.Equal("your_vkvideolive_channel", model.VkVideoLive.Chat.Channel);
        Assert.NotNull(model.VkVideoLive.Api);
        Assert.Equal("your_vkvideolive_client_id", model.VkVideoLive.Api.ClientId);
        Assert.Equal("your_vkvideolive_client_secret", model.VkVideoLive.Api.ClientSecret);
        Assert.Equal("your_vkvideolive_access_token", model.VkVideoLive.Api.AccessToken);
    }
    
    [Fact]
    public void AppSettingsLocalTest()
    {
        var model = AppSettingsReader.Read();
        testOutputHelper.WriteLine("[Twitch]");
        testOutputHelper.WriteLine(model.Twitch.Chat.Username);
        testOutputHelper.WriteLine(model.Twitch.Chat.AccessToken);
        testOutputHelper.WriteLine(model.Twitch.Chat.Channel);
        testOutputHelper.WriteLine(model.Twitch.Chat.ClientId);
        testOutputHelper.WriteLine(model.Twitch.Chat.RefreshToken);
        testOutputHelper.WriteLine("[VkVideoLiveChat]");
        testOutputHelper.WriteLine(model.VkVideoLive.Chat.Channel);
        testOutputHelper.WriteLine("[VkVideoLiveApi]");
        testOutputHelper.WriteLine(model.VkVideoLive.Api.ClientId);
        testOutputHelper.WriteLine(model.VkVideoLive.Api.ClientSecret);
        testOutputHelper.WriteLine(model.VkVideoLive.Api.AccessToken);
    }
}