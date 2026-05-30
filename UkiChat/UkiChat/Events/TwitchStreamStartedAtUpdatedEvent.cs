using System;
using Prism.Events;

namespace UkiChat.Events;

public class TwitchStreamStartedAtUpdatedEvent : PubSubEvent<DateTime?> { }
