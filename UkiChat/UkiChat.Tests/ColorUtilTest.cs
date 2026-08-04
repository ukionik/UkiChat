using System.Collections.Generic;
using UkiChat.Utils;
using Xunit;

namespace UkiChat.Tests;

public class ColorUtilTest
{
    // Палитра Twitch по умолчанию (ColorUtil.DefaultColors).
    private static readonly HashSet<string> DefaultColors =
    [
        "#FF0000", "#0000FF", "#00FF00", "#B22222", "#FF7F50",
        "#9ACD32", "#FF4500", "#2E8B57", "#DAA520", "#D2691E",
        "#5F9EA0", "#1E90FF", "#FF69B4", "#8A2BE2", "#00FF7F"
    ];

    [Fact]
    public void GetDisplayNameColor_ExplicitColor_IsReturnedAsIs()
    {
        Assert.Equal("#ABCDEF", ColorUtil.GetDisplayNameColor("Viewer", "#ABCDEF"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetDisplayNameColor_WithoutColor_FallsBackToPalette(string? hexColor)
    {
        Assert.Contains(ColorUtil.GetDisplayNameColor("Viewer", hexColor), DefaultColors);
    }

    /// <summary>
    ///     Главная гарантия: цвет ника воспроизводим МЕЖДУ ЗАПУСКАМИ приложения.
    ///     Значения посчитаны независимо от кода (FNV-1a поверх UTF-8 имени в нижнем регистре)
    ///     и зашиты здесь намеренно — тест обязан упасть, если хэш-функцию подменят.
    ///     Раньше индекс брался из string.GetHashCode(), который рандомизируется при каждом
    ///     старте процесса, и цвет зрителя менялся после каждого перезапуска.
    /// </summary>
    [Theory]
    [InlineData("Viewer", "#DAA520")]
    [InlineData("ukionik", "#0000FF")]
    [InlineData("moderator", "#8A2BE2")]
    [InlineData("Наташа", "#8A2BE2")]
    [InlineData("", "#0000FF")]
    public void GetDisplayNameColor_WithoutColor_IsReproducibleAcrossRuns(string displayName, string expected)
    {
        Assert.Equal(expected, ColorUtil.GetDisplayNameColor(displayName));
    }

    [Theory]
    [InlineData("ukionik")]
    [InlineData("UkiOnik")]
    [InlineData("UKIONIK")]
    public void GetDisplayNameColor_IgnoresLetterCase(string displayName)
    {
        // Один человек может писаться на площадках по-разному — цвет должен совпадать.
        Assert.Equal("#0000FF", ColorUtil.GetDisplayNameColor(displayName));
    }

    [Fact]
    public void GetDisplayNameColor_SameNameOnDifferentPlatforms_GivesSameColor()
    {
        // Ни Twitch (пустой HexColor), ни YouTube цвет не прислали — цвет считается по нику,
        // поэтому один и тот же зритель выглядит одинаково на обеих площадках.
        var twitch = ColorUtil.GetDisplayNameColor("ukionik", "");
        var youTube = ColorUtil.GetDisplayNameColor("ukionik");
        var vkWithoutIndex = ColorUtil.GetVkVideoLiveNickColor(null, "ukionik");

        Assert.Equal(twitch, youTube);
        Assert.Equal(twitch, vkWithoutIndex);
    }

    [Theory]
    [InlineData(0, "#D66E34")]
    [InlineData(1, "#B8AAFF")]
    [InlineData(15, "#A20BFF")]
    public void GetVkVideoLiveNickColor_ValidIndex_ReturnsPlatformColor(int index, string expected)
    {
        // Индекс от VK — это выбор площадки, аналог HexColor у Twitch: он всегда в приоритете.
        Assert.Equal(expected, ColorUtil.GetVkVideoLiveNickColor(index, "ukionik"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void GetVkVideoLiveNickColor_MissingOrOutOfRange_FallsBackToNameColor(int? index)
    {
        // Раньше здесь возвращался первый цвет палитры VK — все такие зрители были одного цвета.
        Assert.Equal(ColorUtil.GetDisplayNameColor("ukionik"), ColorUtil.GetVkVideoLiveNickColor(index, "ukionik"));
    }
}
